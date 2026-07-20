namespace Paysky.Identity.Keycloak;

/// <summary>
/// Marker for the Paysky.Identity.Keycloak meta-package. The package carries no code of its own —
/// it aggregates the Authentication, Authorization, Admin, and Login packages so a service can
/// reference one package for the full toolkit.
/// </summary>
public static class PayskyKeycloak
{
    /// <summary>The configuration section name every package binds from.</summary>
    public const string ConfigurationSection = KeycloakOptions.SectionName;
}
