# Paysky.Identity.Keycloak

One internal library for Keycloak identity across the PaySky .NET estate. It replaces the two hand-rolled integrations (QRSwitch, APM) and is the standard onboarding path for new services (Super-POS first). See [docs/DESIGN.md](docs/DESIGN.md) for why it exists and why it's built this way.

**Build status (local, verified):** 6 packages × `net8.0` + `net10.0` = 12 assemblies, **0 warnings, 0 errors**. Unit tests: **27 passing**.

**Install:**
```powershell
dotnet add package Paysky.Identity.Keycloak
```

## Documentation

| Doc | Read it for |
|---|---|
| **This README** | Package overview, quick-start code samples |
| [docs/API-REFERENCE.md](docs/API-REFERENCE.md) | Every public method across every package — full signatures |
| [docs/GETTING-STARTED.md](docs/GETTING-STARTED.md) | Full step-by-step: onboarding a brand-new project |
| [docs/CONFIGURATION.md](docs/CONFIGURATION.md) | Every config field, what it does, what's required |
| [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) | Real errors hit onboarding Super-POS, with exact fixes |
| [docs/DESIGN.md](docs/DESIGN.md) | Why it's built this way — package split, security defaults, naming convention |

---

## Packages

Reference the meta-package for everything, or a single package for a narrower surface:

| Package | Use it for | ASP.NET dependency |
|---|---|---|
| **Paysky.Identity.Keycloak** | Meta-package — pulls the four below | (transitive) |
| **Paysky.Identity.Keycloak.Authentication** | Validate Keycloak JWTs (resource server), single- or multi-realm | Yes |
| **Paysky.Identity.Keycloak.Authorization** | `[Permission("…")]` policies mapped to Keycloak roles | Yes |
| **Paysky.Identity.Keycloak.Admin** | Manage users/roles/realms via the Admin REST API | **No** (usable from workers) |
| **Paysky.Identity.Keycloak.Login** | Broker end-user login/refresh/logout/userinfo | **No** |
| **Paysky.Identity.Keycloak.Abstractions** | Contracts, options, results (referenced by all) | **No** |

All packages multi-target `net8.0;net10.0`.

---

## Quick start

### 1. Resource server (validate tokens)

```csharp
builder.Services.AddPayskyKeycloakAuthentication(builder.Configuration);
// ...
app.UseAuthentication();
app.UseAuthorization();
```

- **One** realm in config → single JwtBearer scheme.
- **Two or more** realms → an issuer-based policy scheme routes each token to the right realm's validator automatically (replaces APM's hand-copied `AuthExtensions`).
- Realm roles (`realm_access.roles`) are mapped to `ClaimTypes.Role` by default; client roles (`resource_access.<clientId>.roles`) are opt-in per realm.
- The tenant id is normalized from any of `tenantId` / `tenant_id` / `tid` / `TenantId` into one `X-Tenant` claim.

### 2. Permission authorization

```csharp
builder.Services.AddPayskyPermissionAuthorization(o =>
    o.ForbiddenMessageResolver = ctx => Localize(ctx, "Forbidden")); // optional
```

```csharp
[Permission("Users.Create")]
public IActionResult CreateUser(...) { ... }
```

### 3. Admin client (users / roles / provisioning)

```csharp
builder.Services.AddPayskyKeycloakAdmin(builder.Configuration);
```

```csharp
public sealed class UserService(IKeycloakUserAdmin users)
{
    public Task<KeycloakResult<string>> Create(CreateKeycloakUserRequest req) => users.CreateUserAsync(req);
}
```

### 4. Login broker (direct user login)

```csharp
builder.Services.AddPayskyKeycloakLogin(builder.Configuration);
```

```csharp
public sealed class AuthController(IKeycloakLoginBroker broker)
{
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await broker.LoginAsync(dto.Username, dto.Password);
        return result.Success ? Ok(result.Data) : Unauthorized(result.ErrorMessage);
    }
}
```

App-specific rules (e.g. QRSwitch's tenant-active check, `UPDATE_PASSWORD` handling) stay in the app: call the broker, then apply your policy on the returned token. They are deliberately **not** baked into the shared library.

---

## Configuration

Full canonical schema: [`templates/appsettings.Keycloak.sample.json`](templates/appsettings.Keycloak.sample.json). Every field explained: [docs/CONFIGURATION.md](docs/CONFIGURATION.md). Minimal resource-server example:

```jsonc
"Keycloak": {
  "BaseUrl": "https://idp.paysky.internal",
  "RequireHttpsMetadata": true,
  "Realms": [
    { "Name": "qrswitch", "ClientId": "qrswitch-api", "MapRealmRoles": true }
  ]
}
```

### Security defaults (all overridable, but safe by default)
- `RequireHttpsMetadata` = **true**.
- Audience validation = **on**, expecting a **single** audience (`ClientId`). The generic Keycloak `account` audience is **not** accepted unless you list it in `AdditionalAudiences` — this closes the weakness in the legacy QRSwitch setup.
- Issuer + lifetime validation = **on**, explicit.
- Admin token grant = **`client_credentials`** (service account), not ROPC.
- Every `Add…` method **fails fast** with a clear exception when required config or secrets are missing.

---

## Realm-naming convention

A realm name always begins with the lowercase project slug; append an audience suffix only when a project needs more than one realm.

- Single-realm: `qrswitch`, `super-pos`
- Multi-realm: `apm-admin`, `apm-merchant`, `apm-tenant`

Lowercase kebab-case only — realm names live in issuer URLs and the multi-realm selector matches on them.

**Onboarding a brand-new project?** Full walkthrough: [docs/GETTING-STARTED.md](docs/GETTING-STARTED.md).

## Standard client template

[`templates/keycloak-service-client.template.json`](templates/keycloak-service-client.template.json) — a confidential, service-account-enabled client with direct-access-grants off and a tenant-id protocol mapper. Replace the `REPLACE_*` placeholders and import via the Keycloak Admin console or REST API.

---

## Build & test

```powershell
dotnet build Paysky.Identity.Keycloak.slnx
dotnet test  tests/Paysky.Identity.Keycloak.UnitTests
```

Unit tests cover the behavioural core: role mapping (realm + client + malformed + de-dup), tenant normalization, audience/authority resolution, result factories, Keycloak error parsing, and admin-token caching + grant selection.

Something not working? [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) covers real errors hit onboarding Super-POS, with the actual cause and fix for each.

## Remaining phase (not yet done)

**Integration tests against a live Keycloak (Testcontainers)** are the one planned item still outstanding — they need Docker and a running Keycloak, so they are a separate, environment-dependent phase from this compile-and-unit-verified core. Scope: provision a realm/client from the template, then exercise login → validate → admin CRUD → refresh → logout end-to-end.

---

## Adoption order

1. **Super-POS** — greenfield (no Keycloak today); proves the library on a clean target.
2. **APM** — replace per-service validation, collapse the 3 duplicated `AuthExtensions`, swap the hand-built admin client. Renames realms to `apm-admin` / `apm-merchant` / `apm-tenant`.
3. **QRSwitch** — collapse the two internal `KeycloakBaseService` implementations onto `IKeycloakTokenProvider`; tighten the audience check with a staged token re-issue so in-flight `aud=account` tokens are not locked out mid-deploy.

See [docs/DESIGN.md](docs/DESIGN.md) for the full rationale behind this order, and why QRSwitch's step specifically can't be a simple config swap.
