using HackerNewsAPI.Domain.Enums;

namespace HackerNewsAPI.Infrastructure.Entities;

public class Item
{
    public int Id { get; set; }
    public string By { get; set; } = string.Empty;
    public int Descendants { get; set; }
    public List<int>? Kids { get; set; }
    public int Score { get; set; }
    public long Time { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public ItemType Type { get; set; }
    public DateTime CachedAt { get; set; }
}
