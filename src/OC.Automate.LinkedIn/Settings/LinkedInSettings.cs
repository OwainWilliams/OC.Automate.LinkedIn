namespace OC.Automate.LinkedIn;

public class LinkedInSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public Dictionary<string, string> AccessTokens { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
