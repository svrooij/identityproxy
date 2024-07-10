using IdentityProxy.Api.Identity;
using IdentityProxy.Api.Identity.Models;

var builder = WebApplication.CreateSlimBuilder(args);

// Register some services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CertificateStore>();
var authority = Environment.GetEnvironmentVariable("IDENTITY_AUTHORITY") ?? throw new Exception("IDENTITY_AUTHORITY is not set");
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
app.MapIdentityEndpoints();
app.Run();
