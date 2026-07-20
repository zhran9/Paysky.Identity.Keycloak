# Configuration reference

Full field-by-field reference for the `Keycloak` configuration section. See [`templates/appsettings.Keycloak.sample.json`](../templates/appsettings.Keycloak.sample.json) for a complete, copy-pasteable example (shown there with a multi-realm setup).

Every field below binds from the `Keycloak` section by default. Pass a different section name to any `Add…` method if you need to (e.g. `AddPayskyKeycloakAuthentication(configuration, "MyCustomSection")`).

## Root (`Keycloak`)

| Field | Type | Required | Default | Notes |
|---|---|---|---|---|
| `BaseUrl` | string | **Yes** | — | e.g. `https://idp.paysky.internal`. No trailing slash needed. |
| `RequireHttpsMetadata` | bool | No | `true` | Set `false` only for local Docker dev against plain HTTP. Never in a real environment. |
| `Realms` | array | **Yes**, at least one | — | See below. |
| `TenantClaim` | object | No | see below | Tenant-claim normalization. |
| `Admin` | object | Only if using the Admin package | — | See below. |
| `Login` | object | Only if using the Login package | — | See below. |

## `Realms[]` (used by `Authentication`)

One entry = single-realm mode (default JwtBearer scheme). Two or more = multi-realm mode — an issuer-based policy scheme picks the right validator per token automatically.

| Field | Type | Required | Default | Notes |
|---|---|---|---|---|
| `Name` | string | **Yes** | — | The realm name. See the [realm-naming convention](../README.md#realm-naming-convention) — lowercase, project-slug-prefixed. |
| `SchemeName` | string | No | `Name` | Authentication scheme name in multi-realm mode. Ignored in single-realm mode. |
| `ClientId` | string | **Yes** | — | The client tokens for this realm are issued to. Also the default expected audience. |
| `Audience` | string | No | `ClientId` | Override if the expected audience differs from the client id. |
| `AdditionalAudiences` | array | No | `[]` | Extra audiences to accept — **opt-in only**. Do not add the generic Keycloak `account` audience here unless you specifically intend to accept it (see [Security defaults](../README.md#security-defaults)). |
| `MapRealmRoles` | bool | No | `true` | Copies `realm_access.roles` into `ClaimTypes.Role` claims. |
| `MapClientRoles` | bool | No | `false` | Copies `resource_access.<ClientId>.roles` into `ClaimTypes.Role` claims. |

## `TenantClaim`

| Field | Type | Default | Notes |
|---|---|---|---|
| `SourceNames` | array | `["tenantId","tenant_id","tid","TenantId"]` | Candidate claim names, tried in order. First non-empty match wins. |
| `CanonicalName` | string | `X-Tenant` | The single claim name every consumer should read. |
| `Enabled` | bool | `true` | Set `false` to skip tenant-claim normalization entirely. |

## `Admin` (used by the `Admin` package)

| Field | Type | Required | Default | Notes |
|---|---|---|---|---|
| `GrantType` | string | No | `client_credentials` | `client_credentials` (service account, recommended) or `password` (ROPC — avoid for new services). |
| `ClientId` | string | **Yes** | — | A confidential, service-account-enabled client. |
| `ClientSecret` | string | Required for `client_credentials` | — | Bind from a secret store / env var, never a literal. |
| `Username` / `Password` | string | Required for `password` grant only | — | Bind from a secret store, never literal. |
| `TokenRealm` | string | No | `master` | The realm the admin token itself is obtained from. |
| `ManagedRealm` | string | No | — | Default realm admin operations target when a call doesn't specify one. |
| `TokenExpiryBufferSeconds` | int | No | `30` | Seconds before actual expiry the cached token is treated as stale. |

**Before this works, the client's service account needs Keycloak permissions** — creating a `serviceAccountsEnabled: true` client does **not** automatically grant it any rights. You must assign it `manage-users` and `view-users` (at minimum) from the built-in `realm-management` client. See [TROUBLESHOOTING.md](TROUBLESHOOTING.md#403-forbidden-from-the-admin-package-despite-a-successful-token) for exactly how.

## `Login` (used by the `Login` package)

| Field | Type | Required | Default | Notes |
|---|---|---|---|---|
| `Realm` | string | **Yes** | — | Realm end users authenticate against. |
| `ClientId` | string | **Yes** | — | The public-facing app client used for the login exchange. |
| `ClientSecret` | string | Required if the client is confidential | — | Bind from a secret store, never literal. |
| `DefaultScopes` | string | No | `openid profile email` | Scopes requested at login. |

**This client needs a different Keycloak setting than the Admin client above** — ROPC (the `password` grant) requires `directAccessGrantsEnabled: true` on the client. The standard `Admin`-package client template deliberately sets this to `false` (it's for `client_credentials`, not user login) — don't reuse that same client for both purposes; provision two separate clients (see the standard client template).
