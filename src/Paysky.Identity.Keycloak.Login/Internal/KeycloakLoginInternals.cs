using System.Text.Json;

namespace Paysky.Identity.Keycloak;

internal static class KeycloakLoginDefaults
{
    public const string HttpClientName = "PayskyKeycloakLogin";

    public static string Token(string realm) => $"/realms/{realm}/protocol/openid-connect/token";
    public static string Logout(string realm) => $"/realms/{realm}/protocol/openid-connect/logout";
    public static string UserInfo(string realm) => $"/realms/{realm}/protocol/openid-connect/userinfo";
}

/// <summary>Extracts a human-readable message from a Keycloak OIDC error body.</summary>
internal static class KeycloakLoginErrorParser
{
    public static string Extract(string? raw, string fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("error_description", out var d) && d.ValueKind == JsonValueKind.String)
                    return d.GetString() ?? fallback;
                if (doc.RootElement.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String)
                    return e.GetString() ?? fallback;
            }
        }
        catch (JsonException)
        {
            return raw!;
        }

        return fallback;
    }
}
