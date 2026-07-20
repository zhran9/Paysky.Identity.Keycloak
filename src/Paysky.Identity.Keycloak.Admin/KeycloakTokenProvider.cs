using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Paysky.Identity.Keycloak;

/// <summary>
/// Thread-safe Keycloak admin token provider. Caches the token, refreshes it before expiry, and
/// supports both the <c>client_credentials</c> (default) and <c>password</c> grants. Registered as a singleton.
/// </summary>
internal sealed class KeycloakTokenProvider : IKeycloakTokenProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakAdminOptions _admin;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private CachedAdminToken? _cached;

    public KeycloakTokenProvider(IHttpClientFactory httpClientFactory, IOptions<KeycloakOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _admin = options.Value.Admin
            ?? throw new InvalidOperationException("Keycloak:Admin configuration is required for the admin token provider.");
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var buffer = TimeSpan.FromSeconds(_admin.TokenExpiryBufferSeconds);

        if (IsValid(_cached, buffer))
            return _cached!.AccessToken;

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsValid(_cached, buffer))
                return _cached!.AccessToken;

            if (_cached?.RefreshToken is { Length: > 0 } refreshToken)
            {
                var refreshed = await RequestTokenAsync(BuildRefreshForm(refreshToken), cancellationToken).ConfigureAwait(false);
                if (refreshed is not null)
                {
                    _cached = refreshed;
                    return refreshed.AccessToken;
                }
            }

            var token = await RequestTokenAsync(BuildGrantForm(), cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Failed to obtain a Keycloak admin token.");

            _cached = token;
            return token.AccessToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static bool IsValid(CachedAdminToken? token, TimeSpan buffer)
        => token is not null && token.ExpiresAt - buffer > DateTimeOffset.UtcNow;

    private Dictionary<string, string> BuildGrantForm()
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = _admin.GrantType,
            ["client_id"] = _admin.ClientId
        };

        if (!string.IsNullOrEmpty(_admin.ClientSecret))
            form["client_secret"] = _admin.ClientSecret!;

        if (_admin.GrantType == KeycloakGrantTypes.Password)
        {
            form["username"] = _admin.Username
                ?? throw new InvalidOperationException("Keycloak:Admin:Username is required for the password grant.");
            form["password"] = _admin.Password
                ?? throw new InvalidOperationException("Keycloak:Admin:Password is required for the password grant.");
        }

        return form;
    }

    private Dictionary<string, string> BuildRefreshForm(string refreshToken)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = KeycloakGrantTypes.RefreshToken,
            ["client_id"] = _admin.ClientId,
            ["refresh_token"] = refreshToken
        };

        if (!string.IsNullOrEmpty(_admin.ClientSecret))
            form["client_secret"] = _admin.ClientSecret!;

        return form;
    }

    private async Task<CachedAdminToken?> RequestTokenAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(KeycloakAdminDefaults.HttpClientName);
        using var content = new FormUrlEncodedContent(form);

        using var response = await client
            .PostAsync(KeycloakEndpoints.Token(_admin.TokenRealm), content, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        if (string.IsNullOrEmpty(accessToken))
            return null;

        var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = root.TryGetProperty("expires_in", out var ei) && ei.TryGetInt32(out var seconds) ? seconds : 60;

        return new CachedAdminToken(accessToken!, refreshToken, DateTimeOffset.UtcNow.AddSeconds(expiresIn));
    }
}
