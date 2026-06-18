using Umbraco.Automate.Core.Settings;

namespace OC.Automate.LinkedIn;

public class LinkedInPostSettings
{
    [Field(
        Label = "Content",
        Description = "The post content (max 3000 chars). Supports ${binding} syntax for dynamic values.",
        SupportsBindings = true)]
    public string Content { get; set; } = string.Empty;

    [Field(
        Label = "Post URL",
        Description = "Optional URL to append to the post content.",
        SortOrder = 1,
        SupportsBindings = true)]
    public string? PostUrl { get; set; }

    [Field(
        Label = "Visibility",
        Description = "Post visibility: PUBLIC or CONNECTIONS. Defaults to PUBLIC.",
        SortOrder = 2)]
    public string Visibility { get; set; } = "PUBLIC";
}
