using System.Text.Json.Serialization;

namespace IdentityProxy.Api.Identity.Models;

public class TokenRequest
{
    [JsonPropertyName("expires_in")]
    public int Lifetime { get; set; } = 3600;
    [JsonPropertyName("aud")]
    public string? Audience { get; set; }
    [JsonPropertyName("iss")]
    public string? Issuer { get; set; }
    [JsonPropertyName("sub")]
    public string Subject { get; set; } = default!;
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }
}
