using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Connections;

namespace OC.Automate.LinkedIn;

[ConnectionType("linkedin", "LinkedIn",
    Description = "Connect to LinkedIn",
    Icon = "icon-plugin")]
public class LinkedInConnectionType : ConnectionTypeBase<LinkedInConnectionSettings>
{
    private readonly LinkedInTokenService _tokenService;
    private readonly LinkedInTokenStore _tokenStore;
    private readonly IOptionsMonitor<LinkedInSettings> _linkedInSettings;

    public LinkedInConnectionType(
        ConnectionTypeInfrastructure infrastructure,
        LinkedInTokenService tokenService,
        LinkedInTokenStore tokenStore,
        IOptionsMonitor<LinkedInSettings> linkedInSettings)
        : base(infrastructure)
    {
        _tokenService = tokenService;
        _tokenStore = tokenStore;
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

        if (string.IsNullOrWhiteSpace(connectionSettings.ConnectionName))
        {
            return ConnectionValidationResult.Failure("Connection Name is required.");
        }

        var linkedInSettings = _linkedInSettings.CurrentValue;

        if (string.IsNullOrWhiteSpace(linkedInSettings.ClientId) ||
            string.IsNullOrWhiteSpace(linkedInSettings.ClientSecret))
        {
            return ConnectionValidationResult.Failure(
                "LinkedIn ClientId and ClientSecret are required in appsettings.json.");
        }

        if (!_tokenStore.HasTokens(connectionSettings.ConnectionName))
        {
            return ConnectionValidationResult.Failure(
                $"No tokens found for connection '{connectionSettings.ConnectionName}'. " +
                $"Please authorize first by visiting /umbraco/api/linkedin/authorize?connectionName={Uri.EscapeDataString(connectionSettings.ConnectionName)}");
        }

        try
        {
            await _tokenService.GetAccessTokenAsync(
                connectionSettings.ConnectionName,
                cancellationToken);

            var authorUrn = _tokenStore.GetAuthorUrn(connectionSettings.ConnectionName);
            var urnInfo = string.IsNullOrWhiteSpace(authorUrn) ? "" : $" ({authorUrn})";
            return ConnectionValidationResult.Success(
                $"LinkedIn connection validated{urnInfo}.");
        }
        catch (HttpRequestException ex)
        {
            return ConnectionValidationResult.Failure(
                $"Failed to validate LinkedIn connection: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return ConnectionValidationResult.Failure("Connection to LinkedIn timed out.");
        }
    }
}
