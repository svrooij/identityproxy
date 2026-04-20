using Docker.DotNet.Models;
using DotNet.Testcontainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Images;
using System.Text;

namespace Testcontainers.IdentityProxy;

/// <summary>
/// Builder for the <see cref="IdentityProxyContainer"/>
/// </summary>
/// <remarks>
/// Call <see cref="WithAuthority(string)"/> to set the current authority, this will be mocked.
/// </remarks>
public class IdentityProxyBuilder : ContainerBuilder<IdentityProxyBuilder, IdentityProxyContainer, IdentityProxyConfiguration>
{
    /// <summary>
    /// The default image for the identity proxy
    /// </summary>
    /// <remarks>Source has 'latest', in the nuget package this is replaced with the actual version</remarks>
    public const string IDENTITY_PROXY_IMAGE = "ghcr.io/svrooij/identityproxy:latest";

    /// <summary>
    /// The internal port of the identity proxy
    /// </summary>
    public const ushort API_PORT = 8080;

    /// <summary>
    /// Create a new instance of the <see cref="IdentityProxyBuilder"/>
    /// </summary>
    [Obsolete("This parameterless constructor is obsolete and will be removed. Use IdentityProxyBuilder(authority, image) instead: https://github.com/testcontainers/testcontainers-dotnet/discussions/1470#discussioncomment-15185721.")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public IdentityProxyBuilder() : this(new IdentityProxyConfiguration())
    {
    }

    /// <summary>
    /// Initializes a new instance of the IdentityProxyBuilder class using the specified authority and an optional
    /// Docker image.
    /// </summary>
    /// <param name="authority">The authority URL of the IDP to proxy.</param>
    /// <param name="image">The Docker image to use for the identity proxy container. If not specified, the version tagged with the current version is used.</param>
    public IdentityProxyBuilder(string authority, string image = IDENTITY_PROXY_IMAGE) : this(authority, new DockerImage(image))
    {
    }

    /// <summary>
    /// Initializes a new instance of the IdentityProxyBuilder class using the specified authority and container image.
    /// </summary>
    /// <param name="authority">The authority URL to use for identity proxy configuration. This value is typically the address of the
    /// authentication server.</param>
    /// <param name="image">The container image to use for the identity proxy. Cannot be null.</param>
    public IdentityProxyBuilder(string authority, IImage image) : this(new IdentityProxyConfiguration(authority: authority))
    {
        var builder = Init()
            .WithImage(image)
            .WithAuthority(authority);
        DockerResourceConfiguration = builder.DockerResourceConfiguration;
    }

    internal IdentityProxyBuilder(IdentityProxyConfiguration dockerResourceConfiguration) : base(dockerResourceConfiguration)
    {
        DockerResourceConfiguration = dockerResourceConfiguration;
    }

    /// <inheritdoc/>
    protected override IdentityProxyConfiguration DockerResourceConfiguration { get; }

    /// <summary>
    /// Set the authority for the identity provider
    /// </summary>
    /// <param name="authority">Authority url (eg. 'https://login.microsoftonline.com/{tenant-id}/v2.0/')</param>
    public IdentityProxyBuilder WithAuthority(string authority)
    {
        if (Uri.TryCreate(authority, UriKind.Absolute, out var uri) && !uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The authority must be a valid url", nameof(authority));
        }
        return Merge(DockerResourceConfiguration, new IdentityProxyConfiguration(authority: authority))
            .WithEnvironment("IDENTITY_AUTHORITY", authority);
    }

    /// <inheritdoc/>
    public override IdentityProxyContainer Build()
    {
        Validate();
        return new IdentityProxyContainer(DockerResourceConfiguration);
    }

    /// <summary>
    /// Builds and returns an instance of <see cref="IdentityProxyContainer"/> using the specified <see
    /// cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> instance to be used by the <see cref="IdentityProxyContainer"/>. Cannot be <see
    /// langword="null"/>.</param>
    /// <returns>A new instance of <see cref="IdentityProxyContainer"/> configured with the current settings and the specified
    /// <see cref="HttpClient"/>.</returns>
    public IdentityProxyContainer Build(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        Validate();
        return new IdentityProxyContainer(DockerResourceConfiguration, httpClient);
    }

    /// <inheritdoc/>
    protected override IdentityProxyBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new IdentityProxyConfiguration(resourceConfiguration));
    }

    /// <inheritdoc/>
    protected override IdentityProxyBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new IdentityProxyConfiguration(resourceConfiguration));
    }

    /// <inheritdoc/>
    protected override IdentityProxyBuilder Init()
    {
        return base.Init()
            .WithImage(IDENTITY_PROXY_IMAGE)
            .WithPortBinding(API_PORT, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(req => req
                    .ForPath("/.well-known/openid-configuration")
                    .ForPort(API_PORT)
                    .WithMethod(HttpMethod.Get)
                ))
            .WithStartupCallback(async (container, cancellationToken) =>
            {
                var externalUrl = $"http://{container.Hostname}:{container.GetMappedPublicPort(API_PORT)}";
                var configData = Encoding.UTF8.GetBytes(GenerateSettings(externalUrl));
                await container.CopyAsync(configData, "/app/appsettings.Production.json", ct: cancellationToken);
            });
    }

    /// <inheritdoc/>
    protected override IdentityProxyBuilder Merge(IdentityProxyConfiguration oldValue, IdentityProxyConfiguration newValue)
    {
        return new IdentityProxyBuilder(new IdentityProxyConfiguration(oldValue, newValue));
    }

    /// <inheritdoc/>
    protected override void Validate()
    {
        base.Validate();
        _ = Guard.Argument(DockerResourceConfiguration.Authority, nameof(DockerResourceConfiguration.Authority)).NotNull().NotEmpty();
    }

    /// <summary>
    /// Generate the appsettings.json file, with the correct external url
    /// </summary>
    /// <param name="externalUrl">The URL where the API is reachable from the host</param>
    /// <returns></returns>
    private static string GenerateSettings(string externalUrl)
    {
        return @$"{{ ""EXTERNAL_URL"": ""{externalUrl}""}}";
    }
}
