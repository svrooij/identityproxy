using IdentityProxy.Api;
using IdentityProxy.Api.Identity;
using IdentityProxy.Api.Identity.Models;

var builder = WebApplication.CreateSlimBuilder(args);

// Add sensible defaults to the builder
// This will register OpenTelemetry if provided by environment variables
builder.AddSensibleDefaults();
// Register some services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CertificateStore>();
builder.Services.AddSingleton(TimeProvider.System);

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
app.Logger.LogInformation("IdentityProxy will proxy authority: {Authority}", authority);
await app.RunAsync();
