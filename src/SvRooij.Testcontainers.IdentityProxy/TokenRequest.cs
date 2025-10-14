using System.Text.Json.Serialization;

namespace Testcontainers.IdentityProxy;
/// <summary>
/// Token request to get a mocked token
/// </summary>
public class TokenRequest
{
    /// <summary>
    /// The lifetime of the token in seconds
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int Lifetime { get; set; } = 3600;

    /// <summary>
    /// The audience of the token
    /// </summary>
    [JsonPropertyName("aud")]
    public string? Audience { get; set; }

    /// <summary>
    /// The issuer of the token (optional)
    /// </summary>
    [JsonPropertyName("iss")]
    public string? Issuer { get; set; }

    /// <summary>
    /// The subject of the token
    /// </summary>
    [JsonPropertyName("sub")]
    public required string Subject { get; set; }

    /// <summary>
    /// The additional claims of the token
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalClaims { get; set; }
}
