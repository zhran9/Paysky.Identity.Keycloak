using Microsoft.Extensions.Configuration;
using Paysky.Identity.Keycloak;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the Keycloak Admin REST client (token provider + user/role/provisioning services).</summary>
public static class KeycloakAdminExtensions
{
    /// <summary>
    /// Adds the Keycloak Admin client from the given configuration section. Requires a <c>Keycloak:Admin</c>
    /// subsection. Throws <see cref="InvalidOperationException"/> when required options are missing (fail fast).
    /// </summary>
    public static IServiceCollection AddPayskyKeycloakAdmin(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = KeycloakOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionName);
        var options = section.Get<KeycloakOptions>()
            ?? throw new InvalidOperationException($"Keycloak configuration section '{sectionName}' is missing or empty.");

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            throw new InvalidOperationException($"'{sectionName}:BaseUrl' is required.");

        var admin = options.Admin
            ?? throw new InvalidOperationException($"'{sectionName}:Admin' is required for the Keycloak Admin client.");

        ValidateAdmin(admin, sectionName);

        services.Configure<KeycloakOptions>(section);

        services.AddHttpClient(KeycloakAdminDefaults.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        services.AddSingleton<IKeycloakTokenProvider, KeycloakTokenProvider>();
        services.AddScoped<IKeycloakUserAdmin, KeycloakUserAdmin>();
        services.AddScoped<IKeycloakRoleAdmin, KeycloakRoleAdmin>();
        services.AddScoped<IKeycloakProvisioning, KeycloakProvisioning>();

        return services;
    }

    private static void ValidateAdmin(KeycloakAdminOptions admin, string sectionName)
    {
        if (string.IsNullOrWhiteSpace(admin.ClientId))
            throw new InvalidOperationException($"'{sectionName}:Admin:ClientId' is required.");

        if (admin.GrantType == KeycloakGrantTypes.ClientCredentials && string.IsNullOrWhiteSpace(admin.ClientSecret))
            throw new InvalidOperationException($"'{sectionName}:Admin:ClientSecret' is required for the client_credentials grant.");

        if (admin.GrantType == KeycloakGrantTypes.Password
            && (string.IsNullOrWhiteSpace(admin.Username) || string.IsNullOrWhiteSpace(admin.Password)))
        {
            throw new InvalidOperationException($"'{sectionName}:Admin:Username' and 'Password' are required for the password grant.");
        }
    }
}
