using IdentityProxy.Api.Identity;
using IdentityProxy.Api.Identity.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CertificateStore>();
var authority = Environment.GetEnvironmentVariable("IDENTITY_AUTHORITY") ?? throw new Exception("IDENTITY_AUTHORITY is not set");
builder.Services.AddSingleton(new IdentityServiceSettings { Authority = authority });
builder.Services.AddHttpClient<IdentityService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

app.MapIdentityEndpoints();

app.Run();

[JsonSerializable(typeof(OpenIdConfiguration))]
[JsonSerializable(typeof(Jwks))]
[JsonSerializable(typeof(TokenRequest))]
[JsonSerializable(typeof(TokenResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
