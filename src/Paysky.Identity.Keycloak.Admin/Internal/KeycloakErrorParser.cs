using System.Text.Json;

namespace Paysky.Identity.Keycloak;

/// <summary>
/// Keycloak admin errors return a JSON body such as <c>{"errorMessage":"..."}</c> or
/// <c>{"error":"..."}</c>. This extracts the human-readable message so raw JSON never surfaces to callers.
/// </summary>
internal static class KeycloakErrorParser
{
    public static string Extract(string? rawError, string fallback)
    {
        if (string.IsNullOrWhiteSpace(rawError))
            return fallback;

        try
        {
            using var doc = JsonDocument.Parse(rawError);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("errorMessage", out var m) && m.ValueKind == JsonValueKind.String)
                    return m.GetString() ?? fallback;
                if (doc.RootElement.TryGetProperty("error_description", out var d) && d.ValueKind == JsonValueKind.String)
                    return d.GetString() ?? fallback;
                if (doc.RootElement.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String)
                    return e.GetString() ?? fallback;
            }
        }
        catch (JsonException)
        {
            // Not JSON — return the raw text as-is.
            return rawError!;
        }

        return rawError!;
    }
}
