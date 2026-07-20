using System.Security.Claims;
using FluentAssertions;
using Xunit;

namespace Paysky.Identity.Keycloak.UnitTests;

public class KeycloakClaimsTransformerTests
{
    private static ClaimsIdentity IdentityWith(params Claim[] claims)
        => new(claims, authenticationType: "test");

    [Fact]
    public void MapRoles_MapsRealmRoles_IntoRoleClaims()
    {
        var identity = IdentityWith(new Claim(KeycloakClaimNames.RealmAccess, "{\"roles\":[\"admin\",\"merchant\"]}"));
        var realm = new KeycloakRealmOptions { ClientId = "c", MapRealmRoles = true };

        KeycloakClaimsTransformer.MapRoles(identity, realm);

        identity.FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Should().BeEquivalentTo("admin", "merchant");
    }

    [Fact]
    public void MapRoles_SkipsRealmRoles_WhenDisabled()
    {
        var identity = IdentityWith(new Claim(KeycloakClaimNames.RealmAccess, "{\"roles\":[\"admin\"]}"));
        var realm = new KeycloakRealmOptions { ClientId = "c", MapRealmRoles = false };

        KeycloakClaimsTransformer.MapRoles(identity, realm);

        identity.FindAll(ClaimTypes.Role).Should().BeEmpty();
    }

    [Fact]
    public void MapRoles_MapsClientRoles_OnlyWhenOptedIn_AndClientMatches()
    {
        var identity = IdentityWith(new Claim(KeycloakClaimNames.ResourceAccess, "{\"my-client\":{\"roles\":[\"reader\"]}}"));
        var realm = new KeycloakRealmOptions { ClientId = "my-client", MapRealmRoles = false, MapClientRoles = true };

        KeycloakClaimsTransformer.MapRoles(identity, realm);

        identity.FindAll(ClaimTypes.Role).Select(c => c.Value).Should().ContainSingle().Which.Should().Be("reader");
    }

    [Fact]
    public void MapRoles_IgnoresClientRoles_ForDifferentClient()
    {
        var identity = IdentityWith(new Claim(KeycloakClaimNames.ResourceAccess, "{\"other-client\":{\"roles\":[\"reader\"]}}"));
        var realm = new KeycloakRealmOptions { ClientId = "my-client", MapRealmRoles = false, MapClientRoles = true };

        KeycloakClaimsTransformer.MapRoles(identity, realm);

        identity.FindAll(ClaimTypes.Role).Should().BeEmpty();
    }

    [Fact]
    public void MapRoles_DoesNotThrow_OnMalformedClaim()
    {
        var identity = IdentityWith(new Claim(KeycloakClaimNames.RealmAccess, "this is not json"));
        var realm = new KeycloakRealmOptions { ClientId = "c", MapRealmRoles = true };

        var act = () => KeycloakClaimsTransformer.MapRoles(identity, realm);

        act.Should().NotThrow();
        identity.FindAll(ClaimTypes.Role).Should().BeEmpty();
    }

    [Fact]
    public void MapRoles_DoesNotDuplicate_ExistingRoleClaim()
    {
        var identity = IdentityWith(
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(KeycloakClaimNames.RealmAccess, "{\"roles\":[\"admin\"]}"));
        var realm = new KeycloakRealmOptions { ClientId = "c", MapRealmRoles = true };

        KeycloakClaimsTransformer.MapRoles(identity, realm);

        identity.FindAll(ClaimTypes.Role).Should().ContainSingle();
    }

    [Fact]
    public void NormalizeTenantClaim_CopiesFirstMatchingSource_ToCanonicalName()
    {
        var identity = IdentityWith(new Claim("tenant_id", "T-123"));
        var tenant = new TenantClaimOptions(); // default sources include tenant_id, canonical X-Tenant

        KeycloakClaimsTransformer.NormalizeTenantClaim(identity, tenant);

        identity.FindFirst("X-Tenant")!.Value.Should().Be("T-123");
    }

    [Fact]
    public void NormalizeTenantClaim_IsNoOp_WhenDisabled()
    {
        var identity = IdentityWith(new Claim("tenant_id", "T-123"));
        var tenant = new TenantClaimOptions { Enabled = false };

        KeycloakClaimsTransformer.NormalizeTenantClaim(identity, tenant);

        identity.FindFirst("X-Tenant").Should().BeNull();
    }

    [Fact]
    public void NormalizeTenantClaim_DoesNotOverwrite_ExistingCanonicalClaim()
    {
        var identity = IdentityWith(
            new Claim("X-Tenant", "EXISTING"),
            new Claim("tenant_id", "T-123"));
        var tenant = new TenantClaimOptions();

        KeycloakClaimsTransformer.NormalizeTenantClaim(identity, tenant);

        identity.FindAll("X-Tenant").Should().ContainSingle().Which.Value.Should().Be("EXISTING");
    }
}
