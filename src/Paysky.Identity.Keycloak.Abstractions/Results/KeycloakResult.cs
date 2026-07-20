using System.Net;

namespace Paysky.Identity.Keycloak;

/// <summary>
/// Outcome of a Keycloak admin operation that returns no payload. Carries a parsed error message
/// (never raw Keycloak JSON) and the HTTP status for diagnostics.
/// </summary>
public record KeycloakResult(
    bool Success,
    string? ErrorMessage = null,
    HttpStatusCode StatusCode = HttpStatusCode.OK)
{
    /// <summary>A successful result.</summary>
    public static KeycloakResult Ok(HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(true, null, statusCode);

    /// <summary>A failed result with a human-readable message.</summary>
    public static KeycloakResult Fail(string errorMessage, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        => new(false, errorMessage, statusCode);
}

/// <summary>
/// Outcome of a Keycloak admin operation that returns a typed payload on success.
/// </summary>
/// <typeparam name="T">Payload type returned when <see cref="Success"/> is true.</typeparam>
public record KeycloakResult<T>(
    bool Success,
    T? Data = default,
    string? ErrorMessage = null,
    HttpStatusCode StatusCode = HttpStatusCode.OK)
{
    /// <summary>A successful result carrying <paramref name="data"/>.</summary>
    public static KeycloakResult<T> Ok(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(true, data, null, statusCode);

    /// <summary>A failed result with a human-readable message and no payload.</summary>
    public static KeycloakResult<T> Fail(string errorMessage, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        => new(false, default, errorMessage, statusCode);
}
