using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Testcontainers.IdentityProxy;
/// <summary>
/// Configuration for the <see cref="IdentityProxyContainer"/>
/// </summary>
public sealed class IdentityProxyConfiguration : ContainerConfiguration
{
    public IdentityProxyConfiguration(string? authority = null)
    {
        Authority = authority;
    }

    public IdentityProxyConfiguration(IResourceConfiguration<CreateContainerParameters> resourceConfiguration) : base(resourceConfiguration)
    {
    }

    public IdentityProxyConfiguration(IContainerConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    public IdentityProxyConfiguration(IdentityProxyConfiguration resourceConfiguration) : this(new IdentityProxyConfiguration(), resourceConfiguration)
    {
    }

    public IdentityProxyConfiguration(IdentityProxyConfiguration oldValue, IdentityProxyConfiguration newValue) : base(oldValue, newValue)
    {
        Authority = BuildConfiguration.Combine(oldValue.Authority, newValue.Authority);
    }

    /// <summary>
    /// Authority url (eg. 'https://login.microsoftonline.com/{tenant-id}/v2.0/')
    /// </summary>
    /// <remarks>Will be append with `/.well-known/openid-configuration`</remarks>
    public string? Authority { get; set; }
}
