using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Paysky.Identity.Keycloak;

/// <inheritdoc cref="IKeycloakUserAdmin"/>
internal sealed class KeycloakUserAdmin : KeycloakAdminServiceBase, IKeycloakUserAdmin
{
    public KeycloakUserAdmin(IHttpClientFactory httpClientFactory, IKeycloakTokenProvider tokenProvider, IOptions<KeycloakOptions> options)
        : base(httpClientFactory, tokenProvider, options)
    {
    }

    public async Task<KeycloakResult<string>> CreateUserAsync(CreateKeycloakUserRequest request, string? realm = null, CancellationToken cancellationToken = default)
    {
        var body = BuildCreateBody(request);
        using var response = await SendAsync(HttpMethod.Post, KeycloakEndpoints.Users(ResolveRealm(realm)), body, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return await FailAsync<string>(response, cancellationToken).ConfigureAwait(false);

        var id = response.Headers.Location?.ToString().Split('/').LastOrDefault();
        return string.IsNullOrEmpty(id)
            ? KeycloakResult<string>.Fail("User created but its id could not be resolved from the response.", response.StatusCode)
            : KeycloakResult<string>.Ok(id!, response.StatusCode);
    }

    public async Task<KeycloakResult<KeycloakUser>> GetUserByIdAsync(string userId, string? realm = null, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, KeycloakEndpoints.User(ResolveRealm(realm), userId), null, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return await FailAsync<KeycloakUser>(response, cancellationToken).ConfigureAwait(false);

        using var doc = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
        return KeycloakResult<KeycloakUser>.Ok(MapUser(doc.RootElement));
    }

    public async Task<KeycloakResult<KeycloakUser>> GetUserByUsernameAsync(string username, string? realm = null, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, KeycloakEndpoints.UsersByUsername(ResolveRealm(realm), username), null, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return await FailAsync<KeycloakUser>(response, cancellationToken).ConfigureAwait(false);

        using var doc = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            return KeycloakResult<KeycloakUser>.Fail($"User '{username}' not found.", System.Net.HttpStatusCode.NotFound);

        return KeycloakResult<KeycloakUser>.Ok(MapUser(doc.RootElement[0]));
    }

    public async Task<KeycloakResult> UpdateUserAsync(string userId, UpdateKeycloakUserRequest request, string? realm = null, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?>();
        if (request.Email is not null) body["email"] = request.Email;
        if (request.FirstName is not null) body["firstName"] = request.FirstName;
        if (request.LastName is not null) body["lastName"] = request.LastName;
        if (request.Enabled.HasValue) body["enabled"] = request.Enabled.Value;

        if (body.Count == 0)
            return KeycloakResult.Ok();

        using var response = await SendAsync(HttpMethod.Put, KeycloakEndpoints.User(ResolveRealm(realm), userId), body, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode ? KeycloakResult.Ok(response.StatusCode) : await FailAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<KeycloakResult> DeleteUserAsync(string userId, string? realm = null, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, KeycloakEndpoints.User(ResolveRealm(realm), userId), null, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode ? KeycloakResult.Ok(response.StatusCode) : await FailAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<KeycloakResult> ResetPasswordAsync(string userId, string newPassword, bool temporary = false, string? realm = null, CancellationToken cancellationToken = default)
    {
        var body = new { type = "password", value = newPassword, temporary };
        using var response = await SendAsync(HttpMethod.Put, KeycloakEndpoints.UserResetPassword(ResolveRealm(realm), userId), body, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode ? KeycloakResult.Ok(response.StatusCode) : await FailAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public Task<KeycloakResult> AddRequiredActionAsync(string userId, string requiredAction, string? realm = null, CancellationToken cancellationToken = default)
        => ChangeRequiredActionsAsync(userId, requiredAction, add: true, realm, cancellationToken);

    public Task<KeycloakResult> RemoveRequiredActionAsync(string userId, string requiredAction, string? realm = null, CancellationToken cancellationToken = default)
        => ChangeRequiredActionsAsync(userId, requiredAction, add: false, realm, cancellationToken);

    public async Task<KeycloakResult> AssignRealmRoleAsync(string userId, string roleName, string? realm = null, CancellationToken cancellationToken = default)
    {
        var resolvedRealm = ResolveRealm(realm);

        using var roleResponse = await SendAsync(HttpMethod.Get, KeycloakEndpoints.Role(resolvedRealm, roleName), null, cancellationToken).ConfigureAwait(false);
        if (!roleResponse.IsSuccessStatusCode)
            return await FailAsync(roleResponse, cancellationToken).ConfigureAwait(false);

        using var roleDoc = await ReadJsonAsync(roleResponse, cancellationToken).ConfigureAwait(false);
        var roleRepresentation = new[]
        {
            new { id = GetString(roleDoc.RootElement, "id"), name = GetString(roleDoc.RootElement, "name") }
        };

        using var assignResponse = await SendAsync(HttpMethod.Post, KeycloakEndpoints.UserRealmRoleMappings(resolvedRealm, userId), roleRepresentation, cancellationToken).ConfigureAwait(false);
        return assignResponse.IsSuccessStatusCode ? KeycloakResult.Ok(assignResponse.StatusCode) : await FailAsync(assignResponse, cancellationToken).ConfigureAwait(false);
    }

    public async Task<KeycloakResult<int>> GetUserCountAsync(string? realm = null, string? search = null, CancellationToken cancellationToken = default)
    {
        var path = KeycloakEndpoints.UsersCount(ResolveRealm(realm));
        if (!string.IsNullOrEmpty(search))
            path += $"?search={Uri.EscapeDataString(search)}";

        using var response = await SendAsync(HttpMethod.Get, path, null, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return await FailAsync<int>(response, cancellationToken).ConfigureAwait(false);

        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return int.TryParse(text.Trim(), out var count)
            ? KeycloakResult<int>.Ok(count)
            : KeycloakResult<int>.Fail("Unexpected user-count response from Keycloak.");
    }

    private async Task<KeycloakResult> ChangeRequiredActionsAsync(string userId, string requiredAction, bool add, string? realm, CancellationToken cancellationToken)
    {
        var resolvedRealm = ResolveRealm(realm);

        var current = await GetUserByIdAsync(userId, resolvedRealm, cancellationToken).ConfigureAwait(false);
        if (!current.Success || current.Data is null)
            return KeycloakResult.Fail(current.ErrorMessage ?? "User not found.", current.StatusCode);

        var actions = new HashSet<string>(current.Data.RequiredActions, StringComparer.OrdinalIgnoreCase);
        if (add)
            actions.Add(requiredAction);
        else
            actions.Remove(requiredAction);

        var body = new { requiredActions = actions.ToArray() };
        using var response = await SendAsync(HttpMethod.Put, KeycloakEndpoints.User(resolvedRealm, userId), body, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode ? KeycloakResult.Ok(response.StatusCode) : await FailAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static object BuildCreateBody(CreateKeycloakUserRequest request)
    {
        var body = new Dictionary<string, object?>
        {
            ["username"] = request.Username,
            ["email"] = request.Email,
            ["firstName"] = request.FirstName,
            ["lastName"] = request.LastName,
            ["enabled"] = request.Enabled
        };

        if (!string.IsNullOrEmpty(request.Password))
        {
            body["credentials"] = new[]
            {
                new { type = "password", value = request.Password, temporary = request.RequirePasswordChange }
            };
        }

        if (request.RequirePasswordChange)
            body["requiredActions"] = new[] { "UPDATE_PASSWORD" };

        if (request.Attributes.Count > 0)
            body["attributes"] = request.Attributes.ToDictionary(kv => kv.Key, kv => new[] { kv.Value });

        return body;
    }

    private static KeycloakUser MapUser(JsonElement element)
    {
        var user = new KeycloakUser
        {
            Id = GetString(element, "id"),
            Username = GetString(element, "username"),
            Email = GetStringOrNull(element, "email"),
            FirstName = GetStringOrNull(element, "firstName"),
            LastName = GetStringOrNull(element, "lastName"),
            Enabled = GetBool(element, "enabled")
        };

        if (element.TryGetProperty("requiredActions", out var actions) && actions.ValueKind == JsonValueKind.Array)
        {
            user.RequiredActions = actions.EnumerateArray()
                .Select(a => a.GetString())
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s!)
                .ToList();
        }

        return user;
    }
}
