namespace IdentityProxy.Api.Identity;

internal class IdentityServiceSettings
{
    public required string Authority { get; set; }

    internal Uri GetConfigUri()
    {
        var authority = !Authority.EndsWith("/") ? $"{Authority}/" : Authority;
        return new Uri(new Uri(authority), ".well-known/openid-configuration");
    }
}
