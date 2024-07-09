using System.Text.Json.Serialization;

namespace IdentityProxy.Api.Identity.Models;


public class Jwks
{
    [JsonPropertyName("keys")]
    public required Jwk[] Keys { get; set; }
}

public class Jwk
{
    [JsonPropertyName("kid")]
    public string? Kid { get; set; }
    [JsonPropertyName("nbf")]
    public long? NotBefore { get; set; }
    [JsonPropertyName("use")]
    public string? Usage { get; set; }
    [JsonPropertyName("kty")]
    public string? KeyType { get; set; }
    [JsonPropertyName("e")]
    public string? Exponent { get; set; }
    [JsonPropertyName("n")]
    public string? Modulus { get; set; }
}
