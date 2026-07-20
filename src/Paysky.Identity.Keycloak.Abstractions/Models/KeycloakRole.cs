namespace Paysky.Identity.Keycloak;

/// <summary>A Keycloak realm role.</summary>
public sealed class KeycloakRole
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Composite { get; set; }
}
