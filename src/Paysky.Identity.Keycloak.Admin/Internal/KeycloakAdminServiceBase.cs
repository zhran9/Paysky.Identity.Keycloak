using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Paysky.Identity.Keycloak;

/// <summary>Shared HTTP plumbing for the admin services: authorized send, realm resolution, JSON mapping.</summary>
internal abstract class KeycloakAdminServiceBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IKeycloakTokenProvider _tokenProvider;

    protected KeycloakAdminOptions Admin { get; }

    protected KeycloakAdminServiceBase(
        IHttpClientFactory httpClientFactory,
        IKeycloakTokenProvider tokenProvider,
        IOptions<KeycloakOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _tokenProvider = tokenProvider;
        Admin = options.Value.Admin
            ?? throw new InvalidOperationException("Keycloak:Admin configuration is required for admin operations.");
    }

    protected string ResolveRealm(string? realm)
    {
        var resolved = string.IsNullOrWhiteSpace(realm) ? Admin.ManagedRealm : realm!;
        if (string.IsNullOrWhiteSpace(resolved))
            throw new InvalidOperationException("No realm specified and Keycloak:Admin:ManagedRealm is not configured.");
        return resolved;
    }

    protected async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        var client = _httpClientFactory.CreateClient(KeycloakAdminDefaults.HttpClientName);

        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (body is not null)
        {
            var payload = JsonSerializer.Serialize(body, KeycloakAdminDefaults.JsonOptions);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        }

        return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    protected static async Task<KeycloakResult> FailAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return KeycloakResult.Fail(KeycloakErrorParser.Extract(raw, response.ReasonPhrase ?? "Keycloak request failed"), response.StatusCode);
    }

    protected static async Task<KeycloakResult<T>> FailAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return KeycloakResult<T>.Fail(KeycloakErrorParser.Extract(raw, response.ReasonPhrase ?? "Keycloak request failed"), response.StatusCode);
    }

    protected static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonDocument.Parse(json);
    }

    protected static string GetString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    protected static string? GetStringOrNull(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) ? value.GetString() : null;

    protected static bool GetBool(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;
}
