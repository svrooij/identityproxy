using IdentityProxy.Api.Identity.Models;

namespace IdentityProxy.Api.Identity;

internal static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this WebApplication app, string openIdConfigUrl = "/.well-known/openid-configuration", string identityPrefix = "/api/identity")
    {
        app.MapGet(openIdConfigUrl, async (IdentityService identityService, CancellationToken cancellationToken) =>
        {
            var config = await identityService.GetExternalOpenIdConfigurationAsync(cancellationToken);
            var rootUrl = Environment.GetEnvironmentVariable("EXTERNAL_URL");
            if (rootUrl is not null)
            {
                config!.JwksUri = new Uri(new Uri(rootUrl), $"{identityPrefix}/jwks").ToString();
            }
            return Results.Ok(config);
        });

        var identityApi = app.MapGroup(identityPrefix);
        identityApi.MapGet("/jwks", async (IdentityService identityService, CancellationToken cancellationToken) =>
        {
            var jwks = await identityService.GetJwksWithExtraSigningCertAsync(cancellationToken);
            
            return Results.Ok(jwks);
        });

        identityApi.MapPost("/token", async (IdentityService identityService, TokenRequest request, CancellationToken cancellationToken) =>
        {
            var token = await identityService.GetTokenAsync(request, cancellationToken);
            return Results.Ok(token);
        });
    }
}
