using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Paysky.Identity.Keycloak;

/// <inheritdoc cref="IKeycloakLoginBroker"/>
internal sealed class KeycloakLoginBroker : IKeycloakLoginBroker
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakLoginOptions _login;

    public KeycloakLoginBroker(IHttpClientFactory httpClientFactory, IOptions<KeycloakOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _login = options.Value.Login
            ?? throw new InvalidOperationException("Keycloak:Login configuration is required for the login broker.");
    }

    public Task<KeycloakResult<TokenPair>> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var form = WithClientCredentials(new Dictionary<string, string>
        {
            ["grant_type"] = KeycloakGrantTypes.Password,
            ["username"] = username,
            ["password"] = password,
            ["scope"] = _login.DefaultScopes
        });

        return RequestTokenAsync(form, cancellationToken);
    }

    public Task<KeycloakResult<TokenPair>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var form = WithClientCredentials(new Dictionary<string, string>
        {
            ["grant_type"] = KeycloakGrantTypes.RefreshToken,
            ["refresh_token"] = refreshToken
        });

        return RequestTokenAsync(form, cancellationToken);
    }

    public async Task<KeycloakResult> LogoutAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(KeycloakLoginDefaults.HttpClientName);

        using var request = new HttpRequestMessage(HttpMethod.Post, KeycloakLoginDefaults.Logout(_login.Realm));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new FormUrlEncodedContent(WithClientCredentials(new Dictionary<string, string>()));

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
            return KeycloakResult.Ok(response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return KeycloakResult.Fail(KeycloakLoginErrorParser.Extract(raw, "Logout failed."), response.StatusCode);
    }

    public async Task<KeycloakResult<KeycloakUserInfo>> GetUserFromTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(KeycloakLoginDefaults.HttpClientName);

        using var request = new HttpRequestMessage(HttpMethod.Get, KeycloakLoginDefaults.UserInfo(_login.Realm));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return KeycloakResult<KeycloakUserInfo>.Fail(KeycloakLoginErrorParser.Extract(raw, "Failed to resolve user info."), response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var id = root.TryGetProperty("sub", out var sub) ? sub.GetString() ?? string.Empty : string.Empty;
        var username = root.TryGetProperty(KeycloakClaimNames.PreferredUsername, out var u) ? u.GetString() : null;
        var email = root.TryGetProperty("email", out var e) ? e.GetString() : null;
        var firstName = root.TryGetProperty("given_name", out var g) ? g.GetString() : null;
        var lastName = root.TryGetProperty("family_name", out var f) ? f.GetString() : null;

        var info = new KeycloakUserInfo(id, username ?? email ?? string.Empty, email, firstName, lastName, Array.Empty<string>());
        return KeycloakResult<KeycloakUserInfo>.Ok(info);
    }

    private Dictionary<string, string> WithClientCredentials(Dictionary<string, string> form)
    {
        form["client_id"] = _login.ClientId;
        if (!string.IsNullOrEmpty(_login.ClientSecret))
            form["client_secret"] = _login.ClientSecret!;
        return form;
    }

    private async Task<KeycloakResult<TokenPair>> RequestTokenAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(KeycloakLoginDefaults.HttpClientName);
        using var content = new FormUrlEncodedContent(form);

        using var response = await client
            .PostAsync(KeycloakLoginDefaults.Token(_login.Realm), content, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return KeycloakResult<TokenPair>.Fail(KeycloakLoginErrorParser.Extract(body, "Authentication failed."), response.StatusCode);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        if (string.IsNullOrEmpty(accessToken))
            return KeycloakResult<TokenPair>.Fail("Token response did not contain an access token.", response.StatusCode);

        var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = root.TryGetProperty("expires_in", out var ei) && ei.TryGetInt32(out var seconds) ? seconds : 0;

        return KeycloakResult<TokenPair>.Ok(new TokenPair(accessToken!, refreshToken, expiresIn));
    }
}
