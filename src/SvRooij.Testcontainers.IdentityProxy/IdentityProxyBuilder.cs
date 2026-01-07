using Docker.DotNet.Models;
using DotNet.Testcontainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
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
    [Obsolete("Use constructor with authority. This constructor will be removed in future versions.")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public IdentityProxyBuilder() : this(new IdentityProxyConfiguration())
    {
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
    }

    /// <summary>
    /// Initializes a new instance of the IdentityProxyBuilder class using the specified authority endpoint.
    /// </summary>
    /// <param name="authority">The authority endpoint to use for authentication. Cannot be null or empty.</param>
    /// <param name="image">The image to use, if you want a different version then the default</param>
    public IdentityProxyBuilder(string authority, string image = IDENTITY_PROXY_IMAGE) : this(new IdentityProxyConfiguration())
    {
        ArgumentNullException.ThrowIfNullOrEmpty(authority);
        ArgumentNullException.ThrowIfNullOrEmpty(image);
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
        this.WithImage(image).WithAuthority(authority);
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
