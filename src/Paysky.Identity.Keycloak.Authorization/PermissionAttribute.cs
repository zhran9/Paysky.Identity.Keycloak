using Microsoft.AspNetCore.Authorization;

namespace Paysky.Identity.Keycloak;

/// <summary>
/// Requires the caller to hold a specific permission, expressed as a Keycloak role of the same name.
/// Apply to a controller action or minimal-API endpoint: <c>[Permission("Users.Create")]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class PermissionAttribute : AuthorizeAttribute
{
    /// <summary>Name of the policy these attributes bind to.</summary>
    public const string PolicyName = "PayskyPermission";

    /// <summary>The permission (role name) the caller must have.</summary>
    public string PermissionName { get; }

    public PermissionAttribute(string permissionName)
    {
        PermissionName = permissionName;
        Policy = PolicyName;
    }
}
