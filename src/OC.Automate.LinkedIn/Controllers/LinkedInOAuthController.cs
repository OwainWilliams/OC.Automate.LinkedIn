using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OC.Automate.LinkedIn.Controllers;

[Route("umbraco/api/linkedin")]
public class LinkedInOAuthController : Controller
{
    private readonly IOptionsMonitor<LinkedInSettings> _linkedInSettings;
    private readonly LinkedInTokenService _tokenService;
    private readonly LinkedInTokenStore _tokenStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LinkedInOAuthController> _logger;

    public LinkedInOAuthController(
        IOptionsMonitor<LinkedInSettings> linkedInSettings,
        LinkedInTokenService tokenService,
        LinkedInTokenStore tokenStore,
        IHttpClientFactory httpClientFactory,
        ILogger<LinkedInOAuthController> logger)
    {
        _linkedInSettings = linkedInSettings;
        _tokenService = tokenService;
        _tokenStore = tokenStore;
        _httpClientFactory = httpClientFactory;
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
            var accessToken = await _tokenService.ExchangeAuthorizationCodeAsync(
                connectionName,
                code,
                settings.AuthorizeRedirectUri,
                cancellationToken);

            var (personUrn, urnError) = await FetchPersonUrnAsync(accessToken, cancellationToken);
            if (!string.IsNullOrWhiteSpace(personUrn))
            {
                _tokenStore.StoreAuthorUrn(connectionName, personUrn);
            }

            var urnHtml = string.IsNullOrWhiteSpace(personUrn)
                ? $"<p>Could not retrieve your Author URN automatically.</p><pre>{System.Web.HttpUtility.HtmlEncode(urnError ?? "Unknown error")}</pre>"
                : $"<p><strong>Author URN:</strong> <code>{personUrn}</code> (saved automatically)</p>";

            return Content(
                $"<html><body>" +
                $"<h1>LinkedIn Connected!</h1>" +
                $"<p>Connection <strong>{connectionName}</strong> has been authorized successfully.</p>" +
                $"{urnHtml}" +
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

    [HttpGet("me")]
    public async Task<IActionResult> Me(string connectionName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionName))
        {
            return BadRequest("connectionName is required.");
        }

        try
        {
            var accessToken = await _tokenService.GetAccessTokenAsync(connectionName, cancellationToken);
            var (personUrn, urnError) = await FetchPersonUrnAsync(accessToken, cancellationToken);

            if (string.IsNullOrWhiteSpace(personUrn))
            {
                return Content("<html><body><h1>Could not retrieve URN</h1><p>The profile endpoint did not return a sub claim.</p></body></html>", "text/html");
            }

            return Content(
                $"<html><body>" +
                $"<h1>Your LinkedIn Author URN</h1>" +
                $"<p><code>{personUrn}</code></p>" +
                $"<p>Copy this into the <strong>Author URN</strong> field in your connection settings.</p>" +
                $"</body></html>",
                "text/html");
        }
        catch (Exception ex)
        {
            return Content($"<html><body><h1>Error</h1><p>{ex.Message}</p></body></html>", "text/html");
        }
    }

    private async Task<(string? Urn, string? Error)> FetchPersonUrnAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.linkedin.com/v2/userinfo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("LinkedIn /v2/me returned {Status}: {Body}", response.StatusCode, body);
                return (null, $"{response.StatusCode}: {body}");
            }

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("sub", out var sub))
            {
                return ($"urn:li:person:{sub.GetString()}", null);
            }

            return (null, $"No 'id' property in response: {body}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch LinkedIn person URN");
            return (null, ex.Message);
        }
    }
}
