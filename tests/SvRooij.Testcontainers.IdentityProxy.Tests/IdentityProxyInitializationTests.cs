using TUnit.Core;
using Testcontainers.IdentityProxy;
namespace SvRooij.Testcontainers.IdentityProxy.Tests;

public class IdentityProxyInitializationTests
{
    private static IdentityProxyContainer? _container;

    [Before(Class)]
    public static void ContainerSetup()
    {
        if (_container == null)
        {
            var builder = new IdentityProxyBuilder()
                .WithImage("svrooij/identityproxyapi:test") // Use the test image, make sure this is available locally
                .WithImagePullPolicy((_) => false) // Don't pull the image, use the local one
                .WithAuthority("https://login.microsoftonline.com/svrooij.io/v2.0/");

            _container = builder.Build();
        }
    }

    [Test]
    public async Task IdentityProxyContainer_Should_Initialize_Correctly()
    {
        await _container!.StartAsync(TestContext.Current?.Execution.CancellationToken ?? CancellationToken.None);
        var authority = _container.GetAuthority();
        await Assert.That(authority).IsNotNull();
    }

    [After(Class)]
    public static async Task Cleanup()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
            _container = null;
        }
    }
}
