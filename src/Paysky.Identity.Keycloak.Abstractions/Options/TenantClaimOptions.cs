namespace Paysky.Identity.Keycloak;

/// <summary>
/// Normalizes the tenant identifier claim. Keycloak realms across the estate emit the tenant id
/// under different names (<c>tenantId</c>, <c>tenant_id</c>, <c>tid</c>, <c>TenantId</c>); the first
/// match from <see cref="SourceNames"/> is copied into a single <see cref="CanonicalName"/> claim.
/// </summary>
public sealed class TenantClaimOptions
{
    /// <summary>Candidate source claim names, tried in order. First non-empty wins.</summary>
    public IList<string> SourceNames { get; set; } = new List<string> { "tenantId", "tenant_id", "tid", "TenantId" };

    /// <summary>The single claim name the source value is copied to. Consumers read only this.</summary>
    public string CanonicalName { get; set; } = "X-Tenant";

    /// <summary>When false, no tenant-claim normalization is performed.</summary>
    public bool Enabled { get; set; } = true;
}
