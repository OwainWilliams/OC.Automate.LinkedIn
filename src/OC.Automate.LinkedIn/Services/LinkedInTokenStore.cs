using Umbraco.Cms.Core.Services;

namespace OC.Automate.LinkedIn;

public class LinkedInTokenStore
{
    private const string KeyPrefix = "OC.Automate.LinkedIn";
    private readonly IKeyValueService _keyValueService;

    public LinkedInTokenStore(IKeyValueService keyValueService)
    {
        _keyValueService = keyValueService;
    }

    public string? GetAccessToken(string connectionName)
        => _keyValueService.GetValue($"{KeyPrefix}:{connectionName}:AccessToken");

    public string? GetRefreshToken(string connectionName)
        => _keyValueService.GetValue($"{KeyPrefix}:{connectionName}:RefreshToken");

    public string? GetExpiresAt(string connectionName)
        => _keyValueService.GetValue($"{KeyPrefix}:{connectionName}:ExpiresAt");

    public void StoreTokens(string connectionName, string accessToken, string refreshToken, DateTimeOffset expiresAt)
    {
        _keyValueService.SetValue($"{KeyPrefix}:{connectionName}:AccessToken", accessToken);
        _keyValueService.SetValue($"{KeyPrefix}:{connectionName}:RefreshToken", refreshToken);
        _keyValueService.SetValue($"{KeyPrefix}:{connectionName}:ExpiresAt", expiresAt.ToUnixTimeSeconds().ToString());
    }

    public bool HasTokens(string connectionName)
        => !string.IsNullOrWhiteSpace(GetRefreshToken(connectionName));

    public bool IsAccessTokenExpired(string connectionName)
    {
        var expiresAtStr = GetExpiresAt(connectionName);
        if (string.IsNullOrWhiteSpace(expiresAtStr) || !long.TryParse(expiresAtStr, out var expiresAtUnix))
        {
            return true;
        }

        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expiresAtUnix - 300;
    }
}
