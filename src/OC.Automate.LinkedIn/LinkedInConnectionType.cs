using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Connections;

namespace OC.Automate.LinkedIn;

[ConnectionType("linkedin", "LinkedIn")]
public class LinkedInConnectionType : ConnectionTypeBase<LinkedInConnectionSettings>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<LinkedInSettings> _linkedInSettings;

    public LinkedInConnectionType(
        ConnectionTypeInfrastructure infrastructure,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<LinkedInSettings> linkedInSettings)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
        _linkedInSettings = linkedInSettings;
    }

    public override async Task<ConnectionValidationResult> ValidateAsync(
        object? settings,
        CancellationToken cancellationToken)
    {
        var connectionSettings = settings as LinkedInConnectionSettings;
        if (connectionSettings is null)
        {
            return ConnectionValidationResult.Failure("Invalid connection settings.");
        }

        if (string.IsNullOrWhiteSpace(connectionSettings.AuthorUrn))
        {
            return ConnectionValidationResult.Failure("Author URN is required.");
        }

        if (string.IsNullOrWhiteSpace(connectionSettings.ConnectionName))
        {
            return ConnectionValidationResult.Failure("Connection Name is required.");
        }

        var linkedInSettings = _linkedInSettings.CurrentValue;

        if (!linkedInSettings.AccessTokens.TryGetValue(connectionSettings.ConnectionName, out var accessToken)
            || string.IsNullOrWhiteSpace(accessToken))
        {
            return ConnectionValidationResult.Failure(
                $"No access token found for connection name '{connectionSettings.ConnectionName}' in configuration.");
        }

        if (string.IsNullOrWhiteSpace(linkedInSettings.ClientId) ||
            string.IsNullOrWhiteSpace(linkedInSettings.ClientSecret))
        {
            return ConnectionValidationResult.Failure(
                "LinkedIn ClientId and ClientSecret are required in configuration for token validation.");
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            var introspectContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = linkedInSettings.ClientId,
                ["client_secret"] = linkedInSettings.ClientSecret,
                ["token"] = accessToken
            });

            var response = await httpClient.PostAsync(
                "https://www.linkedin.com/oauth/v2/introspectToken",
                introspectContent,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ConnectionValidationResult.Failure(
                    $"Token introspection failed with status {response.StatusCode}.");
            }

            var introspectionResult = await response.Content
                .ReadFromJsonAsync<LinkedInTokenIntrospectionResponse>(cancellationToken);

            if (introspectionResult is null || !introspectionResult.Active)
            {
                return ConnectionValidationResult.Failure(
                    "The access token is invalid or expired. Please generate a new token.");
            }

            return ConnectionValidationResult.Success(
                $"LinkedIn connection validated for {connectionSettings.AuthorUrn}.");
        }
        catch (HttpRequestException ex)
        {
            return ConnectionValidationResult.Failure($"Failed to connect to LinkedIn: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return ConnectionValidationResult.Failure("Connection to LinkedIn timed out.");
        }
    }
}
