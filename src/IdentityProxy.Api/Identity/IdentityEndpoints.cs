using IdentityProxy.Api.Identity.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdentityProxy.Api.Identity;

internal static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this WebApplication app, string? externalUrl = null, string openIdConfigUrl = "/.well-known/openid-configuration", string identityPrefix = "/api/identity")
    {
        app.MapGet("/", (IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            return Results.Ok(new IdentityProxyDescription
            {
                Authority = configuration.GetValue<string>("IDENTITY_AUTHORITY") ?? "Configure 'IDENTITY_AUTHORITY' in Environment!",
                ExternalUrl = externalUrl ?? configuration.GetValue<string>("EXTERNAL_URL") ?? "Configure 'EXTERNAL_URL' in environment"
            });
        })
            .WithName("GetProxyConfig")
            .WithTags("IdentityProxy")
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                operation.Summary = "Get config";
                operation.Description = "Get basic information about the identity proxy, or use to check if running.";
                return Task.CompletedTask;
            }).Produces<IdentityProxyDescription>(200);

        // Add the well known config endpoint /.well-known/openid-configuration
        // The IdentityService is injected
        app.MapGet(openIdConfigUrl, async ([FromServices] IdentityService identityService, [FromServices] IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            // We need to clone the configuration because we need to change the jwks uri and we don't want to change the original.
            var config = (await identityService.GetExternalOpenIdConfigurationAsync(cancellationToken))!.Clone() as OpenIdConfiguration;
            externalUrl ??= configuration.GetValue<string>("EXTERNAL_URL");

            if (externalUrl is not null)
            {
                config!.JwksUri = new Uri(new Uri(externalUrl), $"{identityPrefix}/jwks").ToString();
            }
            return Results.Ok(config);
        })
            .WithName("GetOpenIDConnectConfiguration")
            .WithTags("OpenID Connect")
            .AddOpenApiOperationTransformer((operation, _, _) =>
        {
            operation.Summary = "openid-configuration";
            operation.Description = "Modified OpenID configuration";
            return Task.CompletedTask;
        })
        .Produces<OpenIdConfiguration>(200);

        // Group all Identity endpoints under the identityPrefix
        var identityApi = app.MapGroup(identityPrefix);

        // Add the jwks endpoint {identityPrefix}/jwks
        // The IdentityService is injected
        identityApi.MapGet("/jwks", async ([FromServices] IdentityService identityService, CancellationToken cancellationToken) =>
        {
            var jwks = await identityService.GetJwksWithExtraSigningCertAsync(cancellationToken);
            return Results.Ok(jwks);
        })
            .WithName("GetAllSigningKeys")
            .WithTags("OpenID Connect")
            .AddOpenApiOperationTransformer((operation, _, _) =>
        {
            operation.Summary = "jwks";
            operation.Description = "Modified JWKS response, copy from IDP with extra cert";
            return Task.CompletedTask;
        })
        .Produces<Jwks>(200);

        // Add the token endpoint {identityPrefix}/token
        // The IdentityService is injected and the TokenRequest is bound from the request body
        identityApi.MapPost("/token", async ([FromBody] TokenRequest request, [FromServices] IdentityService identityService, CancellationToken cancellationToken) =>
        {
            var token = await identityService.GetTokenAsync(request, cancellationToken);
            return Results.Ok(token);
        })
            .WithName("GetToken")
            .WithTags("Tokens")
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                operation.Summary = "Get token";
                operation.Description = "Get a token signed with the extra signing certificate";
                return Task.CompletedTask;
            })
            .Produces<TokenResponse>(200);

        // Add the token endpoint {identityPrefix}/duplicate-token
        // The IdentityService is injected and the token to duplicate is bound from the request body
        identityApi.MapPost("/duplicate-token", async ([FromBody] DuplicateTokenRequest tokenRequest, [FromServices] IdentityService identityService, CancellationToken cancellationToken) =>
        {
            var newToken = await identityService.GetTokenAsync(tokenRequest.Token, cancellationToken);
            if (newToken is null)
            {
                return Results.BadRequest(new { error = "invalid_token", error_description = "The provided token is invalid or could not be duplicated." });
            }
            return Results.Ok(newToken);
        })
            .WithName("CloneToken")
            .WithTags("Tokens")
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                operation.Summary = "Clone token";
                operation.Description = "Get a token signed with the extra signing certificate";
                return Task.CompletedTask;
            })
            .Produces<TokenResponse>(200);
    }
}
