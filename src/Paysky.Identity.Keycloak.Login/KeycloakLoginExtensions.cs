using Microsoft.Extensions.Configuration;
using Paysky.Identity.Keycloak;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the Keycloak user login/session broker.</summary>
public static class KeycloakLoginExtensions
{
    /// <summary>
    /// Adds the login broker from the given configuration section. Requires a <c>Keycloak:Login</c>
    /// subsection. Throws <see cref="InvalidOperationException"/> when required options are missing.
    /// </summary>
    public static IServiceCollection AddPayskyKeycloakLogin(
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

        var login = options.Login
            ?? throw new InvalidOperationException($"'{sectionName}:Login' is required for the login broker.");

        if (string.IsNullOrWhiteSpace(login.Realm))
            throw new InvalidOperationException($"'{sectionName}:Login:Realm' is required.");
        if (string.IsNullOrWhiteSpace(login.ClientId))
            throw new InvalidOperationException($"'{sectionName}:Login:ClientId' is required.");

        services.Configure<KeycloakOptions>(section);

        services.AddHttpClient(KeycloakLoginDefaults.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        services.AddScoped<IKeycloakLoginBroker, KeycloakLoginBroker>();

        return services;
    }
}
