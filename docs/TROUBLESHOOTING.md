# Troubleshooting

Every entry here is a real error hit while onboarding Super-POS, with the actual root cause and fix — not a guess. If you hit something not listed, it's genuinely new; open an issue with the exact error text.

---

### `Unable to find project information for '...csproj'`

**Cause:** you added a `ProjectReference` (or the packages via a source other than NuGet) to a project that isn't part of your solution's `.sln`/`.slnx` file. Restore can't resolve a reference to a project it doesn't know about, even though the file path is correct.

**Fix:**
```powershell
dotnet sln YourSolution.sln add path\to\the\missing\Project.csproj
```
If you're consuming this library via `PackageReference` from NuGet (the normal case), this doesn't apply — it's specific to local `ProjectReference` development/testing.

---

### `'Keycloak:Admin:ClientSecret' is required for the client_credentials grant.`

**Cause:** exactly what it says — the secret isn't set anywhere `IConfiguration` can see it. Every `Add…` method fails fast on missing required config by design, rather than silently starting up broken.

**Fix:**
```powershell
dotnet user-secrets set "Keycloak:Admin:ClientSecret" "<value>"
```
(Run from the project directory with the matching `UserSecretsId` in its `.csproj`.) Confirm it's actually set with `dotnet user-secrets list` before assuming the app will pick it up.

---

### `invalid_grant: Invalid user credentials`

**Cause:** almost always a literal copy-paste mistake — the password value still contains placeholder characters like `<...>` that were meant to be replaced, not typed literally. Also check for stray whitespace from a paste.

**Fix:** double-check the exact string being sent, character for character. This is not a Keycloak-side problem in the vast majority of cases.

---

### `invalid_grant: Account is not fully set up`

**Cause:** the user has a pending **required action** (most commonly `UPDATE_PASSWORD`, sometimes `VERIFY_EMAIL`) still attached to their account. This blocks the direct-grant (ROPC) flow specifically — resetting the password via the Credentials tab does **not** clear this; it's a separate field on the user record.

**Fix:** Keycloak console → Users → the user → **Details** tab → **Required user actions** → remove the pending entry → Save. (Or have the user complete it via the realm's Account Console, which clears it automatically on success.)

---

### 403 Forbidden from the `Admin` package, despite a successful token

**Cause:** creating a client with `serviceAccountsEnabled: true` does **not** automatically grant that service account any permissions. It can authenticate (get a token) but has zero rights to call the Admin REST API until explicitly granted.

**Fix:** grant the service account `manage-users` and `view-users` (at minimum) from the realm's built-in `realm-management` client:
1. Keycloak console → Clients → your service client → **Service accounts roles** tab (only visible when "Service accounts roles" is enabled in Settings).
2. Assign role → filter by client `realm-management` → select `manage-users`, `view-users`.

---

### PowerShell: Keycloak rejects a role-assignment call with a JSON parse error

**Cause:** a classic PowerShell gotcha — `@(@{...}) | ConvertTo-Json` silently unwraps a single-element array before serializing, sending Keycloak a bare object `{...}` where it requires an array `[{...}]` (e.g. the realm role-mappings endpoint).

**Fix:** call `ConvertTo-Json` with `-InputObject`, not through the pipeline:
```powershell
$body = ConvertTo-Json -InputObject @(@{ id = $roleId; name = $roleName })
```

---

### PowerShell script throws "missing string terminator" / "missing closing `}`" at the end of the file, but the code looks fine

**Cause:** Windows PowerShell 5.1 has known encoding quirks with non-ASCII characters (em-dashes `—`, smart quotes, etc.) in script files — they can be mis-decoded, corrupting string parsing far from where the actual character sits.

**Fix:** stick to plain ASCII in `.ps1` files — a `-` instead of `—`, straight quotes instead of curly ones. Validate any script before handing it over: `[scriptblock]::Create((Get-Content -Raw $path))` throws with a clear parse error if something's wrong, without executing anything.

---

### `Invoke-WebRequest` prompts "Script Execution Risk" and requires `[Y]/[A]/[N]` every run

**Cause:** Windows PowerShell 5.1's `Invoke-WebRequest` parses responses using the IE engine by default, which triggers this warning for HTML-ish content.

**Fix:** add `-UseBasicParsing` — this is literally what the warning message itself recommends.

---

### A resource server rejects a token that was just issued by Login/ROPC, even though the login itself succeeded

**Cause:** tokens issued via the `password` grant carry `aud: account` by default in Keycloak, **not** the app's own client id — unless the issuing client has an explicit audience mapper. Our `Authentication` package validates against a single expected audience *by design* (this is the fix for a real weakness found in an earlier hand-rolled integration that accepted the generic `account` audience) — so a token without the right `aud` claim is correctly rejected, not a bug.

**Fix:** add an `oidc-audience-mapper` protocol mapper to the **login client** (not the admin/service client), configured with `included.client.audience` set to that same client's id. See [`templates/keycloak-service-client.template.json`](../templates/keycloak-service-client.template.json) for the shape, and don't "fix" this by adding `account` to `AdditionalAudiences` instead — that just reintroduces the original weakness.

---

### Local build fails to restore packages / can't find `Microsoft.AspNetCore.Authentication.JwtBearer` for `net10.0`

**Cause:** the package version pinned for `net10.0` isn't in your local NuGet cache and there's no network access, or the SDK for `net10.0` isn't installed.

**Fix:** `dotnet --list-sdks` to confirm both `net8.0`- and `net10.0`-capable SDKs are present; `dotnet restore` with network access at least once to populate the cache.
