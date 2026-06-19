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
        Description = "A friendly name for this connection (used internally for token caching)",
        SortOrder = 1)]
    public string ConnectionName { get; set; } = string.Empty;

    [Field(
        Label = "Refresh Token",
        Description = "Your LinkedIn OAuth2 refresh token. Get this from the LinkedIn Developer Portal (see README for steps).",
        SortOrder = 2)]
    public string RefreshToken { get; set; } = string.Empty;
}
