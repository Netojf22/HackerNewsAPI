using System.Text.Json.Serialization;

namespace HackerNewsAPI.Domain.Entities;

public class Story
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string Uri { get; set; } = string.Empty;
    
    [JsonPropertyName("by")]
    public string PostedBy { get; set; } = string.Empty;
    
    [JsonPropertyName("time")]
    public long UnixTime { get; set; }
    
    [JsonIgnore]
    public DateTime Time => DateTimeOffset.FromUnixTimeSeconds(UnixTime).UtcDateTime;
    
    [JsonPropertyName("score")]
    public int Score { get; set; }
    
    [JsonPropertyName("descendants")]
    public int CommentCount { get; set; }
}
