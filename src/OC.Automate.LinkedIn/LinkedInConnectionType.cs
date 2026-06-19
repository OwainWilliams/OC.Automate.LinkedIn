using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Connections;

namespace OC.Automate.LinkedIn;

[ConnectionType("linkedin", "LinkedIn")]
public class LinkedInConnectionType : ConnectionTypeBase<LinkedInConnectionSettings>
{
    private readonly LinkedInTokenService _tokenService;
    private readonly IOptionsMonitor<LinkedInSettings> _linkedInSettings;

    public LinkedInConnectionType(
        ConnectionTypeInfrastructure infrastructure,
        LinkedInTokenService tokenService,
        IOptionsMonitor<LinkedInSettings> linkedInSettings)
        : base(infrastructure)
    {
        _tokenService = tokenService;
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

        if (string.IsNullOrWhiteSpace(connectionSettings.RefreshToken))
        {
            return ConnectionValidationResult.Failure("Refresh Token is required.");
        }

        var linkedInSettings = _linkedInSettings.CurrentValue;

        if (string.IsNullOrWhiteSpace(linkedInSettings.ClientId) ||
            string.IsNullOrWhiteSpace(linkedInSettings.ClientSecret))
        {
            return ConnectionValidationResult.Failure(
                "LinkedIn ClientId and ClientSecret are required in appsettings.json.");
        }

        try
        {
            var accessToken = await _tokenService.GetAccessTokenAsync(
                connectionSettings.ConnectionName,
                connectionSettings.RefreshToken,
                cancellationToken);

            return ConnectionValidationResult.Success(
                $"LinkedIn connection validated for {connectionSettings.AuthorUrn}.");
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
