namespace Paysky.Identity.Keycloak;

/// <summary>
/// Root configuration bound from the <c>Keycloak</c> configuration section. Shared by every
/// Paysky.Identity.Keycloak package; each package reads only the parts it needs.
/// </summary>
public sealed class KeycloakOptions
{
    /// <summary>Default configuration section name.</summary>
    public const string SectionName = "Keycloak";

    /// <summary>Keycloak base URL including scheme, e.g. <c>https://idp.paysky.internal</c>.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Require HTTPS for metadata/JWKS retrieval. Secure by default (true); override to false only
    /// for local development.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Realms this service accepts tokens from. One = single-realm, many = multi-realm.</summary>
    public IList<KeycloakRealmOptions> Realms { get; set; } = new List<KeycloakRealmOptions>();

    /// <summary>Tenant-claim normalization settings.</summary>
    public TenantClaimOptions TenantClaim { get; set; } = new();

    /// <summary>Admin REST client settings (only for services using the Admin package).</summary>
    public KeycloakAdminOptions? Admin { get; set; }

    /// <summary>Login broker settings (only for services using the Login package).</summary>
    public KeycloakLoginOptions? Login { get; set; }

    /// <summary>Builds the OIDC authority URL for a realm: <c>{BaseUrl}/realms/{realm}</c>.</summary>
    public string BuildAuthority(string realm)
        => $"{BaseUrl.TrimEnd('/')}/realms/{realm}";

    /// <summary>Builds the OIDC discovery metadata URL for a realm.</summary>
    public string BuildMetadataAddress(string realm)
        => $"{BuildAuthority(realm)}/.well-known/openid-configuration";
}
