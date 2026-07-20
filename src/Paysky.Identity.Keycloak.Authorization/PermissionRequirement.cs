using Microsoft.AspNetCore.Authorization;

namespace Paysky.Identity.Keycloak;

/// <summary>
/// Marker requirement for the permission policy. The concrete permission is read from the endpoint's
/// <see cref="PermissionAttribute"/> at evaluation time, so a single policy serves every permission.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement;
