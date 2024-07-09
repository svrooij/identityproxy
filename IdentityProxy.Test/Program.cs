// See https://aka.ms/new-console-template for more information

// This is how you would build the docker container in this project
/*
using DotNet.Testcontainers.Builders;

var futureImage = new ImageFromDockerfileBuilder()
    .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
    .WithDockerfile("IdentityProxy.Api/Dockerfile")
    .Build();
await futureImage!.CreateAsync();
var hostPort = new Random().Next(49152, 65535);
var futureContainer = new ContainerBuilder()
    .WithImage(futureImage)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("EXTERNAL_URL", $"http://localhost:{hostPort}")
    .WithEnvironment("IDENTITY_AUTHORITY", "https://login.microsoftonline.com/{tenant-id}/v2.0/")
    .WithPortBinding(hostPort, 8080)
    .Build();


await futureContainer!.StartAsync();

Console.WriteLine($"Well known config at: http://localhost:{hostPort}/.well-known/openid-configuration");
Console.ReadLine();

await futureContainer!.DisposeAsync();
*/

// This is how you would use the Testcontainers.IdenityProxy library

using Testcontainers.IdentityProxy;

var identityProxy = new IdentityProxyBuilder()
    .WithAuthority("https://login.microsoftonline.com/svrooij.io/v2.0/")
    .WithRandomPort()
    .Build();

await identityProxy.StartAsync();

Console.WriteLine($"Well known config at: {identityProxy.GetAuthority()}.well-known/openid-configuration");

Console.WriteLine($"You can request a token by posting to {identityProxy.GetAuthority()}api/identity/token");
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