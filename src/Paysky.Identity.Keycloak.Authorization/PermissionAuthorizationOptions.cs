using Microsoft.AspNetCore.Http;

namespace Paysky.Identity.Keycloak;

/// <summary>Options for the permission authorization pipeline.</summary>
public sealed class PermissionAuthorizationOptions
{
    /// <summary>
    /// Resolves the message returned in the 403 body. Receives the current request (e.g. to read
    /// <c>Accept-Language</c> for localization). Defaults to a fixed English message when not set.
    /// </summary>
    public Func<HttpContext, string>? ForbiddenMessageResolver { get; set; }
}
