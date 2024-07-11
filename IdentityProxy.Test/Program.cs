// See https://aka.ms/new-console-template for more information

// This is how you would use the Testcontainers.IdentityProxy library

using Testcontainers.IdentityProxy;

var identityProxy = new IdentityProxyBuilder()
    .WithAuthority("https://login.microsoftonline.com/svrooij.io/v2.0/")
    .Build();

await identityProxy.StartAsync();
var authority = identityProxy.GetAuthority();
Console.WriteLine($"Well known config at: {authority}.well-known/openid-configuration");
Console.WriteLine($"JWKS_uri at: {authority}api/identity/jwks");

Console.WriteLine($"You can request a token by posting to {authority}api/identity/token");
// Or by using identityProxy.GetTokenAsync(...)
var tokenResult = await identityProxy.GetTokenAsync(new TokenRequest
{
    Audience = "https://api.svrooij.io",
    Subject = "test",
    AdditionalClaims = new Dictionary<string, object>
    {
        { "scope", "openid profile email" }
    }
});

Console.WriteLine($"Token: {tokenResult?.AccessToken}");

Console.ReadLine();

await identityProxy.DisposeAsync();