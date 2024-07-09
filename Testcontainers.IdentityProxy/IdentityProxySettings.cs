using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testcontainers.IdentityProxy;
/// <summary>
/// Configuration for the <see cref="IdentityProxyContainer"/>
/// </summary>
public sealed class IdentityProxyConfiguration : ContainerConfiguration
{
    public IdentityProxyConfiguration(string? authority = null, int port = 0)
    {
        Authroity = authority;
        Port = port;
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
        Authroity = BuildConfiguration.Combine(oldValue.Authroity, newValue.Authroity);
        Port = BuildConfiguration.Combine(oldValue.Port, newValue.Port);
    }

    /// <summary>
    /// Authority url (eg. 'https://login.microsoftonline.com/{tenant-id}/v2.0/')
    /// </summary>
    /// <remarks>Will be append with `/.well-known/openid-configuration`</remarks>
    public string? Authroity { get; set; }

    /// <summary>
    /// The port the auth proxy will listen on
    /// </summary>
    /// <remarks>Also used to tell the proxy what the host of the jwks_uri should be.</remarks>
    public int Port { get; set; }
}
