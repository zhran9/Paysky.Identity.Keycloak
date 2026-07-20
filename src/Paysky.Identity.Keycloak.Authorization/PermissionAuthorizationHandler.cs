using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Paysky.Identity.Keycloak;

/// <summary>
/// Grants access when the authenticated caller holds a role matching the permission named on the
/// endpoint's <see cref="PermissionAttribute"/>. Roles come from the token via
/// <c>KeycloakClaimsTransformer</c> in the Authentication package.
/// </summary>
internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var requiredPermission = ResolveRequiredPermission();
        if (string.IsNullOrEmpty(requiredPermission))
        {
            // Policy applied without a [Permission] attribute — deny rather than silently allow.
            context.Fail();
            return Task.CompletedTask;
        }

        var hasPermission = context.User.Claims.Any(claim =>
            claim.Type == ClaimTypes.Role &&
            string.Equals(claim.Value, requiredPermission, StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
            context.Succeed(requirement);
        else
            context.Fail(new AuthorizationFailureReason(this, $"Missing required permission: {requiredPermission}"));

        return Task.CompletedTask;
    }

    private string? ResolveRequiredPermission()
    {
        var endpoint = _httpContextAccessor.HttpContext?.GetEndpoint();
        return endpoint?.Metadata.GetMetadata<PermissionAttribute>()?.PermissionName;
    }
}
