namespace Paysky.Identity.Keycloak;

/// <summary>
/// Brokers end-user login against Keycloak (Resource Owner Password flow) for services that
/// authenticate users directly rather than redirecting to Keycloak's login page.
/// </summary>
public interface IKeycloakLoginBroker
{
    /// <summary>Exchanges username/password for a token pair.</summary>
    Task<KeycloakResult<TokenPair>> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>Exchanges a refresh token for a new token pair.</summary>
    Task<KeycloakResult<TokenPair>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes the session associated with the given access token.</summary>
    Task<KeycloakResult> LogoutAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>Resolves the user profile for an access token via the <c>userinfo</c> endpoint.</summary>
    Task<KeycloakResult<KeycloakUserInfo>> GetUserFromTokenAsync(string accessToken, CancellationToken cancellationToken = default);
}
