namespace Paysky.Identity.Keycloak;

/// <summary>Well-known Keycloak JWT claim names consumed during token validation and mapping.</summary>
public static class KeycloakClaimNames
{
    /// <summary>Container claim holding realm-level roles: <c>{ "roles": [ ... ] }</c>.</summary>
    public const string RealmAccess = "realm_access";

    /// <summary>Container claim holding per-client roles: <c>{ "&lt;clientId&gt;": { "roles": [ ... ] } }</c>.</summary>
    public const string ResourceAccess = "resource_access";

    /// <summary>Property name of the roles array inside <see cref="RealmAccess"/> / <see cref="ResourceAccess"/>.</summary>
    public const string Roles = "roles";

    /// <summary>Standard OIDC issuer claim.</summary>
    public const string Issuer = "iss";

    /// <summary>Keycloak's default username claim.</summary>
    public const string PreferredUsername = "preferred_username";
}
