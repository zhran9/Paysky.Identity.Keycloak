using Microsoft.AspNetCore.Authorization;
using Paysky.Identity.Keycloak;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the permission-based authorization pipeline for the <see cref="PermissionAttribute"/>.</summary>
public static class KeycloakAuthorizationExtensions
{
    /// <summary>
    /// Adds the permission policy, its handler, and the JSON 403 result handler. Safe to call alongside
    /// <c>AddPayskyKeycloakAuthentication</c>, which also registers authorization services.
    /// </summary>
    public static IServiceCollection AddPayskyPermissionAuthorization(
        this IServiceCollection services,
        Action<PermissionAuthorizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();

        if (configure is not null)
            services.Configure(configure);
        else
            services.AddOptions<PermissionAuthorizationOptions>();

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ForbiddenResultHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PermissionAttribute.PolicyName, policy =>
                policy.Requirements.Add(new PermissionRequirement()));
        });

        return services;
    }
}
