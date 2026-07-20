namespace Paysky.Identity.Keycloak;

/// <summary>A Keycloak user representation returned by the Admin REST API.</summary>
public sealed class KeycloakUser
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Enabled { get; set; }
    public IReadOnlyList<string> RequiredActions { get; set; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Attributes { get; set; }
        = new Dictionary<string, IReadOnlyList<string>>();
}

/// <summary>Fields for creating a Keycloak user. Password is set separately as a credential.</summary>
public sealed class CreateKeycloakUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Password { get; set; }
    public bool Enabled { get; set; } = true;
    /// <summary>When true, the temporary password must be changed on first login.</summary>
    public bool RequirePasswordChange { get; set; }
    public IDictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
}

/// <summary>Mutable fields for updating a Keycloak user. Null fields are left unchanged.</summary>
public sealed class UpdateKeycloakUserRequest
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool? Enabled { get; set; }
}
