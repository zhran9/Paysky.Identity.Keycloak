namespace Paysky.Identity.Keycloak;

/// <summary>OAuth2 grant type identifiers used against Keycloak's token endpoint.</summary>
public static class KeycloakGrantTypes
{
    public const string Password = "password";
    public const string RefreshToken = "refresh_token";
    public const string ClientCredentials = "client_credentials";
}
