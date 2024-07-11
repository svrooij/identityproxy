using IdentityProxy.Api.Identity.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
namespace IdentityProxy.Api.Identity;

internal partial class IdentityService
{
    private readonly IMemoryCache _cache;
    private readonly CertificateStore _certificateStore;
    private readonly HttpClient _httpClient;
    private readonly ILogger<IdentityService> _logger;
    private readonly IdentityServiceSettings _settings;

    public IdentityService(IMemoryCache cache, CertificateStore certificateStore, HttpClient httpClient, ILogger<IdentityService> logger, IdentityServiceSettings settings)
    {
        _cache = cache;
        _certificateStore = certificateStore;
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings;
    }

    public async Task<OpenIdConfiguration?> GetExternalOpenIdConfigurationAsync(CancellationToken cancellationToken)
    {
        LogGettingOpenIdConfiguration(_settings.Authority);
        
        // GetOrCreate will cache when needed or return the cached value
        var result = await _cache.GetOrCreateAsync("OpenIdConfiguration", async entry =>
        {
            LogLoadingOpenIdConfiguration(_settings.Authority);
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

            // Load the configuration from the authority
            var response = await _httpClient.GetAsync(_settings.GetConfigUri());
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync(IdentityJsonSerializerContext.Default.OpenIdConfiguration, cancellationToken);
        });

        return result;
    }

    public async Task<Jwks?> GetExternalJwksAsync(CancellationToken cancellationToken)
    {
        LogGettingJwks();

        // Check if we loaded the jwks already
        return await _cache.GetOrCreateAsync("Jwks", async entry =>
        {
            var config = await GetExternalOpenIdConfigurationAsync(cancellationToken);
            LogLoadingJwks(config!.JwksUri);
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

            // Load the jwks from the authority
            var response = await _httpClient.GetAsync(config.JwksUri);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync(IdentityJsonSerializerContext.Default.Jwks, cancellationToken);
        });
    }

    public async Task<Jwks?> GetJwksWithExtraSigningCertAsync(CancellationToken cancellationToken)
    {
        var externalJwks = await GetExternalJwksAsync(cancellationToken);
        LogGotExternalJwks(externalJwks?.Keys?.Length ?? 0);
        var certificate = _certificateStore.GetX509Certificate2();

        using var rsa = certificate.PublicKey.GetRSAPublicKey();
        // Using an RSA Security Key here, instead of a X509SecurityKey, because the support for RSA keys is better on JWT libraries on other platforms.
        var rsaKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(rsa));
        // Apparently the KeyId of an RSA key does not get set by default, bug? This is how the `InternalKeyId` is calculated.
        var kid = Base64UrlEncoder.Encode(rsaKey.ComputeJwkThumbprint());
       LogInjectingCertificate(kid);

        var jwk = new Jwk
        {
            // Not sure if this is the correct way to get the kid
            Kid = kid,
            KeyType = rsaKey.Kty,
            // rsaKey.Use is not available, so we use "sig" instead
            Usage = "sig",
            NotBefore = new DateTimeOffset(certificate.NotBefore).ToUnixTimeSeconds(),
            Exponent = rsaKey.E,
            Modulus = rsaKey.N,
        };

        // Create new Jwks object with the keys from the externalJwks.Keys and the jwk for the certificate
        return new Jwks
        {
            Keys = externalJwks?.Keys.Concat([jwk]).ToArray() ?? [jwk]
        };
    }

    public async Task<TokenResponse> GetTokenAsync(TokenRequest request, CancellationToken cancellationToken)
    {
        LogGeneratingToken(request.Audience, request.Subject);
        var openIdConfiguration = await GetExternalOpenIdConfigurationAsync(cancellationToken);
        var certificate = _certificateStore.GetX509Certificate2();
        string token;
        // Not using the new using syntax here, because the key seems to be disposed before the token is created.
        using (var rsa = certificate.GetRSAPrivateKey())
        {
            // Using an RSA Security Key here, instead of a X509SecurityKey, because the support for RSA keys is better on JWT libraries on other platforms.
            var securityKey = new RsaSecurityKey(rsa);
            // WTF Microsoft, why is this empty?
            securityKey.KeyId = Base64UrlEncoder.Encode(securityKey.ComputeJwkThumbprint());

            Dictionary<string, object> claims = new()
            {
                ["sub"] = request.Subject
            };
            if (request.AdditionalData is not null)
            {
                foreach (var (key, value) in request.AdditionalData)
                {
                    claims[key] = value;
                }
            }

            var descriptor = new SecurityTokenDescriptor
            {
                Audience = request.Audience,
                Issuer = request.Issuer ?? openIdConfiguration?.Issuer!,
                Claims = claims,
                IssuedAt = DateTime.UtcNow,
                NotBefore = DateTime.UtcNow.AddSeconds(-10),
                Expires = DateTime.UtcNow.AddSeconds(request.Lifetime),
                SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256),
            };

            var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
            handler.SetDefaultTimesOnTokenCreation = false;

            token = handler.CreateToken(descriptor);
        }

        return new TokenResponse
        {
            AccessToken = token,
            ExpiresIn = request.Lifetime,
        };
    }

    // Use Log source generator, for more speed.
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Proxy OpenId configuration for {Authority}")]
    private partial void LogGettingOpenIdConfiguration(string authority);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Loading remote OpenId configuration {Authority}")]
    private partial void LogLoadingOpenIdConfiguration(string authority);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Getting JWKS")]
    private partial void LogGettingJwks();

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Loading remote JWKS {JwksUri}")]
    private partial void LogLoadingJwks(string? jwksUri);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Got external JWKS with {KeyCount} keys")]
    private partial void LogGotExternalJwks(int keyCount);
    
    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Injecting certificate with kid {Kid}")]
    private partial void LogInjectingCertificate(string kid);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Generating token for audience: '{Audience}' subject: '{Subject}'")]
    private partial void LogGeneratingToken(string Audience, string Subject);
}
