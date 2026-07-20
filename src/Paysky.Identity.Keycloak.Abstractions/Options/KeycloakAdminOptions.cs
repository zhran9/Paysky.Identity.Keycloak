namespace Paysky.Identity.Keycloak;

/// <summary>
/// Configuration for the Keycloak Admin REST client (user/role/realm management).
/// Defaults to the <c>client_credentials</c> service-account grant; ROPC (<c>password</c>) is opt-in.
/// </summary>
public sealed class KeycloakAdminOptions
{
    /// <summary>Grant used to obtain the admin access token. See <see cref="KeycloakGrantTypes"/>. Default client_credentials.</summary>
    public string GrantType { get; set; } = KeycloakGrantTypes.ClientCredentials;

    /// <summary>Admin client id (a confidential, service-account-enabled client for client_credentials).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Admin client secret. Required for client_credentials; bind from a secret store, never a literal.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Admin username. Required only for the <c>password</c> grant.</summary>
    public string? Username { get; set; }

    /// <summary>Admin password. Required only for the <c>password</c> grant; bind from a secret store.</summary>
    public string? Password { get; set; }

    /// <summary>Realm the admin token is obtained from (typically <c>master</c>).</summary>
    public string TokenRealm { get; set; } = "master";

    /// <summary>Default realm admin operations target when a call does not specify one.</summary>
    public string ManagedRealm { get; set; } = string.Empty;

    /// <summary>Seconds subtracted from token lifetime before treating it as expired. Default 30.</summary>
    public int TokenExpiryBufferSeconds { get; set; } = 30;
}
