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
        Description = "The key used to look up the access token in appsettings.json under OwainCodes:Automate:LinkedIn:AccessTokens",
        SortOrder = 1)]
    public string ConnectionName { get; set; } = string.Empty;
}
