using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Automate.Core.Actions;

namespace OC.Automate.LinkedIn;

[Action("linkedInSendPost", "Send LinkedIn Post", ConnectionTypeAlias = "linkedin")]
public class SendLinkedInPostAction : ActionBase<LinkedInPostSettings>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SendLinkedInPostAction> _logger;
    private readonly IOptionsMonitor<LinkedInSettings> _linkedInSettings;

    public SendLinkedInPostAction(
        ActionInfrastructure infrastructure,
        IHttpClientFactory httpClientFactory,
        ILogger<SendLinkedInPostAction> logger,
        IOptionsMonitor<LinkedInSettings> linkedInSettings)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _linkedInSettings = linkedInSettings;
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

        if (string.IsNullOrWhiteSpace(connectionSettings.AuthorUrn))
        {
            return ActionResult.Failed(
                new InvalidOperationException("Author URN is required."),
                StepRunErrorCategory.ConfigurationError);
        }

        if (string.IsNullOrWhiteSpace(connectionSettings.ConnectionName))
        {
            return ActionResult.Failed(
                new InvalidOperationException("Connection Name is required."),
                StepRunErrorCategory.ConfigurationError);
        }

        var linkedInSettings = _linkedInSettings.CurrentValue;

        if (!linkedInSettings.AccessTokens.TryGetValue(connectionSettings.ConnectionName, out var accessToken)
            || string.IsNullOrWhiteSpace(accessToken))
        {
            return ActionResult.Failed(
                new InvalidOperationException(
                    $"No access token found for connection name '{connectionSettings.ConnectionName}'."),
                StepRunErrorCategory.ConfigurationError);
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

        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            var postBody = new
            {
                author = connectionSettings.AuthorUrn,
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
                    new HttpRequestException($"LinkedIn API returned {response.StatusCode}."),
                    StepRunErrorCategory.InvalidResponse);
            }

            _logger.LogInformation("Successfully posted to LinkedIn for {AuthorUrn}", connectionSettings.AuthorUrn);
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
