using System.Text.Json.Serialization;

namespace OC.Automate.LinkedIn;

public sealed class LinkedInTokenIntrospectionResponse
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("expires_at")]
    public long? ExpiresAt { get; set; }
}
