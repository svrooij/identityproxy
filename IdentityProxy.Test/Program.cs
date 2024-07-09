// See https://aka.ms/new-console-template for more information

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