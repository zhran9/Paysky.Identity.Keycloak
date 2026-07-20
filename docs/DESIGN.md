# Design rationale

Why this library exists and why it's built the way it is — for anyone deciding whether/how to adopt it, not just how to call it.

## Why this exists

Before this library, three PaySky projects integrated with Keycloak independently: **QRSwitch**, **APM**, and (in spirit) the pattern any new project would have copy-pasted from whichever of those it found first. A direct comparison of the two real integrations found real, meaningful drift, not just cosmetic differences:

- **APM** validates issuer/audience/lifetime strictly, but never reads Keycloak roles into .NET claims at all — authorization is ad hoc claim reads, not a role system.
- **QRSwitch** maps Keycloak roles into a real permission system, but its audience validation accepts the generic Keycloak `account` audience alongside its own — a real weakness, not a stylistic choice.
- Both hardcode `RequireHttpsMetadata=false` with no environment override, and use the deprecated `password` (ROPC) grant for admin/service operations instead of `client_credentials`.
- QRSwitch has **two different, non-identical** admin-token-caching implementations within its own codebase.

None of this was a specific team's fault — it's what happens when the same integration gets built more than once with no shared reference. This library is that shared reference: it adopts the stronger half of each drifted decision (QRSwitch's role-mapping, APM's stricter validation style), fixes the known weaknesses, and gives every future project one thing to copy instead of one more independent implementation to drift from.

## Why five packages instead of one

The PaySky estate has two genuinely different kinds of Keycloak consumers:

1. **ASP.NET Core web APIs** that need to validate incoming tokens and authorize requests — needs `Microsoft.AspNetCore.Authentication.JwtBearer`.
2. **Background workers with no web surface at all** (Windows Services, WCF hosts) that only need to call Keycloak's Admin API or broker a login — has no business depending on the entire ASP.NET Core authentication stack just for that.

Real examples of case 2 already exist in the estate (Clearing_Service's Windows Service, NotificationService's WCF host). A single monolithic package would force every worker to carry web-framework weight it never uses. So the split tracks exactly one boundary — does this capability need ASP.NET Core, or not:

| Has ASP.NET Core dependency | No ASP.NET Core dependency |
|---|---|
| `Authentication` | `Admin` |
| `Authorization` | `Login` |
| | `Abstractions` (referenced by all) |

`Paysky.Identity.Keycloak` is a meta-package that pulls in all four — the right default for a normal web API that doesn't care about this distinction. Most consumers should just reference that one package; the split exists for the minority that need it, not as a tax on everyone.

## Security defaults — and why they're not the "convenient" defaults

Every default is the **safe** one, even where that's less convenient locally, because a library used across a payment platform shouldn't make insecure the path of least resistance:

- `RequireHttpsMetadata = true` by default — both prior integrations defaulted this to `false`. Override it explicitly for local dev; never let it be the silent default in real config.
- Audience validation accepts **exactly** the configured client id unless you explicitly opt other audiences in via `AdditionalAudiences` — closes the QRSwitch weakness of silently accepting the generic `account` audience.
- Admin authentication defaults to `client_credentials`, not the deprecated `password` grant both prior integrations used.
- Every `Add…` registration method **fails fast** with a specific exception on missing required config, rather than starting up in a broken, half-configured state.

## Realm-naming convention

A realm name always begins with the lowercase project slug; append an audience suffix only when a project genuinely needs more than one realm.

- Single-realm: `qrswitch`, `super-pos`
- Multi-realm: `apm-admin`, `apm-merchant`, `apm-tenant`

**Why this, specifically:** the alternative (APM's original pattern of bare `admin`/`merchant`/`tenant` realm names, shared across whichever services needed them) creates a real collision risk the moment a second project also wants an "admin" realm on the same Keycloak instance — and it already nearly happened once Super-POS was in the picture. Project-prefixing removes the collision class entirely, at the cost of renaming APM's existing realms during its own adoption (a one-time, planned cost, not an ongoing one). Lowercase-only avoids a separate, real bug class: realm names live in issuer URLs, and a case-sensitive string match in a multi-realm scheme selector will silently misroute a token if casing ever drifts.

## Adoption order, and why this order specifically

1. **Super-POS** first — it had zero existing Keycloak integration, so there was no legacy behavior to preserve and no risk of breaking live traffic while proving the library out. This is also why the first real integration deliberately used a **second, additive authentication scheme** to validate the whole chain against real running code before touching anything live — see [GETTING-STARTED.md](GETTING-STARTED.md#6-prove-it-works-before-trusting-it).
2. **APM** next — highest structural payoff (collapses three duplicated `AuthExtensions.cs` copies into one config-driven multi-realm setup), but requires the realm rename discussed above, so it needs its own planned rollout, not a quiet swap.
3. **QRSwitch** last — the most invasive change, because tightening the audience check is a **live-traffic-affecting** change: tokens already in circulation with `aud=account` would stop validating the moment the fix lands, unless the rollout is staged (accept both audiences → re-issue tokens with the correct one → tighten). This is the one adoption step that cannot be a simple config swap.

Full drift analysis and the original comparison this library was built from: see the project history — this document captures the durable "why," not the session-by-session process that produced it.
