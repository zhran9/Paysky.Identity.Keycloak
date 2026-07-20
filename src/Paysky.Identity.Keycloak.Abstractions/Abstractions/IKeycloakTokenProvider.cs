namespace Paysky.Identity.Keycloak;

/// <summary>
/// Supplies a valid Keycloak admin/service access token, caching and refreshing it transparently.
/// Registered as a singleton; implementations must be thread-safe.
/// </summary>
public interface IKeycloakTokenProvider
{
    /// <summary>Returns a currently-valid access token, acquiring or refreshing as needed.</summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
