﻿using DotNet.Testcontainers.Containers;
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
    public IdentityProxyContainer(IdentityProxyConfiguration configuration) : base(configuration)
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
        return $"http://{this.Hostname}:{this.GetMappedPublicPort(IdentityProxyBuilder.API_PORT)}/";
    }

    /// <summary>
    /// Get a mocked token with all the claims you want in the token
    /// </summary>
    /// <param name="tokenRequest">TokenRequest</param>
    /// <returns></returns>
    public async Task<TokenResult?> GetTokenAsync(TokenRequest tokenRequest, CancellationToken cancellationToken = default)
    {
        _httpClient.BaseAddress ??= new Uri($"http://{this.Hostname}:{this.GetMappedPublicPort(IdentityProxyBuilder.API_PORT)}/");
        TokenResult? token = null;
        int retries = 0;
        while(token == null && !cancellationToken.IsCancellationRequested && retries < 3)
        {
            try
            {
                token = await GetTokenInternalAsync(tokenRequest, cancellationToken);
            }
            catch
            {
                // Ignore exceptions
            }
            if(token == null)
            {
                retries++;
                if(retries > 3)
                {
                    break;
                }
                await Task.Delay(new Random().Next(5, 50), cancellationToken);
            }
        }
        return token;
    }

    private async Task<TokenResult?> GetTokenInternalAsync(TokenRequest tokenRequest, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/identity/token", tokenRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadFromJsonAsync<TokenResult>(cancellationToken);
    }
}
