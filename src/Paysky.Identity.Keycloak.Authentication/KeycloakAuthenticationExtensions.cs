using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Paysky.Identity.Keycloak;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers Keycloak JWT bearer authentication from the <c>Keycloak</c> configuration section.
/// One realm configured = single-scheme validation; two or more = issuer-based multi-realm selection.
/// </summary>
public static class KeycloakAuthenticationExtensions
{
    /// <summary>Policy scheme name used to route requests in multi-realm mode.</summary>
    public const string MultiRealmScheme = "PayskyKeycloak";

    /// <summary>
    /// Adds Keycloak authentication (and authorization services) using the given configuration section.
    /// Throws <see cref="InvalidOperationException"/> if required options are missing (fail fast).
    /// </summary>
    public static IServiceCollection AddPayskyKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = KeycloakOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionName);
        var options = section.Get<KeycloakOptions>()
            ?? throw new InvalidOperationException($"Keycloak configuration section '{sectionName}' is missing or empty.");

        Validate(options, sectionName);
        services.Configure<KeycloakOptions>(section);

        if (options.Realms.Count == 1)
            ConfigureSingleRealm(services, options);
        else
            ConfigureMultiRealm(services, options);

        services.AddAuthorization();
        return services;
    }

    private static void Validate(KeycloakOptions options, string sectionName)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            throw new InvalidOperationException($"'{sectionName}:BaseUrl' is required.");

        if (options.Realms.Count == 0)
            throw new InvalidOperationException($"'{sectionName}:Realms' must contain at least one realm.");

        foreach (var realm in options.Realms)
        {
            if (string.IsNullOrWhiteSpace(realm.Name))
                throw new InvalidOperationException($"'{sectionName}:Realms' contains a realm with no Name.");
            if (string.IsNullOrWhiteSpace(realm.ClientId))
                throw new InvalidOperationException($"Realm '{realm.Name}' is missing a ClientId.");
        }
    }

    private static void ConfigureSingleRealm(IServiceCollection services, KeycloakOptions options)
    {
        var realm = options.Realms[0];
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o => ConfigureJwtBearer(o, options, realm));
    }

    private static void ConfigureMultiRealm(IServiceCollection services, KeycloakOptions options)
    {
        var builder = services.AddAuthentication(MultiRealmScheme);

        builder.AddPolicyScheme(MultiRealmScheme, MultiRealmScheme, policy =>
        {
            policy.ForwardDefaultSelector = context => SelectScheme(context, options);
        });

        foreach (var realm in options.Realms)
            builder.AddJwtBearer(realm.ResolveSchemeName(), o => ConfigureJwtBearer(o, options, realm));
    }

    private static string SelectScheme(HttpContext context, KeycloakOptions options)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = header["Bearer ".Length..].Trim();
            var issuer = ReadIssuerUnvalidated(token);
            if (!string.IsNullOrEmpty(issuer))
            {
                foreach (var realm in options.Realms)
                {
                    if (issuer!.Contains($"/realms/{realm.Name}", StringComparison.OrdinalIgnoreCase))
                        return realm.ResolveSchemeName();
                }
            }
        }

        // No/unrecognized token: forward to the first realm's handler, which still rejects it properly.
        return options.Realms[0].ResolveSchemeName();
    }

    private static void ConfigureJwtBearer(JwtBearerOptions o, KeycloakOptions options, KeycloakRealmOptions realm)
    {
        var authority = options.BuildAuthority(realm.Name);

        o.Authority = authority;
        o.MetadataAddress = options.BuildMetadataAddress(realm.Name);
        o.RequireHttpsMetadata = options.RequireHttpsMetadata;

        var audiences = new List<string> { realm.ResolveAudience() };
        audiences.AddRange(realm.AdditionalAudiences);

        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudiences = audiences,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = KeycloakClaimNames.PreferredUsername,
            RoleClaimType = ClaimTypes.Role
        };

        o.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                if (ctx.Principal?.Identity is ClaimsIdentity identity)
                {
                    KeycloakClaimsTransformer.MapRoles(identity, realm);
                    KeycloakClaimsTransformer.NormalizeTenantClaim(identity, options.TenantClaim);
                }
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync("{\"status\":401,\"message\":\"Unauthorized: token is invalid or missing\"}");
            }
        };
    }

    /// <summary>Reads the <c>iss</c> claim from an unvalidated JWT payload (scheme selection only).</summary>
    private static string? ReadIssuerUnvalidated(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
            return null;

        try
        {
            var payload = Base64UrlDecode(parts[1]);
            using var doc = JsonDocument.Parse(payload);
            return doc.RootElement.TryGetProperty(KeycloakClaimNames.Issuer, out var iss)
                ? iss.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        s += (s.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty
        };
        return Convert.FromBase64String(s);
    }
}
