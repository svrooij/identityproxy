using System.Text.Json.Serialization;

namespace IdentityProxy.Api.Identity.Models;

public class DuplicateTokenRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = default!;
}
