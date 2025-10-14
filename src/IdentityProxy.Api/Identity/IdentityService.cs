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
    private readonly TimeProvider _timeProvider;

    public IdentityService(IMemoryCache cache, CertificateStore certificateStore, HttpClient httpClient, ILogger<IdentityService> logger, IdentityServiceSettings settings, TimeProvider timeProvider)
    {
        _cache = cache;
        _certificateStore = certificateStore;
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings;
        _timeProvider = timeProvider;
    }

    public async Task<OpenIdConfiguration?> GetExternalOpenIdConfigurationAsync(CancellationToken cancellationToken)
    {
        LogGettingOpenIdConfiguration(_settings.Authority);

        // GetOrCreate will cache when needed or return the cached value
        var result = await _cache.GetOrCreateAsync("OpenIdConfiguration", async entry =>
        {
            LogLoadingOpenIdConfiguration(_settings.Authority);
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

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
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

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
        using var certificate = _certificateStore.GetX509Certificate2();

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

    /// <summary>
    /// Generated a new token based on the provided TokenRequest
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TokenResponse> GetTokenAsync(TokenRequest request, CancellationToken cancellationToken)
    {
        LogGeneratingToken(request.Audience, request.Subject);
        var openIdConfiguration = await GetExternalOpenIdConfigurationAsync(cancellationToken);
        var certificate = _certificateStore.GetX509Certificate2();
        string token;
        // Not disposing the RSA key here, we got errors
        var rsa = certificate.GetRSAPrivateKey();
        // Using an RSA Security Key here, instead of a X509SecurityKey, because the support for RSA keys is better on JWT libraries on other platforms.
        // And because I have not seen a lot of examples with X509SecurityKey or the 'x5t' in the jwks.
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
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Audience = request.Audience ?? "specify-audience-in-token-request",
            Issuer = request.Issuer ?? openIdConfiguration?.Issuer!,
            Claims = claims,
            IssuedAt = now,
            NotBefore = now.AddSeconds(-10), // 10 seconds clock skew
            Expires = now.AddSeconds(request.Lifetime),
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256),
        };

        var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
        handler.SetDefaultTimesOnTokenCreation = false;

        token = handler.CreateToken(descriptor);

        return new TokenResponse
        {
            AccessToken = token,
            ExpiresIn = request.Lifetime,
        };
    }

    // These claims are excluded when duplicating a token, since they are copied during token creation
    private static readonly string[] _excludedFromDuplication = ["iat", "nbf", "exp", "jti", "sub", "aud", "iss"];

    /// <summary>
    /// Generate a new valid token based on an existing token.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<TokenResponse?> GetTokenAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentNullException(nameof(token));
        }
        var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
        handler.MapInboundClaims = false;
        if (token.Count(c => c == '.') == 1)
        {
            // Add fake signature to make it a semantically correct jwt
            // this allows for duplication of tokens that are not signed or where the signature is not sent here
            token += ".a";
        }
        if (!handler.CanReadToken(token))
        {
            _logger.LogWarning("Cannot read token: {Token}", token);
            return null;
        }
        var jwt = handler.ReadJsonWebToken(token);
        int lifetime = (int)(jwt.ValidTo - jwt.ValidFrom).TotalSeconds;
        var request = new TokenRequest
        {
            Audience = jwt.Audiences.FirstOrDefault(),
            Subject = jwt.Subject,
            Issuer = jwt.Issuer,
            AdditionalData = jwt.Claims.Where(c => !_excludedFromDuplication.Contains(c.Type)).ToDictionary(c => c.Type, c => (object)c.Value),
            Lifetime = lifetime
        };
        return await GetTokenAsync(request, cancellationToken);
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
    private partial void LogGeneratingToken(string? Audience, string Subject);
}
