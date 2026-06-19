using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OC.Automate.LinkedIn;

public class LinkedInTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<LinkedInSettings> _linkedInSettings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LinkedInTokenService> _logger;

    public LinkedInTokenService(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<LinkedInSettings> linkedInSettings,
        IMemoryCache cache,
        ILogger<LinkedInTokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _linkedInSettings = linkedInSettings;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(
        string connectionName,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"linkedin_token_{connectionName}";

        if (_cache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrWhiteSpace(cachedToken))
        {
            return cachedToken;
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

        // Cache with a buffer before actual expiry (refresh 5 minutes early)
        var expiry = TimeSpan.FromSeconds(Math.Max(tokenResponse.ExpiresIn - 300, 60));
        _cache.Set(cacheKey, tokenResponse.AccessToken, expiry);

        _logger.LogInformation(
            "LinkedIn access token refreshed for connection '{ConnectionName}', expires in {ExpiresIn}s",
            connectionName, tokenResponse.ExpiresIn);

        return tokenResponse.AccessToken;
    }
}
