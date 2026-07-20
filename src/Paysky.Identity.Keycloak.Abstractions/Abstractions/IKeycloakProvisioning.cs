namespace Paysky.Identity.Keycloak;

/// <summary>Keycloak Admin REST operations for realm/client provisioning (bootstrap-time use).</summary>
public interface IKeycloakProvisioning
{
    /// <summary>Creates the realm if it does not already exist. Idempotent.</summary>
    Task<KeycloakResult> EnsureRealmAsync(string realm, CancellationToken cancellationToken = default);

    /// <summary>Creates a confidential, service-account-enabled client and returns its internal id (uuid).</summary>
    Task<KeycloakResult<string>> CreateConfidentialClientAsync(string clientId, string realm, CancellationToken cancellationToken = default);
}
