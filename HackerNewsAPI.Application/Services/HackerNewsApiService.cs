using Microsoft.Extensions.Logging;
using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Infrastructure.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HackerNewsAPI.Application.Services;

public class HackerNewsApiService : IHackerNewsApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HackerNewsApiService> _logger;
    private readonly SemaphoreSlim _apiSemaphore;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    public HackerNewsApiService(HttpClient httpClient, ILogger<HackerNewsApiService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiSemaphore = new SemaphoreSlim(5, 5); // Max 5 concurrent API requests
    }

    public async Task<Item?> GetItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        await _apiSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Fetching item {ItemId} from Hacker News API", itemId);
            var response = await _httpClient.GetAsync($"https://hacker-news.firebaseio.com/v0/item/{itemId}.json", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch item {ItemId}: {StatusCode}", itemId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var item = JsonSerializer.Deserialize<Item>(json, JsonOptions);

            if (item != null)
            {
                _logger.LogDebug("Successfully fetched item {ItemId}", itemId);
            }

            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching item {ItemId} from Hacker News API", itemId);
            throw;
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<IEnumerable<int>> GetTopStoryIdsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching top story IDs from Hacker News API");
            var response = await _httpClient.GetAsync("https://hacker-news.firebaseio.com/v0/topstories.json?print=pretty", cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var storyIds = JsonSerializer.Deserialize<IEnumerable<int>>(json) ?? Enumerable.Empty<int>();

            _logger.LogDebug("Successfully fetched {Count} story IDs", storyIds.Count());

            return storyIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top story IDs from Hacker News API");
            throw;
        }
    }
}
