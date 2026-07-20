namespace Paysky.Identity.Keycloak;

/// <summary>
/// Keycloak Admin REST operations for realm-role management. The <c>realm</c> parameter is optional;
/// when null/empty the configured <see cref="KeycloakAdminOptions.ManagedRealm"/> is used.
/// </summary>
public interface IKeycloakRoleAdmin
{
    Task<KeycloakResult> CreateRoleAsync(string roleName, string? description = null, string? realm = null, CancellationToken cancellationToken = default);

    Task<KeycloakResult<KeycloakRole>> GetRoleByNameAsync(string roleName, string? realm = null, CancellationToken cancellationToken = default);

    Task<KeycloakResult<IReadOnlyList<KeycloakRole>>> GetAllRolesAsync(string? realm = null, CancellationToken cancellationToken = default);

    Task<KeycloakResult> DeleteRoleAsync(string roleName, string? realm = null, CancellationToken cancellationToken = default);

    Task<KeycloakResult<IReadOnlyList<KeycloakRole>>> GetRolesForUserAsync(string userId, string? realm = null, CancellationToken cancellationToken = default);
}
