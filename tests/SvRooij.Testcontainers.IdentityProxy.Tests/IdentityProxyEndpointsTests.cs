using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.IdentityProxy;

namespace SvRooij.Testcontainers.IdentityProxy.Tests;

public class IdentityProxyEndpointsTests
{
    private static IdentityProxyContainer? _container;
    [Before(Class)]
    public static async Task ContainerSetup()
    {
        if (_container == null)
        {
            var builder = new IdentityProxyBuilder("https://login.microsoftonline.com/svrooij.io/v2.0/")
                .WithImage("svrooij/identityproxyapi:test") // Use the test image, make sure this is available locally
                .WithImagePullPolicy((_) => false); // Don't pull the image, use the local one
            _container = builder.Build();
        }
        await _container.StartAsync(TestContext.Current?.Execution.CancellationToken ?? CancellationToken.None);
    }

    [Test]
    public async Task IdentityProxy_Should_provide_correct_authority()
    {
        var authority = _container!.GetAuthority();
        await Assert.That(authority).IsNotNull();
        await Assert.That(authority).StartsWith("http://");
    }

    [Test]
    public async Task IdentityProxy_should_have_response_at_root()
    {
        var authority = _container!.GetAuthority();
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(authority);
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains(@"""authority"":""https://login.microsoftonline.com/svrooij.io/v2.0/""");
    }

    [Test]
    public async Task IdentityProxy_should_provide_token()
    {
        var tokenRequest = new TokenRequest
        {
            Audience = "62eb2412-f410-4e23-95e7-6a91146bc32c",
            Subject = "99f0cbaa-b3bb-4a77-81a5-e8d17b2232ec",
            AdditionalClaims = new()
        };
        tokenRequest.AdditionalClaims.Add("some_claim", "with a value");
        var token = await _container!.GetTokenAsync(tokenRequest, TestContext.Current?.Execution.CancellationToken ?? CancellationToken.None);
        await Assert.That(token).IsNotNull();
        await Assert.That(token.AccessToken).IsNotNull();
        await Assert.That(token.ExpiresIn).IsGreaterThan(0);
    }

    [Test]
    // Complete token
    [Arguments("eyJhbGciOiJSUzI1NiIsImtpZCI6InhsVXBic3RoWmZHX04wUGRzYTRsbW10bU53bF91YlUxWXRtVHVjd1pOSkkiLCJ0eXAiOiJKV1QifQ.eyJhdWQiOiI2MmViMjQxMi1mNDEwLTRlMjMtOTVlNy02YTkxMTQ2YmMzMmMiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vZGY2OGFhMDMtNDhlYi00YjA5LTlmM2UtOGFlY2M1OGUyMDdjL3YyLjAiLCJleHAiOjE3NjA0NzU1ODAsImlhdCI6MTc2MDQ3MTk4MCwibmJmIjoxNzYwNDcxOTcwLCJzdWIiOiI5OWYwY2JhYS1iM2JiLTRhNzctODFhNS1lOGQxN2IyMjMyZWMifQ.NxZxI1IIqyFQVIq9ETI3XSvGxpX76jwPOXODD83dLU1a8ALrlQy6biTJ-qO98KiRlo4xAf5RxNirX6AACSiVbiLzEH_Usm2G3LM2EMZsGDIlX86cFNMAI-_MCo9TZWUquUY_hQfyA4HROEpkcL9K5ioRcq496BpKbyywOlAV03bFpdkiGirbE6ysbqt-aoDKrYo-uASyplsoGRs0FDZPAcR1n7Vb8XSM_bdMjCa_6fmScwXmC3vtfUnZHYxXe87MOPJS7ivkoHXHDaV1Y-nBIAC6vPaeJZOhqRwej-byva1IsVZfLqCZA9noKnEcYfaBVBDkrqXggApKs8IssRh5VA")]
    // Stripped part of signature
    [Arguments("eyJhbGciOiJSUzI1NiIsImtpZCI6InhsVXBic3RoWmZHX04wUGRzYTRsbW10bU53bF91YlUxWXRtVHVjd1pOSkkiLCJ0eXAiOiJKV1QifQ.eyJhdWQiOiI2MmViMjQxMi1mNDEwLTRlMjMtOTVlNy02YTkxMTQ2YmMzMmMiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vZGY2OGFhMDMtNDhlYi00YjA5LTlmM2UtOGFlY2M1OGUyMDdjL3YyLjAiLCJleHAiOjE3NjA0NzU1ODAsImlhdCI6MTc2MDQ3MTk4MCwibmJmIjoxNzYwNDcxOTcwLCJzdWIiOiI5OWYwY2JhYS1iM2JiLTRhNzctODFhNS1lOGQxN2IyMjMyZWMifQ.NxZxI1IIqyFQVIq9ETI3XSvGxpX76jwPOXODD83dLU1a8ALrlQy6biTJ-qO98KiRlo4xAf5RxNirX6AACSiVbiLzEH_Usm2G3LM2EMZsGDIlX86cFNMAI-_MCo9TZWUquUY_hQfyA4HROEpkcL9K5ioRcq496BpKbyywOlAV03bFpdkiGirbE6ysbqt-aoDKrY")]
    // Stripped signature and last dot.
    [Arguments("eyJhbGciOiJSUzI1NiIsImtpZCI6InhsVXBic3RoWmZHX04wUGRzYTRsbW10bU53bF91YlUxWXRtVHVjd1pOSkkiLCJ0eXAiOiJKV1QifQ.eyJhdWQiOiI2MmViMjQxMi1mNDEwLTRlMjMtOTVlNy02YTkxMTQ2YmMzMmMiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vZGY2OGFhMDMtNDhlYi00YjA5LTlmM2UtOGFlY2M1OGUyMDdjL3YyLjAiLCJleHAiOjE3NjA0NzU1ODAsImlhdCI6MTc2MDQ3MTk4MCwibmJmIjoxNzYwNDcxOTcwLCJzdWIiOiI5OWYwY2JhYS1iM2JiLTRhNzctODFhNS1lOGQxN2IyMjMyZWMifQ")]
    public async Task IdentityProxy_should_duplicate_token(string inputToken)
    {
        var token = await _container!.DuplicateTokenAsync(inputToken, TestContext.Current?.Execution.CancellationToken ?? CancellationToken.None);
        await Assert.That(token).IsNotNull();
        await Assert.That(token.AccessToken).IsNotNull();
    }

    [Test]
    public async Task IdentityProxy_should_provide_jwks_response()
    {
        var jwksUri = $"{_container!.GetAuthority()}api/identity/jwks";
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(jwksUri);
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains(@"""keys"":");
    }

    [Test]
    public async Task IdentityProxy_should_provide_openid_configuration_response()
    {
        var openIdUri = $"{_container!.GetAuthority()}.well-known/openid-configuration";
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(openIdUri);
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains(@"""issuer"":");
        await Assert.That(content).Contains(@"""jwks_uri"":");
    }

    [After(Class)]
    public static async Task Cleanup()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
            _container = null;
        }
    }

}
