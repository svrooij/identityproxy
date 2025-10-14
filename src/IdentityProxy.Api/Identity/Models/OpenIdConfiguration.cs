using System.Text.Json.Serialization;

namespace IdentityProxy.Api.Identity.Models;

public class OpenIdConfiguration : ICloneable
{
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = default!;
    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; } = default!;
    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = default!;
    [JsonPropertyName("end_session_endpoint")]
    public string? EndSessionEndpoint { get; set; }
    [JsonPropertyName("jwks_uri")]
    public string? JwksUri { get; set; }
    [JsonPropertyName("response_modes_supported")]
    public string[]? ResponseModesSupported { get; set; }
    [JsonPropertyName("response_types_supported")]
    public string[]? ResponseTypesSupported { get; set; }
    [JsonPropertyName("scopes_supported")]
    public string[]? ScopesSupported { get; set; }
    [JsonPropertyName("subject_types_supported")]
    public string[]? SubjectTypesSupported { get; set; }
    [JsonPropertyName("id_token_signing_alg_values_supported")]
    public string[]? IdTokenSigningAlgValuesSupported { get; set; }
    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public string[]? TokenEndpointAuthMethodsSupported { get; set; }
    [JsonPropertyName("claims_supported")]
    public string[]? ClaimsSupported { get; set; }

    public object Clone()
    {
        return new OpenIdConfiguration
        {
            Issuer = Issuer,
            AuthorizationEndpoint = AuthorizationEndpoint,
            TokenEndpoint = TokenEndpoint,
            EndSessionEndpoint = EndSessionEndpoint,
            JwksUri = JwksUri,
            ResponseModesSupported = ResponseModesSupported,
            ResponseTypesSupported = ResponseTypesSupported,
            ScopesSupported = ScopesSupported,
            SubjectTypesSupported = SubjectTypesSupported,
            IdTokenSigningAlgValuesSupported = IdTokenSigningAlgValuesSupported,
            TokenEndpointAuthMethodsSupported = TokenEndpointAuthMethodsSupported,
            ClaimsSupported = ClaimsSupported
        };
    }
}
