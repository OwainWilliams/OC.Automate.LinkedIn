using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Umbraco.Automate.Core.Actions;

namespace OC.Automate.LinkedIn;

[Action("linkedInSendPost", "Send LinkedIn Post",
    Description = "Sends a LinkedIn Post",
    Group = "Social Networks",
    Icon = "icon-paper-plane",
    ConnectionTypeAlias = "linkedin")]
public class SendLinkedInPostAction : ActionBase<LinkedInPostSettings>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SendLinkedInPostAction> _logger;
    private readonly LinkedInTokenService _tokenService;
    private readonly LinkedInTokenStore _tokenStore;

    public SendLinkedInPostAction(
        ActionInfrastructure infrastructure,
        IHttpClientFactory httpClientFactory,
        ILogger<SendLinkedInPostAction> logger,
        LinkedInTokenService tokenService,
        LinkedInTokenStore tokenStore)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _tokenService = tokenService;
        _tokenStore = tokenStore;
    }

    public override async Task<ActionResult> ExecuteAsync(
        ActionContext context,
        CancellationToken cancellationToken)
    {
        var connectionSettings = context.Connection?.GetSettings<LinkedInConnectionSettings>();
        if (connectionSettings is null)
        {
            return ActionResult.Failed(
                new InvalidOperationException("Invalid connection settings."),
                StepRunErrorCategory.ConfigurationError);
        }

        if (string.IsNullOrWhiteSpace(connectionSettings.ConnectionName))
        {
            return ActionResult.Failed(
                new InvalidOperationException("Connection Name is required."),
                StepRunErrorCategory.ConfigurationError);
        }

        string accessToken;
        try
        {
            accessToken = await _tokenService.GetAccessTokenAsync(
                connectionSettings.ConnectionName,
                cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to obtain LinkedIn access token");
            return ActionResult.Failed(ex, StepRunErrorCategory.ConfigurationError);
        }

        var actionSettings = context.GetSettings<LinkedInPostSettings>();
        if (actionSettings is null || string.IsNullOrWhiteSpace(actionSettings.Content))
        {
            return ActionResult.Failed(
                new InvalidOperationException("Post content is required."),
                StepRunErrorCategory.Validation);
        }

        var postText = actionSettings.Content;
        if (!string.IsNullOrWhiteSpace(actionSettings.PostUrl))
        {
            postText += $"\n\n{actionSettings.PostUrl}";
        }

        var visibility = actionSettings.Visibility?.ToUpperInvariant() switch
        {
            "CONNECTIONS" => "CONNECTIONS",
            _ => "PUBLIC"
        };

        var authorUrn = _tokenStore.GetAuthorUrn(connectionSettings.ConnectionName);
        if (string.IsNullOrWhiteSpace(authorUrn))
        {
            return ActionResult.Failed(
                new InvalidOperationException("Author URN not found. Please re-authorize the connection."),
                StepRunErrorCategory.ConfigurationError);
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            var postBody = new
            {
                author = authorUrn,
                commentary = postText,
                visibility = visibility,
                distribution = new
                {
                    feedDistribution = "MAIN_FEED",
                    targetEntities = Array.Empty<object>(),
                    thirdPartyDistributionChannels = Array.Empty<object>()
                },
                lifecycleState = "PUBLISHED",
                isReshareDisabledByAuthor = false
            };

            var json = JsonSerializer.Serialize(postBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.linkedin.com/rest/posts")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
            request.Headers.Add("LinkedIn-Version", "202506");

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("LinkedIn API error {StatusCode}: {Body}", response.StatusCode, errorBody);
                return ActionResult.Failed(
                    new HttpRequestException($"LinkedIn API returned {response.StatusCode}: {errorBody}"),
                    StepRunErrorCategory.InvalidResponse);
            }

            _logger.LogInformation("Successfully posted to LinkedIn for {AuthorUrn}", authorUrn);
            return Success();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to LinkedIn API");
            return ActionResult.Failed(ex, StepRunErrorCategory.ServiceUnavailable);
        }
        catch (TaskCanceledException ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.Timeout);
        }
    }
}
