namespace Paysky.Identity.Keycloak;

/// <summary>
/// Configuration for the user login/session broker (Resource Owner Password flow against the app client).
/// Used only by services that broker end-user login themselves rather than redirecting to Keycloak.
/// </summary>
public sealed class KeycloakLoginOptions
{
    /// <summary>Realm end users authenticate against.</summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>The public-facing app client id used for the login token exchange.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client secret for a confidential login client; bind from a secret store, never a literal.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Scopes requested at login.</summary>
    public string DefaultScopes { get; set; } = "openid profile email";
}
