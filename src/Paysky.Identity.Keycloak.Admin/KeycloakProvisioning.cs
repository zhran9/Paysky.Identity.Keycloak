using System.Net;
using Microsoft.Extensions.Options;

namespace Paysky.Identity.Keycloak;

/// <inheritdoc cref="IKeycloakProvisioning"/>
internal sealed class KeycloakProvisioning : KeycloakAdminServiceBase, IKeycloakProvisioning
{
    public KeycloakProvisioning(IHttpClientFactory httpClientFactory, IKeycloakTokenProvider tokenProvider, IOptions<KeycloakOptions> options)
        : base(httpClientFactory, tokenProvider, options)
    {
    }

    public async Task<KeycloakResult> EnsureRealmAsync(string realm, CancellationToken cancellationToken = default)
    {
        using var check = await SendAsync(HttpMethod.Get, KeycloakEndpoints.Realm(realm), null, cancellationToken).ConfigureAwait(false);
        if (check.IsSuccessStatusCode)
            return KeycloakResult.Ok();

        if (check.StatusCode != HttpStatusCode.NotFound)
            return await FailAsync(check, cancellationToken).ConfigureAwait(false);

        var body = new { realm, enabled = true };
        using var create = await SendAsync(HttpMethod.Post, KeycloakEndpoints.Realms(), body, cancellationToken).ConfigureAwait(false);
        return create.IsSuccessStatusCode ? KeycloakResult.Ok(create.StatusCode) : await FailAsync(create, cancellationToken).ConfigureAwait(false);
    }

    public async Task<KeycloakResult<string>> CreateConfidentialClientAsync(string clientId, string realm, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            clientId,
            enabled = true,
            protocol = "openid-connect",
            publicClient = false,
            serviceAccountsEnabled = true,
            standardFlowEnabled = true,
            directAccessGrantsEnabled = false
        };

        using var response = await SendAsync(HttpMethod.Post, KeycloakEndpoints.Clients(realm), body, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return await FailAsync<string>(response, cancellationToken).ConfigureAwait(false);

        var id = response.Headers.Location?.ToString().Split('/').LastOrDefault();
        return string.IsNullOrEmpty(id)
            ? KeycloakResult<string>.Fail("Client created but its id could not be resolved from the response.", response.StatusCode)
            : KeycloakResult<string>.Ok(id!, response.StatusCode);
    }
}
