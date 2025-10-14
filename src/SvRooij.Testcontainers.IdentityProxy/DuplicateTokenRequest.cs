using System.Text.Json.Serialization;

namespace Testcontainers.IdentityProxy;

/// <summary>
/// Request to duplicate an existing token
/// </summary>
public class DuplicateTokenRequest
{
    /// <summary>
    /// Token to duplicated
    /// </summary>
    /// <remarks>You should strip of the signature and the last dot for security reasons!</remarks>
    [JsonPropertyName("token")]
    public required string Token { get; set; }
}
