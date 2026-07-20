using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Paysky.Identity.Keycloak;

/// <summary>
/// Returns a consistent JSON 403 body when authorization forbids a request, using the configured
/// <see cref="PermissionAuthorizationOptions.ForbiddenMessageResolver"/> when present. Delegates all
/// other outcomes (including 401 challenges) to the default handler.
/// </summary>
internal sealed class ForbiddenResultHandler : IAuthorizationMiddlewareResultHandler
{
    private const string DefaultMessage = "Forbidden: you do not have permission to perform this action.";

    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();
    private readonly PermissionAuthorizationOptions _options;

    public ForbiddenResultHandler(IOptions<PermissionAuthorizationOptions> options)
    {
        _options = options.Value;
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            var message = _options.ForbiddenMessageResolver?.Invoke(context) ?? DefaultMessage;

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = 403, message }));
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
