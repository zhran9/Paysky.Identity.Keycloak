namespace Paysky.Identity.Keycloak;

/// <summary>An access/refresh token pair returned by a login or refresh exchange.</summary>
/// <param name="AccessToken">The bearer access token.</param>
/// <param name="RefreshToken">The refresh token, when issued.</param>
/// <param name="ExpiresInSeconds">Access-token lifetime in seconds.</param>
public record TokenPair(string AccessToken, string? RefreshToken, int ExpiresInSeconds);
