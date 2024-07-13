using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Testcontainers.IdentityProxy;
/// <summary>
/// Use this inject additional signing credentials from an identity provider and to get tokens for testing
/// </summary>
/// <remarks>
/// Replace the JWT authority with the value from <see cref="GetAuthority"/> in the client configuration. And get fake tokens with <see cref="GetTokenAsync"/>
/// </remarks>
public class IdentityProxyContainer : DockerContainer
{
    private readonly HttpClient _httpClient;
    private int? _port;
    internal IdentityProxyContainer(IdentityProxyConfiguration configuration) : base(configuration)
    {
        // Create a http client to communicate with the auth proxy and get mocked tokens
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Get the mocked authority with the jwks_uri replaced to inject additional signing credentials
    /// </summary>
    /// <remarks>The default jwt middleware, require metadata over https unless you set 'RequireHttpsMetadata' to <see langword="false"/></remarks>
    public string GetAuthority()
    {
        _port ??= this.GetMappedPublicPort(IdentityProxyBuilder.API_PORT);
        return $"http://{this.Hostname}:{_port}/";
    }

    /// <summary>
    /// Get a mocked token with all the claims you want in the token
    /// </summary>
    /// <param name="tokenRequest">TokenRequest</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> if you want to be able to cancel the call</param>
    /// <returns></returns>
    /// <exception cref="TokenRetrievalFailedException">Thrown when the token could not be retrieved, check container log for what might be happening</exception>
    public async Task<TokenResult> GetTokenAsync(TokenRequest tokenRequest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokenRequest);
        this.Logger?.LogDebug("Getting token from identity proxy for audience: {Audience} with subject {Subject}", tokenRequest.Audience, tokenRequest.Subject);
        _httpClient.BaseAddress ??= new Uri($"http://{this.Hostname}:{_port ??= this.GetMappedPublicPort(IdentityProxyBuilder.API_PORT)}/");
        TokenResult? tokenResult = null;
        int retries = 0;
        while (tokenResult == null && !cancellationToken.IsCancellationRequested && retries < 3)
        {
            try
            {
                tokenResult = await GetTokenInternalAsync(tokenRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                this.Logger?.LogError(ex, "Failed to get token from identity proxy");
                // Ignore exceptions
            }
            if (tokenResult == null)
            {
                retries++;
                if (retries >= 3)
                {
                    break;
                }
                await Task.Delay(new Random().Next(1, 10), cancellationToken);
            }
        }
        return tokenResult ?? throw new TokenRetrievalFailedException();
    }

    private async Task<TokenResult?> GetTokenInternalAsync(TokenRequest tokenRequest, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/identity/token", tokenRequest, cancellationToken);
        // Ensure the response is successful
        // This will throw an exception if the response is not successful
        // which will be logged by the caller
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TokenResult>(cancellationToken);
    }

    /// <summary>
    /// Failed to get a token after 3 retries, this should never happen!
    /// </summary>
    public class TokenRetrievalFailedException : Exception
    {
        internal TokenRetrievalFailedException() : base("Failed to retrieve token from identity proxy") { }
    }
}
