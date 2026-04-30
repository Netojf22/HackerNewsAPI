using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using HackerNewsAPI.Domain.Entities;
using HackerNewsAPI.Domain.Interfaces;
using System.Text.Json;

namespace HackerNewsAPI.Infrastructure.Repositories;

public class HackerNewsRepository : IHackerNewsRepository
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HackerNewsRepository> _logger;
    private readonly IMemoryCache _cache;

    private const string BestStoriesCacheKey = "best_stories";
    private const string StoryCacheKeyPrefix = "story_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public HackerNewsRepository(HttpClient httpClient, ILogger<HackerNewsRepository> logger, IMemoryCache cache)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<IEnumerable<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(BestStoriesCacheKey, out IEnumerable<int>? cachedIds))
        {
            _logger.LogDebug("Retrieved best story IDs from cache");
            return cachedIds ?? Enumerable.Empty<int>();
        }

        try
        {
            _logger.LogDebug("Fetching best story IDs from Hacker News API");
            var response = await _httpClient.GetAsync("https://hacker-news.firebaseio.com/v0/beststories.json", cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var storyIds = JsonSerializer.Deserialize<IEnumerable<int>>(json) ?? Enumerable.Empty<int>();
            
            _cache.Set(BestStoriesCacheKey, storyIds, CacheDuration);
            _logger.LogDebug("Successfully fetched and cached {Count} story IDs", storyIds.Count());
            
            return storyIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching best story IDs from Hacker News API");
            throw;
        }
    }

    public async Task<Story?> GetStoryDetailsAsync(int storyId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{StoryCacheKeyPrefix}{storyId}";
        
        if (_cache.TryGetValue(cacheKey, out Story? cachedStory))
        {
            _logger.LogDebug("Retrieved story {StoryId} from cache", storyId);
            return cachedStory;
        }

        try
        {
            _logger.LogDebug("Fetching story {StoryId} details from Hacker News API", storyId);
            var response = await _httpClient.GetAsync($"https://hacker-news.firebaseio.com/v0/item/{storyId}.json", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch story {StoryId}: {StatusCode}", storyId, response.StatusCode);
                return null;
            }
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var story = JsonSerializer.Deserialize<Story>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (story != null)
            {
                _cache.Set(cacheKey, story, CacheDuration);
                _logger.LogDebug("Successfully fetched and cached story {StoryId}", storyId);
            }
            
            return story;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching story {StoryId} details from Hacker News API", storyId);
            throw;
        }
    }
}
