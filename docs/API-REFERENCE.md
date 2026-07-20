# API Reference

Every public method you can call, grouped by package. Signatures are copied directly from the interfaces — if this ever drifts from the actual code, the code wins; open an issue.

Every async method takes an optional trailing `CancellationToken cancellationToken = default` — omitted below for brevity.

---

## Paysky.Identity.Keycloak.Authentication

### Registration

```csharp
IServiceCollection AddPayskyKeycloakAuthentication(
    this IServiceCollection services,
    IConfiguration configuration,
    string sectionName = KeycloakOptions.SectionName)
```
Registers JWT bearer validation. Reads `Realms[]` from config: one entry → single scheme (`JwtBearerDefaults.AuthenticationScheme`, i.e. `"Bearer"`); two or more → an issuer-based policy scheme (`KeycloakAuthenticationExtensions.MultiRealmScheme`) that routes each token to the right realm's validator automatically. Throws `InvalidOperationException` immediately if `BaseUrl` or `Realms` is missing (fail-fast, not a silent broken startup).

There's nothing else to call here — this one method does the whole job. Role/claim mapping happens automatically via `KeycloakClaimsTransformer` (see below) as part of token validation; you don't call it directly in the normal case.

### `KeycloakClaimsTransformer` (static — call directly only if composing auth manually)

```csharp
static void MapRoles(ClaimsIdentity identity, KeycloakRealmOptions realm)
static void NormalizeTenantClaim(ClaimsIdentity identity, TenantClaimOptions tenant)
```
Pure, side-effect-on-`identity`-only, no DI dependency. `AddPayskyKeycloakAuthentication` calls both internally on every token. Call these yourself only if your project has pre-existing custom authentication schemes you're composing around (see [GETTING-STARTED.md](GETTING-STARTED.md#5-wire-it-up)) rather than using the one-line registration.

---

## Paysky.Identity.Keycloak.Authorization

### Registration

```csharp
IServiceCollection AddPayskyPermissionAuthorization(
    this IServiceCollection services,
    Action<PermissionAuthorizationOptions>? configure = null)
```
Registers the `PermissionAttribute.PolicyName` policy, its handler, and a JSON 403 result handler.

```csharp
public sealed class PermissionAuthorizationOptions
{
    Func<HttpContext, string>? ForbiddenMessageResolver { get; set; }
}
```
Optional — customize the 403 body's message (e.g. localize it). Defaults to a fixed English message.

### `[Permission("...")]` attribute

```csharp
[Permission("Users.Create")]
public IActionResult CreateUser(...) { ... }
```
Apply to a controller or action. The string is a Keycloak realm role name — the caller must have that exact role (case-insensitive) in `ClaimTypes.Role`.

---

## Paysky.Identity.Keycloak.Admin

### Registration

```csharp
IServiceCollection AddPayskyKeycloakAdmin(
    this IServiceCollection services,
    IConfiguration configuration,
    string sectionName = KeycloakOptions.SectionName)
```
Requires `Keycloak:Admin` in config. Registers `IKeycloakTokenProvider` (singleton), `IKeycloakUserAdmin`, `IKeycloakRoleAdmin`, `IKeycloakProvisioning` (all scoped). Throws immediately if required admin config/secrets are missing.

### `IKeycloakTokenProvider`

| Method | Returns | Does |
|---|---|---|
| `GetAccessTokenAsync()` | `Task<string>` | A currently-valid admin access token, caching/refreshing transparently. You won't normally call this directly — the other Admin interfaces use it internally. |

### `IKeycloakUserAdmin`

Every method takes an optional trailing `string? realm = null` — defaults to `Keycloak:Admin:ManagedRealm` when omitted.

| Method | Returns | Does |
|---|---|---|
| `CreateUserAsync(CreateKeycloakUserRequest request)` | `Task<KeycloakResult<string>>` | Creates a user. Result data is the new user's id. |
| `GetUserByIdAsync(string userId)` | `Task<KeycloakResult<KeycloakUser>>` | Fetch a user by id. |
| `GetUserByUsernameAsync(string username)` | `Task<KeycloakResult<KeycloakUser>>` | Fetch a user by exact username match. |
| `UpdateUserAsync(string userId, UpdateKeycloakUserRequest request)` | `Task<KeycloakResult>` | Updates only the non-null fields on `request`. |
| `DeleteUserAsync(string userId)` | `Task<KeycloakResult>` | Deletes a user. |
| `ResetPasswordAsync(string userId, string newPassword, bool temporary = false)` | `Task<KeycloakResult>` | Sets a password credential. `temporary: true` forces a change on next login. |
| `AddRequiredActionAsync(string userId, string requiredAction)` | `Task<KeycloakResult>` | Adds a required action (e.g. `"UPDATE_PASSWORD"`, `"VERIFY_EMAIL"`) without disturbing existing ones. |
| `RemoveRequiredActionAsync(string userId, string requiredAction)` | `Task<KeycloakResult>` | Removes one required action. |
| `AssignRealmRoleAsync(string userId, string roleName)` | `Task<KeycloakResult>` | Assigns an existing realm role to the user. |
| `GetUserCountAsync(string? search = null)` | `Task<KeycloakResult<int>>` | Total user count, optionally filtered by search term. |

### `IKeycloakRoleAdmin`

| Method | Returns | Does |
|---|---|---|
| `CreateRoleAsync(string roleName, string? description = null)` | `Task<KeycloakResult>` | Creates a realm role. |
| `GetRoleByNameAsync(string roleName)` | `Task<KeycloakResult<KeycloakRole>>` | Fetch a role by name. |
| `GetAllRolesAsync()` | `Task<KeycloakResult<IReadOnlyList<KeycloakRole>>>` | All realm roles. |
| `DeleteRoleAsync(string roleName)` | `Task<KeycloakResult>` | Deletes a realm role. |
| `GetRolesForUserAsync(string userId)` | `Task<KeycloakResult<IReadOnlyList<KeycloakRole>>>` | Realm roles currently assigned to a user. |

### `IKeycloakProvisioning`

Bootstrap-time operations — realm/client setup, not day-to-day app code.

| Method | Returns | Does |
|---|---|---|
| `EnsureRealmAsync(string realm)` | `Task<KeycloakResult>` | Creates the realm if it doesn't already exist. Idempotent. |
| `CreateConfidentialClientAsync(string clientId, string realm)` | `Task<KeycloakResult<string>>` | Creates a confidential, service-account-enabled client (matches the [standard template](../templates/keycloak-service-client.template.json)'s shape). Result data is the client's internal uuid. |

---

## Paysky.Identity.Keycloak.Login

### Registration

```csharp
IServiceCollection AddPayskyKeycloakLogin(
    this IServiceCollection services,
    IConfiguration configuration,
    string sectionName = KeycloakOptions.SectionName)
```
Requires `Keycloak:Login` in config. Registers `IKeycloakLoginBroker` (scoped).

### `IKeycloakLoginBroker`

| Method | Returns | Does |
|---|---|---|
| `LoginAsync(string username, string password)` | `Task<KeycloakResult<TokenPair>>` | Resource Owner Password exchange. Fails with `KeycloakResult.ErrorMessage` set (not an exception) on bad credentials or a pending required action. |
| `RefreshAsync(string refreshToken)` | `Task<KeycloakResult<TokenPair>>` | Exchanges a refresh token for a new token pair. |
| `LogoutAsync(string accessToken)` | `Task<KeycloakResult>` | Revokes the session tied to the token. |
| `GetUserFromTokenAsync(string accessToken)` | `Task<KeycloakResult<KeycloakUserInfo>>` | Resolves profile claims via Keycloak's `userinfo` endpoint. |

App-specific login rules (tenant-active checks, custom required-action handling) are **not** part of this interface by design — call `LoginAsync`, then apply your own policy to the result. See [DESIGN.md](DESIGN.md) for why.

---

## Paysky.Identity.Keycloak.Abstractions

Referenced transitively by everything above — you rarely add this package directly, but these are the types you'll actually see in your own code.

### Result types

```csharp
record KeycloakResult(bool Success, string? ErrorMessage = null, HttpStatusCode StatusCode = HttpStatusCode.OK)
record KeycloakResult<T>(bool Success, T? Data = default, string? ErrorMessage = null, HttpStatusCode StatusCode = HttpStatusCode.OK)
```
Every Admin/Login call returns one of these instead of throwing on an expected failure (bad credentials, user not found, etc.) — check `.Success` before using `.Data`. Static factories: `KeycloakResult.Ok()` / `.Fail(message)`, `KeycloakResult<T>.Ok(data)` / `.Fail(message)`.

### Request/response models

| Type | Used by | Fields |
|---|---|---|
| `CreateKeycloakUserRequest` | `IKeycloakUserAdmin.CreateUserAsync` | `Username`, `Email`, `FirstName`, `LastName`, `Password`, `Enabled` (default `true`), `RequirePasswordChange`, `Attributes` |
| `UpdateKeycloakUserRequest` | `IKeycloakUserAdmin.UpdateUserAsync` | `Email`, `FirstName`, `LastName`, `Enabled` — all nullable, null = unchanged |
| `KeycloakUser` | Returned by user lookups | `Id`, `Username`, `Email`, `FirstName`, `LastName`, `Enabled`, `RequiredActions`, `Attributes` |
| `KeycloakRole` | Returned by role lookups | `Id`, `Name`, `Description`, `Composite` |
| `TokenPair` | Returned by `IKeycloakLoginBroker` | `AccessToken`, `RefreshToken`, `ExpiresInSeconds` |
| `KeycloakUserInfo` | Returned by `GetUserFromTokenAsync` | `Id`, `Username`, `Email`, `FirstName`, `LastName`, `Roles` |

### Options (bind from config — see [CONFIGURATION.md](CONFIGURATION.md) for every field)

`KeycloakOptions`, `KeycloakRealmOptions`, `TenantClaimOptions`, `KeycloakAdminOptions`, `KeycloakLoginOptions` — you set these via `appsettings.json`, not by constructing them in code.

### Constants

```csharp
static class KeycloakGrantTypes { const string Password, RefreshToken, ClientCredentials; }
static class KeycloakClaimNames  { const string RealmAccess, ResourceAccess, Roles, Issuer, PreferredUsername; }
```
