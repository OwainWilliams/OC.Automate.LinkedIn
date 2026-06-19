using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OC.Automate.LinkedIn.Controllers;

[Route("umbraco/api/linkedin")]
public class LinkedInOAuthController : Controller
{
    private readonly IOptionsMonitor<LinkedInSettings> _linkedInSettings;
    private readonly LinkedInTokenService _tokenService;
    private readonly ILogger<LinkedInOAuthController> _logger;

    public LinkedInOAuthController(
        IOptionsMonitor<LinkedInSettings> linkedInSettings,
        LinkedInTokenService tokenService,
        ILogger<LinkedInOAuthController> logger)
    {
        _linkedInSettings = linkedInSettings;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpGet("authorize")]
    public IActionResult Authorize(string connectionName)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
        {
            return BadRequest("connectionName is required.");
        }

        var settings = _linkedInSettings.CurrentValue;

        if (string.IsNullOrWhiteSpace(settings.ClientId))
        {
            return BadRequest("LinkedIn ClientId is not configured in appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(settings.AuthorizeRedirectUri))
        {
            return BadRequest("LinkedIn AuthorizeRedirectUri is not configured in appsettings.json.");
        }

        var scopes = "openid profile w_member_social";
        var state = connectionName;

        var authorizationUrl =
            $"https://www.linkedin.com/oauth/v2/authorization" +
            $"?response_type=code" +
            $"&client_id={Uri.EscapeDataString(settings.ClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(settings.AuthorizeRedirectUri)}" +
            $"&scope={Uri.EscapeDataString(scopes)}" +
            $"&state={Uri.EscapeDataString(state)}";

        return Redirect(authorizationUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        string? code,
        string? state,
        string? error,
        string? error_description,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            _logger.LogError("LinkedIn OAuth error: {Error} - {Description}", error, error_description);
            return Content(
                $"<html><body><h1>LinkedIn Authorization Failed</h1><p>{error_description ?? error}</p></body></html>",
                "text/html");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return BadRequest("Missing authorization code or state.");
        }

        var connectionName = state;
        var settings = _linkedInSettings.CurrentValue;

        try
        {
            await _tokenService.ExchangeAuthorizationCodeAsync(
                connectionName,
                code,
                settings.AuthorizeRedirectUri,
                cancellationToken);

            return Content(
                $"<html><body>" +
                $"<h1>LinkedIn Connected!</h1>" +
                $"<p>Connection <strong>{connectionName}</strong> has been authorized successfully.</p>" +
                $"<p>You can close this window and return to the Umbraco backoffice.</p>" +
                $"</body></html>",
                "text/html");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to exchange LinkedIn authorization code for connection '{ConnectionName}'", connectionName);
            return Content(
                $"<html><body><h1>Authorization Failed</h1><p>{ex.Message}</p></body></html>",
                "text/html");
        }
    }
}
