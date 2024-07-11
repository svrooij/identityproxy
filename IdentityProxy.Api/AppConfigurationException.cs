namespace IdentityProxy.Api;

/// <summary>
/// Exception thrown when the configuration is invalid
/// </summary>
public sealed class AppConfigurationException : ApplicationException
{
    internal AppConfigurationException(string? message) : base(message)
    {
    }
}
