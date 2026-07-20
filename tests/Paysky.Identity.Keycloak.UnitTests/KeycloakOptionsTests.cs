using FluentAssertions;
using Xunit;

namespace Paysky.Identity.Keycloak.UnitTests;

public class KeycloakOptionsTests
{
    [Fact]
    public void BuildAuthority_TrimsTrailingSlash_AndAppendsRealm()
    {
        var options = new KeycloakOptions { BaseUrl = "https://idp.paysky.internal/" };

        options.BuildAuthority("apm-admin").Should().Be("https://idp.paysky.internal/realms/apm-admin");
    }

    [Fact]
    public void BuildMetadataAddress_PointsAtDiscoveryDocument()
    {
        var options = new KeycloakOptions { BaseUrl = "https://idp.paysky.internal" };

        options.BuildMetadataAddress("qrswitch")
            .Should().Be("https://idp.paysky.internal/realms/qrswitch/.well-known/openid-configuration");
    }

    [Fact]
    public void ResolveSchemeName_FallsBackToRealmName()
    {
        var realm = new KeycloakRealmOptions { Name = "qrswitch", ClientId = "c" };

        realm.ResolveSchemeName().Should().Be("qrswitch");
    }

    [Fact]
    public void ResolveSchemeName_UsesExplicitSchemeName_WhenSet()
    {
        var realm = new KeycloakRealmOptions { Name = "apm-merchant", SchemeName = "merchant", ClientId = "c" };

        realm.ResolveSchemeName().Should().Be("merchant");
    }

    [Fact]
    public void ResolveAudience_FallsBackToClientId()
    {
        var realm = new KeycloakRealmOptions { Name = "r", ClientId = "my-api" };

        realm.ResolveAudience().Should().Be("my-api");
    }

    [Fact]
    public void ResolveAudience_UsesExplicitAudience_WhenSet()
    {
        var realm = new KeycloakRealmOptions { Name = "r", ClientId = "my-api", Audience = "custom-aud" };

        realm.ResolveAudience().Should().Be("custom-aud");
    }
}
