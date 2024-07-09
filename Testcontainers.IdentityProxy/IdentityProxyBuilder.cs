using Docker.DotNet.Models;
using DotNet.Testcontainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Testcontainers.IdentityProxy;

/// <summary>
/// Builder for the <see cref="IdentityProxyContainer"/>
/// </summary>
/// <remarks>
/// Call <see cref="WithAuthority(string)"/> to set the current authority, this will be mocked. Be sure to also call <see cref="WithPort(int)"/> or <see cref="WithRandomPort"/> to set the port and the "EXTERNAL_URL".
/// </remarks>
public class IdentityProxyBuilder : ContainerBuilder<IdentityProxyBuilder, IdentityProxyContainer, IdentityProxyConfiguration>
{
    private const string AUTH_PROXY_IMAGE = "ghcr.io/svrooij/identityproxy:latest";

    /// <summary>
    /// Create a new instance of the <see cref="IdentityProxyBuilder"/>
    /// </summary>
    public IdentityProxyBuilder() : this(new IdentityProxyConfiguration())
    {
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
    }

    public IdentityProxyBuilder(IdentityProxyConfiguration dockerResourceConfiguration) : base(dockerResourceConfiguration)
    {
        DockerResourceConfiguration = dockerResourceConfiguration;
    }

    protected override IdentityProxyConfiguration DockerResourceConfiguration { get; }

    /// <summary>
    /// Set the authority for the identity provider
    /// </summary>
    /// <param name="authority">Authority url (eg. 'https://login.microsoftonline.com/{tenant-id}/v2.0/')</param>
    public IdentityProxyBuilder WithAuthority(string authority)
    {
        return Merge(DockerResourceConfiguration, new IdentityProxyConfiguration(authority: authority))
            .WithEnvironment("IDENTITY_AUTHORITY", authority);
    }

    /// <summary>
    /// Map this app to a specific port at the host
    /// </summary>
    /// <param name="port"></param>
    public IdentityProxyBuilder WithPort(int port)
    {
        return Merge(DockerResourceConfiguration, new IdentityProxyConfiguration(port: port))
            .WithPortBinding(port, 8080)
            .WithEnvironment("EXTERNAL_URL", $"http://localhost:{port}");
    }

    /// <summary>
    /// Map this app to a random port at the host
    /// </summary>
    public IdentityProxyBuilder WithRandomPort()
    {
        var port = new Random().Next(10000, 60000);
        return Merge(DockerResourceConfiguration, new IdentityProxyConfiguration(port: port))
            .WithPortBinding(port, 8080)
            .WithEnvironment("EXTERNAL_URL", $"http://localhost:{port}");
    }



    public override IdentityProxyContainer Build()
    {
        Validate();
        return new IdentityProxyContainer(DockerResourceConfiguration);
    }

    protected override IdentityProxyBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new IdentityProxyConfiguration(resourceConfiguration));
    }

    protected override IdentityProxyBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new IdentityProxyConfiguration(resourceConfiguration));
    }

    protected override IdentityProxyBuilder Init()
    {
        return base.Init()
            .WithImage(AUTH_PROXY_IMAGE)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(req => req
                    .ForPath("/.well-known/openid-configuration")
                    .ForPort(8080)
                    .WithMethod(HttpMethod.Get)
                ));
    }

    protected override IdentityProxyBuilder Merge(IdentityProxyConfiguration oldValue, IdentityProxyConfiguration newValue)
    {
        return new IdentityProxyBuilder(new IdentityProxyConfiguration(oldValue, newValue));
    }

    protected override void Validate()
    {
        base.Validate();
        _ = Guard.Argument(DockerResourceConfiguration.Authroity, nameof(DockerResourceConfiguration.Authroity)).NotNull().NotEmpty();
        _ = Guard.Argument(DockerResourceConfiguration.Port, nameof(DockerResourceConfiguration.Port)).HasValue();
    }
}
