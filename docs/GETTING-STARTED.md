# Onboarding a new project — step by step

This is the exact sequence used to onboard Super-POS, generalized. Follow it in order.

## 1. Decide your realm shape

One realm, or several? Almost every project needs exactly one realm. Only split into multiple realms if you have genuinely separate audiences that must never see each other's users (APM's `admin` vs `merchant` is the one real example in the estate today).

Pick a realm name now, following the [naming convention](../README.md#realm-naming-convention): lowercase, project-slug-prefixed (`your-project`, or `your-project-admin` / `your-project-merchant` if multi-realm).

## 2. Provision Keycloak

You need, per realm:
- The realm itself.
- A **service client** for the `Admin` package (`client_credentials`, `serviceAccountsEnabled: true`, `directAccessGrantsEnabled: false`) — use the [standard client template](../templates/keycloak-service-client.template.json).
- If you use the `Login` package: a **separate login client** (`directAccessGrantsEnabled: true`) — ROPC needs this flag; the service client above deliberately doesn't have it.
- Any realm roles your app's authorization policies need (e.g. `SystemAdmin`, `MerchantAdmin`).
- The service client's service account granted `manage-users` + `view-users` on `realm-management` — **this is not automatic**, see [CONFIGURATION.md](CONFIGURATION.md#admin-used-by-the-admin-package).

Copy `Super-POS/Keycloak-setup/` as a starting point for the actual provisioning tooling (docker-compose + a generic init script that reads config from JSON files — no code changes needed to onboard, just add your project's config folder).

## 3. Reference the packages

```powershell
dotnet add package Paysky.Identity.Keycloak
```

That's the meta-package — pulls in `Authentication`, `Authorization`, `Admin`, and `Login`. If you're a background worker with no web surface, reference `Paysky.Identity.Keycloak.Admin` and/or `.Login` directly instead — they carry no ASP.NET Core dependency.

## 4. Add configuration

Copy [`templates/appsettings.Keycloak.sample.json`](../templates/appsettings.Keycloak.sample.json)'s `Keycloak` section into your `appsettings.json`, filling in your realm/client names. **Secrets never go in the committed file** — use `dotnet user-secrets` for local dev:

```powershell
dotnet user-secrets set "Keycloak:Admin:ClientSecret" "<value>"
dotnet user-secrets set "Keycloak:Login:ClientSecret" "<value>"
```

## 5. Wire it up

```csharp
builder.Services.AddPayskyKeycloakAuthentication(builder.Configuration);
builder.Services.AddPayskyKeycloakAdmin(builder.Configuration);      // if you use the Admin package
builder.Services.AddPayskyKeycloakLogin(builder.Configuration);      // if you use the Login package
builder.Services.AddPayskyPermissionAuthorization();                 // if you use [Permission("...")]
// ...
app.UseAuthentication();
app.UseAuthorization();
```

**If your project already has its own custom authentication schemes** (device auth, a legacy JWT scheme, etc.) — register `AddPayskyKeycloakAuthentication` **before** your existing `AddAuthentication(...)` chain that sets your app's actual default scheme. Multiple `AddAuthentication()` calls compose correctly (schemes accumulate, and the *last* registered `DefaultScheme` setting wins) as long as no scheme names collide. See Super-POS's `AddJwtAuthentication` for a real example of this composition working alongside a pre-existing `DynamicScheme`/`NoAuth`/`Reject` setup.

## 6. Prove it works before trusting it

Don't assume — get a real token and call a real protected endpoint:

```powershell
$body = @{
    grant_type    = "password"
    client_id     = "<your-login-client-id>"
    client_secret = "<your-login-client-secret>"
    username      = "<a real test user>"
    password      = "<their password>"
}
$token = (Invoke-RestMethod -Method Post -Uri "https://idp.paysky.internal/realms/<your-realm>/protocol/openid-connect/token" -Body $body).access_token
Invoke-RestMethod -Uri "https://your-api/some-protected-endpoint" -Headers @{ Authorization = "Bearer $token" }
```

If this is your first integration and you want zero risk to existing live traffic while proving it out, add a **second, separate** authentication scheme name (e.g. `"KeycloakBearer"`) that nothing routes to by default, and test against a throwaway endpoint using `[Authorize(AuthenticationSchemes = "KeycloakBearer")]` — that's exactly how Super-POS validated the whole chain before touching its real login flow. See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) if anything fails along the way — it's built from the real errors hit doing exactly this.
