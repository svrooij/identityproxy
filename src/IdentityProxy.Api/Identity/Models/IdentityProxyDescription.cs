namespace IdentityProxy.Api.Identity.Models;

public class IdentityProxyDescription
{
    public string Application { get; set; } = "IdentityProxy";
    public string Repository { get; set; } = "https://github.com/svrooij/identityproxy";
    public string Version => GetType().Assembly.GetName().Version?.ToString() ?? "0.0.0";
    public required string Authority { get; set; }
    public required string ExternalUrl { get; set; }
    public string OpenIdConfigUrl => $"{ExternalUrl}/.well-known/openid-configuration";
    public string JwksUrl => $"{ExternalUrl}/api/identity/jwks";
    public string TokenUrl => $"{ExternalUrl}/api/identity/token";
    public string DocumentationUrl => $"{ExternalUrl}/scalar/";
}
