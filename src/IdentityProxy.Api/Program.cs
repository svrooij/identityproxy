using IdentityProxy.Api;
using IdentityProxy.Api.Identity;
using IdentityProxy.Api.Identity.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateSlimBuilder(args);

// Register some services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CertificateStore>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApi();

var authority = builder.Configuration.GetValue<string>("IDENTITY_AUTHORITY") ?? throw new AppConfigurationException("IDENTITY_AUTHORITY is not set");
builder.Services.AddSingleton(new IdentityServiceSettings { Authority = authority });
// This will add the IdentityService to the DI container and configure the HttpClient
builder.Services.AddHttpClient<IdentityService>();

// Json serialization in AOT project
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, IdentityJsonSerializerContext.Default);
});

var app = builder.Build();
// Add the identity endpoints
app.MapIdentityEndpoints(externalUrl: app.Configuration.GetValue<string>("EXTERNAL_URL"));
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Identity Proxy API";
    options.HideClientButton = true;
    options.DarkMode = true;
    options.DynamicBaseServerUrl = true;
});

app.Logger.LogInformation("IdentityProxy will proxy authority: {Authority}", authority);
await app.RunAsync();
