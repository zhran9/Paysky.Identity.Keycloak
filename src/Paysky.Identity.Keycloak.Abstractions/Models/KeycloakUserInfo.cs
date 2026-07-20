namespace Paysky.Identity.Keycloak;

/// <summary>User profile resolved from the Keycloak <c>userinfo</c> endpoint or an admin lookup.</summary>
/// <param name="Id">Keycloak user id (subject).</param>
/// <param name="Username">Preferred username.</param>
/// <param name="Email">Email address.</param>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name.</param>
/// <param name="Roles">Realm/client roles carried on the token, when available.</param>
public record KeycloakUserInfo(
    string Id,
    string Username,
    string? Email,
    string? FirstName,
    string? LastName,
    IReadOnlyCollection<string> Roles);
