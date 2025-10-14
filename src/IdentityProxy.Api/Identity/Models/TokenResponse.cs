using System.Text.Json.Serialization;

namespace IdentityProxy.Api.Identity.Models;

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
