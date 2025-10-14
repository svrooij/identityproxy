using IdentityProxy.Api.Identity.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdentityProxy.Api.Identity;

internal static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this WebApplication app, string? externalUrl = null, string openIdConfigUrl = "/.well-known/openid-configuration", string identityPrefix = "/api/identity")
    {
        app.MapGet("/", async (IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            return Results.Ok(new IdentityProxyDescription
            {
                Authority = configuration.GetValue<string>("IDENTITY_AUTHORITY") ?? "Configure 'IDENTITY_AUTHORITY' in Environment!",
                ExternalUrl = externalUrl ?? configuration.GetValue<string>("EXTERNAL_URL") ?? "Configure 'EXTERNAL_URL' in environment"
            });
        });

        // Add the well known config endpoint /.well-known/openid-configuration
        // The IdentityService is injected
        app.MapGet(openIdConfigUrl, async (IdentityService identityService, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            // We need to clone the configuration because we need to change the jwks uri and we don't want to change the original.
            var config = (await identityService.GetExternalOpenIdConfigurationAsync(cancellationToken))!.Clone() as OpenIdConfiguration;
            externalUrl ??= configuration.GetValue<string>("EXTERNAL_URL");

            if (externalUrl is not null)
            {
                config!.JwksUri = new Uri(new Uri(externalUrl), $"{identityPrefix}/jwks").ToString();
            }
            return Results.Ok(config);
        });

        // Group all Identity endpoints under the identityPrefix
        var identityApi = app.MapGroup(identityPrefix);

        // Add the jwks endpoint {identityPrefix}/jwks
        // The IdentityService is injected
        identityApi.MapGet("/jwks", async (IdentityService identityService, CancellationToken cancellationToken) =>
        {
            var jwks = await identityService.GetJwksWithExtraSigningCertAsync(cancellationToken);
            return Results.Ok(jwks);
        });

        // Add the token endpoint {identityPrefix}/token
        // The IdentityService is injected and the TokenRequest is bound from the request body
        identityApi.MapPost("/token", async (IdentityService identityService, [FromBody]TokenRequest request, CancellationToken cancellationToken) =>
        {
            var token = await identityService.GetTokenAsync(request, cancellationToken);
            return Results.Ok(token);
        });

        // Add the token endpoint {identityPrefix}/duplicate-token
        // The IdentityService is injected and the token to duplicate is bound from the request body
        identityApi.MapPost("/duplicate-token", async (IdentityService identityService, [FromBody]DuplicateTokenRequest tokenRequest, CancellationToken cancellationToken) =>
        {
            var newToken = await identityService.GetTokenAsync(tokenRequest.Token, cancellationToken);
            if (newToken is null)
            {
                return Results.BadRequest(new { error = "invalid_token", error_description = "The provided token is invalid or could not be duplicated." });
            }
            return Results.Ok(newToken);
        });
    }
}
