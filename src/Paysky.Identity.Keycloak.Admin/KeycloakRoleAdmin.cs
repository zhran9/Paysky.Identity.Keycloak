using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Paysky.Identity.Keycloak;

/// <inheritdoc cref="IKeycloakRoleAdmin"/>
internal sealed class KeycloakRoleAdmin : KeycloakAdminServiceBase, IKeycloakRoleAdmin
{
    public KeycloakRoleAdmin(IHttpClientFactory httpClientFactory, IKeycloakTokenProvider tokenProvider, IOptions<KeycloakOptions> options)
        : base(httpClientFactory, tokenProvider, options)
    {
    }

    public async Task<KeycloakResult> CreateRoleAsync(string roleName, string? description = null, string? realm = null, CancellationToken cancellationToken = default)
    {
        var body = new { name = roleName, description };
        using var response = await SendAsync(HttpMethod.Post, KeycloakEndpoints.Roles(ResolveRealm(realm)), body, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode ? KeycloakResult.Ok(response.StatusCode) : await FailAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<KeycloakResult<KeycloakRole>> GetRoleByNameAsync(string roleName, string? realm = null, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, KeycloakEndpoints.Role(ResolveRealm(realm), roleName), null, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return await FailAsync<KeycloakRole>(response, cancellationToken).ConfigureAwait(false);

        using var doc = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
        return KeycloakResult<KeycloakRole>.Ok(MapRole(doc.RootElement));
    }

    public async Task<KeycloakResult<IReadOnlyList<KeycloakRole>>> GetAllRolesAsync(string? realm = null, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, KeycloakEndpoints.Roles(ResolveRealm(realm)), null, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return await FailAsync<IReadOnlyList<KeycloakRole>>(response, cancellationToken).ConfigureAwait(false);

        using var doc = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
        return KeycloakResult<IReadOnlyList<KeycloakRole>>.Ok(MapRoles(doc.RootElement));
    }

    public async Task<KeycloakResult> DeleteRoleAsync(string roleName, string? realm = null, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, KeycloakEndpoints.Role(ResolveRealm(realm), roleName), null, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode ? KeycloakResult.Ok(response.StatusCode) : await FailAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<KeycloakResult<IReadOnlyList<KeycloakRole>>> GetRolesForUserAsync(string userId, string? realm = null, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, KeycloakEndpoints.UserRealmRoleMappings(ResolveRealm(realm), userId), null, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return await FailAsync<IReadOnlyList<KeycloakRole>>(response, cancellationToken).ConfigureAwait(false);

        using var doc = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
        return KeycloakResult<IReadOnlyList<KeycloakRole>>.Ok(MapRoles(doc.RootElement));
    }

    private static IReadOnlyList<KeycloakRole> MapRoles(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
            return Array.Empty<KeycloakRole>();

        return element.EnumerateArray().Select(MapRole).ToList();
    }

    private static KeycloakRole MapRole(JsonElement element) => new()
    {
        Id = GetString(element, "id"),
        Name = GetString(element, "name"),
        Description = GetStringOrNull(element, "description"),
        Composite = GetBool(element, "composite")
    };
}
