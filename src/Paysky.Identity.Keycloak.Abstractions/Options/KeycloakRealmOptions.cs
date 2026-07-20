namespace Paysky.Identity.Keycloak;

/// <summary>
/// Configuration for a single Keycloak realm this service accepts tokens from.
/// One entry = single-realm mode; two or more = multi-realm mode (issuer-based scheme selection).
/// </summary>
public sealed class KeycloakRealmOptions
{
    /// <summary>
    /// Realm name, e.g. <c>qrswitch</c> or <c>apm-admin</c>.
    /// Convention: lowercase, begins with the project slug; append an audience suffix only when a
    /// project needs more than one realm.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Authentication scheme name for this realm in multi-realm mode. Defaults to <see cref="Name"/>.
    /// Ignored in single-realm mode (the default JwtBearer scheme is used).
    /// </summary>
    public string? SchemeName { get; set; }

    /// <summary>The client id tokens for this realm are issued to. Also the default expected audience.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Expected token audience. Defaults to <see cref="ClientId"/> when not set.
    /// The generic Keycloak <c>account</c> audience is intentionally NOT accepted unless listed
    /// explicitly in <see cref="AdditionalAudiences"/>.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>Extra audiences to accept, opt-in only. Empty by default.</summary>
    public IList<string> AdditionalAudiences { get; set; } = new List<string>();

    /// <summary>Map <c>realm_access.roles</c> into <see cref="System.Security.Claims.ClaimTypes.Role"/> claims. Default true.</summary>
    public bool MapRealmRoles { get; set; } = true;

    /// <summary>Map <c>resource_access.&lt;ClientId&gt;.roles</c> into role claims. Opt-in, default false.</summary>
    public bool MapClientRoles { get; set; }

    /// <summary>Resolves the effective scheme name (falls back to <see cref="Name"/>).</summary>
    public string ResolveSchemeName() => string.IsNullOrWhiteSpace(SchemeName) ? Name : SchemeName!;

    /// <summary>Resolves the effective audience (falls back to <see cref="ClientId"/>).</summary>
    public string ResolveAudience() => string.IsNullOrWhiteSpace(Audience) ? ClientId : Audience!;
}
