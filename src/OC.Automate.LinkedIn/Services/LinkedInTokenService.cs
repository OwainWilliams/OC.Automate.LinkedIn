using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OC.Automate.LinkedIn;

public class LinkedInTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<LinkedInSettings> _linkedInSettings;
    private readonly LinkedInTokenStore _tokenStore;
    private readonly ILogger<LinkedInTokenService> _logger;

    public LinkedInTokenService(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<LinkedInSettings> linkedInSettings,
        LinkedInTokenStore tokenStore,
        ILogger<LinkedInTokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _linkedInSettings = linkedInSettings;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(
        string connectionName,
        CancellationToken cancellationToken)
    {
        if (!_tokenStore.HasTokens(connectionName))
        {
            throw new InvalidOperationException(
                $"No tokens found for connection '{connectionName}'. Please authorize the connection first.");
        }

        if (!_tokenStore.IsAccessTokenExpired(connectionName))
        {
            var existingToken = _tokenStore.GetAccessToken(connectionName);
            if (!string.IsNullOrWhiteSpace(existingToken))
            {
                return existingToken;
            }
        }

        return await RefreshAccessTokenAsync(connectionName, cancellationToken);
    }

    private async Task<string> RefreshAccessTokenAsync(
        string connectionName,
        CancellationToken cancellationToken)
    {
        var refreshToken = _tokenStore.GetRefreshToken(connectionName);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidOperationException(
                $"No refresh token found for connection '{connectionName}'. Please re-authorize.");
        }

        var settings = _linkedInSettings.CurrentValue;
        var httpClient = _httpClientFactory.CreateClient();

        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = settings.ClientId,
            ["client_secret"] = settings.ClientSecret
        });

        var response = await httpClient.PostAsync(
            "https://www.linkedin.com/oauth/v2/accessToken",
            tokenRequest,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("LinkedIn token refresh failed {StatusCode}: {Body}", response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Failed to refresh LinkedIn access token. Status: {response.StatusCode}");
        }

        var tokenResponse = await response.Content
            .ReadFromJsonAsync<LinkedInTokenResponse>(cancellationToken);

        if (tokenResponse?.AccessToken is null)
        {
            throw new HttpRequestException("LinkedIn token refresh returned no access token.");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        var newRefreshToken = tokenResponse.RefreshToken ?? refreshToken;

        _tokenStore.StoreTokens(connectionName, tokenResponse.AccessToken, newRefreshToken, expiresAt);

        _logger.LogInformation(
            "LinkedIn access token refreshed for connection '{ConnectionName}', expires in {ExpiresIn}s",
            connectionName, tokenResponse.ExpiresIn);

        return tokenResponse.AccessToken;
    }

    public async Task<string> ExchangeAuthorizationCodeAsync(
        string connectionName,
        string authorizationCode,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var settings = _linkedInSettings.CurrentValue;
        var httpClient = _httpClientFactory.CreateClient();

        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = authorizationCode,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = settings.ClientId,
            ["client_secret"] = settings.ClientSecret
        });

        var response = await httpClient.PostAsync(
            "https://www.linkedin.com/oauth/v2/accessToken",
            tokenRequest,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("LinkedIn token exchange failed {StatusCode}: {Body}", response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Failed to exchange authorization code. Status: {response.StatusCode}");
        }

        var tokenResponse = await response.Content
            .ReadFromJsonAsync<LinkedInTokenResponse>(cancellationToken);

        if (tokenResponse?.AccessToken is null)
        {
            throw new HttpRequestException("LinkedIn token exchange returned no access token.");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        _tokenStore.StoreTokens(connectionName, tokenResponse.AccessToken, tokenResponse.RefreshToken ?? string.Empty, expiresAt);

        _logger.LogInformation("LinkedIn tokens stored for connection '{ConnectionName}'", connectionName);

        return tokenResponse.AccessToken;
    }
}
