using Umbraco.Automate.Core.Settings;

namespace OC.Automate.LinkedIn;

public class LinkedInConnectionSettings
{
    [Field(
        Label = "Author URN",
        Description = "Your LinkedIn author URN (e.g. urn:li:person:abc123 or urn:li:organization:12345)")]
    public string AuthorUrn { get; set; } = string.Empty;

    [Field(
        Label = "Connection Name",
        Description = "A unique name for this connection (used to link authorization and token storage)",
        SortOrder = 1)]
    public string ConnectionName { get; set; } = string.Empty;
}
