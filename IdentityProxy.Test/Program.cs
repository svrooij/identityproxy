// See https://aka.ms/new-console-template for more information

// This is how you would use the Testcontainers.IdentityProxy library
using IdentityProxy.Test;
using Testcontainers.IdentityProxy;

var identityProxy = new IdentityProxyBuilder()
    .WithAuthority("https://login.microsoftonline.com/svrooij.io/v2.0/")
    .Build();

await identityProxy.StartAsync();
var authority = identityProxy.GetAuthority();

Console.WriteLine($"Well known config at: {authority}.well-known/openid-configuration");
Console.WriteLine($"JWKS_uri at: {authority}api/identity/jwks");

Console.WriteLine($"You can request a token by posting to {authority}api/identity/token");
Console.WriteLine("Sample token request:");
Console.WriteLine();
Console.Write($@"POST {authority}api/identity/token
Accept: application/json
Content-Type: application/json

{{
  ""aud"": ""62eb2412-f410-4e23-95e7-6a91146bc32c"",
  ""sub"": ""99f0cbaa-b3bb-4a77-81a5-e8d17b2232ec""
}}");

Console.WriteLine();
Console.WriteLine();

var token = await identityProxy.GetTokenAsync(new TokenRequest
{
    Audience = "62eb2412-f410-4e23-95e7-6a91146bc32c",
    Subject = "99f0cbaa-b3bb-4a77-81a5-e8d17b2232ec"
});
Console.WriteLine("Sample token response:");
Console.WriteLine();
Console.Write($@"{{
  ""access_token"": ""{token?.AccessToken}"",
  ""expires_in"": {token?.ExpiresIn},
}}");


//// Get 100 tokens (to test if the proxy can handle multiple requests, as it should in test cases)
//var tasks = new List<Task<TokenResult?>>();
//for (int i = 0; i < 100; i++)
//{
//    tasks.Add(identityProxy.GetTokenAsync(new TokenRequest
//    {
//        Audience = "https://api.svrooij.io",
//        Subject = "test",
//        AdditionalClaims = new Dictionary<string, object>
//        {
//            { "scope", "openid profile email" },
//            { "jti", Guid.NewGuid().ToString() }
//        },
//    }));
//}

//int mycounter = 0;
//await foreach (var task in tasks.ExecuteAsync())
//{
//    //Console.WriteLine($"Token {mycounter++}");
//    Console.WriteLine(task?.AccessToken);
//}


Console.ReadLine();

await identityProxy.DisposeAsync();