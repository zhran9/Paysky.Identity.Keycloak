using System.Security.Claims;
using System.Text.Json;

namespace Paysky.Identity.Keycloak;

/// <summary>
/// Maps Keycloak-specific token claims onto a <see cref="ClaimsIdentity"/> after validation:
/// realm roles (<c>realm_access.roles</c>), optional client roles
/// (<c>resource_access.&lt;clientId&gt;.roles</c>), and a normalized tenant claim.
/// Pure and side-effect-free apart from mutating the supplied identity — unit-testable without ASP.NET.
/// </summary>
public static class KeycloakClaimsTransformer
{
    /// <summary>Adds realm and (optionally) client roles as <see cref="ClaimTypes.Role"/> claims.</summary>
    public static void MapRoles(ClaimsIdentity identity, KeycloakRealmOptions realm)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(realm);

        if (realm.MapRealmRoles)
        {
            var realmAccess = identity.FindFirst(KeycloakClaimNames.RealmAccess)?.Value;
            AddRolesFromContainer(identity, realmAccess);
        }

        if (realm.MapClientRoles)
        {
            var resourceAccess = identity.FindFirst(KeycloakClaimNames.ResourceAccess)?.Value;
            AddClientRoles(identity, resourceAccess, realm.ClientId);
        }
    }

    /// <summary>Copies the first matching tenant source claim into a single canonical claim.</summary>
    public static void NormalizeTenantClaim(ClaimsIdentity identity, TenantClaimOptions tenant)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(tenant);

        if (!tenant.Enabled)
            return;

        // Already normalized (e.g. token issued the canonical claim directly) — leave it.
        if (identity.FindFirst(tenant.CanonicalName) is not null)
            return;

        foreach (var source in tenant.SourceNames)
        {
            var value = identity.FindFirst(source)?.Value;
            if (!string.IsNullOrEmpty(value))
            {
                identity.AddClaim(new Claim(tenant.CanonicalName, value!));
                return;
            }
        }
    }

    private static void AddRolesFromContainer(ClaimsIdentity identity, string? containerJson)
    {
        if (string.IsNullOrEmpty(containerJson))
            return;

        try
        {
            using var doc = JsonDocument.Parse(containerJson);
            AddRoles(identity, doc.RootElement);
        }
        catch (JsonException)
        {
            // Malformed claim — never let it break authentication.
        }
    }

    private static void AddClientRoles(ClaimsIdentity identity, string? resourceAccessJson, string clientId)
    {
        if (string.IsNullOrEmpty(resourceAccessJson) || string.IsNullOrEmpty(clientId))
            return;

        try
        {
            using var doc = JsonDocument.Parse(resourceAccessJson);
            if (doc.RootElement.TryGetProperty(clientId, out var client))
                AddRoles(identity, client);
        }
        catch (JsonException)
        {
        }
    }

    private static void AddRoles(ClaimsIdentity identity, JsonElement container)
    {
        if (container.ValueKind != JsonValueKind.Object
            || !container.TryGetProperty(KeycloakClaimNames.Roles, out var roles)
            || roles.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var role in roles.EnumerateArray())
        {
            var value = role.GetString();
            if (!string.IsNullOrEmpty(value) && !identity.HasClaim(ClaimTypes.Role, value!))
                identity.AddClaim(new Claim(ClaimTypes.Role, value!));
        }
    }
}
