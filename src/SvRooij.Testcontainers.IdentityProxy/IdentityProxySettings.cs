using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Testcontainers.IdentityProxy;
/// <summary>
/// Configuration for the <see cref="IdentityProxyContainer"/>
/// </summary>
public sealed class IdentityProxyConfiguration : ContainerConfiguration
{
    /// <summary>
    /// <see cref="IdentityProxyConfiguration"/> constructor with just an authority
    /// </summary>
    /// <param name="authority"></param>
    public IdentityProxyConfiguration(string? authority = null)
    {
        Authority = authority;
    }

    /// <summary>
    /// <see cref="IdentityProxyConfiguration"/> constructor with a <see cref="IResourceConfiguration{TResource}"/>
    /// </summary>
    /// <param name="resourceConfiguration">Resource configuration</param>
    internal IdentityProxyConfiguration(IResourceConfiguration<CreateContainerParameters> resourceConfiguration) : base(resourceConfiguration)
    {
    }

    /// <summary>
    /// <see cref="IdentityProxyConfiguration"/> constructor with a <see cref="IContainerConfiguration"/>
    /// </summary>
    /// <param name="containerConfiguration">Container configuration</param>
    internal IdentityProxyConfiguration(IContainerConfiguration containerConfiguration)
        : base(containerConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    /// <summary>
    /// Merge two <see cref="IdentityProxyConfiguration"/> instances
    /// </summary>
    /// <param name="oldValue">Previous value</param>
    /// <param name="newValue">New properties to merge</param>
    internal IdentityProxyConfiguration(IdentityProxyConfiguration oldValue, IdentityProxyConfiguration newValue) : base(oldValue, newValue)
    {
        Authority = BuildConfiguration.Combine(oldValue.Authority, newValue.Authority);
    }

    /// <summary>
    /// Authority url (eg. 'https://login.microsoftonline.com/{tenant-id}/v2.0/')
    /// </summary>
    /// <remarks>Will be append with `/.well-known/openid-configuration`</remarks>
    public string? Authority { get; set; }
}
