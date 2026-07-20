using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paysky.Identity.Keycloak;

/// <summary>Shared constants for the Admin package.</summary>
internal static class KeycloakAdminDefaults
{
    /// <summary>Named <see cref="System.Net.Http.HttpClient"/> used for all admin/token calls.</summary>
    public const string HttpClientName = "PayskyKeycloakAdmin";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

/// <summary>Keycloak REST endpoint paths, relative to the configured base URL.</summary>
internal static class KeycloakEndpoints
{
    public static string Token(string realm) => $"/realms/{realm}/protocol/openid-connect/token";
    public static string Logout(string realm) => $"/realms/{realm}/protocol/openid-connect/logout";
    public static string UserInfo(string realm) => $"/realms/{realm}/protocol/openid-connect/userinfo";

    public static string Realm(string realm) => $"/admin/realms/{realm}";
    public static string Realms() => "/admin/realms";

    public static string Users(string realm) => $"/admin/realms/{realm}/users";
    public static string User(string realm, string userId) => $"/admin/realms/{realm}/users/{userId}";
    public static string UsersByUsername(string realm, string username)
        => $"/admin/realms/{realm}/users?username={Uri.EscapeDataString(username)}&exact=true";
    public static string UserResetPassword(string realm, string userId) => $"/admin/realms/{realm}/users/{userId}/reset-password";
    public static string UserRealmRoleMappings(string realm, string userId) => $"/admin/realms/{realm}/users/{userId}/role-mappings/realm";
    public static string UsersCount(string realm) => $"/admin/realms/{realm}/users/count";

    public static string Roles(string realm) => $"/admin/realms/{realm}/roles";
    public static string Role(string realm, string roleName) => $"/admin/realms/{realm}/roles/{Uri.EscapeDataString(roleName)}";

    public static string Clients(string realm) => $"/admin/realms/{realm}/clients";
}

/// <summary>A cached admin token with its absolute expiry.</summary>
internal sealed record CachedAdminToken(string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAt);
