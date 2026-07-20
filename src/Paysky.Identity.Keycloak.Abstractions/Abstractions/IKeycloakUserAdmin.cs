namespace Paysky.Identity.Keycloak;

/// <summary>
/// Keycloak Admin REST operations for user lifecycle management. The <c>realm</c> parameter is
/// optional on every call; when null/empty the configured <see cref="KeycloakAdminOptions.ManagedRealm"/> is used.
/// </summary>
public interface IKeycloakUserAdmin
{
    Task<KeycloakResult<string>> CreateUserAsync(CreateKeycloakUserRequest request, string? realm = null, CancellationToken cancellationToken = default);

    Task<KeycloakResult<KeycloakUser>> GetUserByIdAsync(string userId, string? realm = null, CancellationToken cancellationToken = default);

    Task<KeycloakResult<KeycloakUser>> GetUserByUsernameAsync(string username, string? realm = null, CancellationToken cancellationToken = default);

    Task<KeycloakResult> UpdateUserAsync(string userId, UpdateKeycloakUserRequest request, string? realm = null, CancellationToken cancellationToken = default);

    Task<KeycloakResult> DeleteUserAsync(string userId, string? realm = null, CancellationToken cancellationToken = default);

    Task<KeycloakResult> ResetPasswordAsync(string userId, string newPassword, bool temporary = false, string? realm = null, CancellationToken cancellationToken = default);

    Task<KeycloakResult> AddRequiredActionAsync(string userId, string requiredAction, string? realm = null, CancellationToken cancellationToken = default);

    Task<KeycloakResult> RemoveRequiredActionAsync(string userId, string requiredAction, string? realm = null, CancellationToken cancellationToken = default);

    Task<KeycloakResult> AssignRealmRoleAsync(string userId, string roleName, string? realm = null, CancellationToken cancellationToken = default);

    Task<KeycloakResult<int>> GetUserCountAsync(string? realm = null, string? search = null, CancellationToken cancellationToken = default);
}
