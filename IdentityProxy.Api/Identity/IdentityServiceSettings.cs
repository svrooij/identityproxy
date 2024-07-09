namespace IdentityProxy.Api.Identity;

public class IdentityServiceSettings
{
    public required string Authority { get; set; }

    internal Uri GetConfigUri()
    {
        return new Uri(new Uri(Authority), ".well-known/openid-configuration");
    }
}
