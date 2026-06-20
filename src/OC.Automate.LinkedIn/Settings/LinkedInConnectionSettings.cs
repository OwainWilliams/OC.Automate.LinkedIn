using System.Text.Json.Serialization;
using Umbraco.Automate.Core.Settings;

namespace OC.Automate.LinkedIn;

public class LinkedInConnectionSettings
{
    [Field(
        Label = "Connection Name",
        Description = "A unique name for this connection (e.g. LinkedIn)")]
    public string ConnectionName { get; set; } = string.Empty;

    [Field(
        Label = "Authorize",
        Description = "Click to authorize this connection with LinkedIn",
        SortOrder = 1,
        EditorUiAlias = "OC.Automate.LinkedIn.AuthorizeButton")]
    [JsonIgnore]
    public string? Authorize { get; set; }
}
