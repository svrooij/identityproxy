using System.Text.Json.Serialization;

namespace Testcontainers.IdentityProxy;
/// <summary>
/// Mocked token result
/// </summary>
public class TokenResult
{
    /// <summary>
    /// Actual access token
    /// </summary>
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }

    /// <summary>
    /// Validity of the token in seconds
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
