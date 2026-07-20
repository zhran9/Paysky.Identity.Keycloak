using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Paysky.Identity.Keycloak.UnitTests;

public class KeycloakTokenProviderTests
{
    private static IOptions<KeycloakOptions> Options(string grantType) => Microsoft.Extensions.Options.Options.Create(
        new KeycloakOptions
        {
            BaseUrl = "http://keycloak:8080",
            Admin = new KeycloakAdminOptions
            {
                GrantType = grantType,
                ClientId = "svc-client",
                ClientSecret = "svc-secret",
                Username = "admin",
                Password = "pwd",
                TokenRealm = "master",
                TokenExpiryBufferSeconds = 30
            }
        });

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsAccessToken()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json("{\"access_token\":\"tok-1\",\"expires_in\":300}"));
        var provider = new KeycloakTokenProvider(FactoryFor(handler), Options(KeycloakGrantTypes.ClientCredentials));

        var token = await provider.GetAccessTokenAsync();

        token.Should().Be("tok-1");
    }

    [Fact]
    public async Task GetAccessTokenAsync_CachesToken_AcrossCalls()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json("{\"access_token\":\"tok-1\",\"expires_in\":300}"));
        var provider = new KeycloakTokenProvider(FactoryFor(handler), Options(KeycloakGrantTypes.ClientCredentials));

        var first = await provider.GetAccessTokenAsync();
        var second = await provider.GetAccessTokenAsync();

        first.Should().Be("tok-1");
        second.Should().Be("tok-1");
        handler.RequestCount.Should().Be(1, "the token is cached until it nears expiry");
    }

    [Fact]
    public async Task GetAccessTokenAsync_UsesClientCredentialsGrant_ByDefault()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json("{\"access_token\":\"tok-1\",\"expires_in\":300}"));
        var provider = new KeycloakTokenProvider(FactoryFor(handler), Options(KeycloakGrantTypes.ClientCredentials));

        await provider.GetAccessTokenAsync();

        handler.LastRequestBody.Should().Contain("grant_type=client_credentials");
        handler.LastRequestBody.Should().Contain("client_secret=svc-secret");
    }

    [Fact]
    public async Task GetAccessTokenAsync_UsesPasswordGrant_WhenConfigured()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json("{\"access_token\":\"tok-1\",\"expires_in\":300}"));
        var provider = new KeycloakTokenProvider(FactoryFor(handler), Options(KeycloakGrantTypes.Password));

        await provider.GetAccessTokenAsync();

        handler.LastRequestBody.Should().Contain("grant_type=password");
        handler.LastRequestBody.Should().Contain("username=admin");
    }

    private static StubHttpClientFactory FactoryFor(StubHttpMessageHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://keycloak:8080") });
}
