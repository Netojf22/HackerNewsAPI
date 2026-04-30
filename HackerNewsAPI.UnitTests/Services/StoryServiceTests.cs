using HackerNewsAPI.Application.Services;
using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Domain.Entities;
using HackerNewsAPI.Infrastructure.Interfaces;
using HackerNewsAPI.Domain.ValueObjects;
using HackerNewsAPI.Infrastructure.Entities;
using HackerNewsAPI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsAPI.Tests.Services;

public class StoryServiceTests
{
    private readonly Mock<IHackerNewsRepository> _mockRepository;
    private readonly Mock<IHackerNewsApiService> _mockApiService;
    private readonly Mock<ILogger<StoryService>> _mockLogger;
    private readonly StoryService _storyService;

    public StoryServiceTests()
    {
        _mockRepository = new Mock<IHackerNewsRepository>();
        _mockApiService = new Mock<IHackerNewsApiService>();
        _mockLogger = new Mock<ILogger<StoryService>>();
        _storyService = new StoryService(_mockRepository.Object, _mockApiService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldReturnStoriesOrderedByScoreDescending()
    {
        var storyIds = new[] { 1, 2, 3 };
        var items = new[]
        {
            new Item { Id = 1, By = "user1", Title = "Story 1", Score = 100, Descendants = 10, Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Url = "http://example.com/1", Type = ItemType.Story, CachedAt = DateTime.UtcNow },
            new Item { Id = 2, By = "user2", Title = "Story 2", Score = 300, Descendants = 20, Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Url = "http://example.com/2", Type = ItemType.Story, CachedAt = DateTime.UtcNow },
            new Item { Id = 3, By = "user3", Title = "Story 3", Score = 200, Descendants = 15, Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Url = "http://example.com/3", Type = ItemType.Story, CachedAt = DateTime.UtcNow }
        };

        _mockApiService.Setup(s => s.GetTopStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _mockRepository.Setup(r => r.IsItemExpiredAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) => items.First(i => i.Id == id));

        var result = await _storyService.GetBestStoriesAsync(3);

        Assert.Equal(3, result.Count());
        var storyArray = result.ToArray();
        Assert.Equal("Story 2", storyArray[0].Title); // Highest score
        Assert.Equal("Story 3", storyArray[1].Title); // Medium score
        Assert.Equal("Story 1", storyArray[2].Title); // Lowest score
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldReturnRequestedNumberOfStories()
    {
        var storyIds = new[] { 1, 2, 3, 4, 5 };
        var items = storyIds.Select(id => new Item 
        { 
            Id = id, 
            By = $"user{id}",
            Title = $"Story {id}", 
            Score = id * 100, 
            Descendants = id * 10,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = $"http://example.com/{id}",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow
        }).ToArray();

        _mockApiService.Setup(s => s.GetTopStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _mockRepository.Setup(r => r.IsItemExpiredAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) => items.First(i => i.Id == id));

        var result = await _storyService.GetBestStoriesAsync(3);

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldFilterOutNullStories()
    {
        var storyIds = new[] { 1, 2, 3 };
        var items = new[]
        {
            new Item { Id = 1, By = "user1", Title = "Story 1", Score = 100, Descendants = 10, Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Url = "http://example.com/1", Type = ItemType.Story, CachedAt = DateTime.UtcNow },
            null,
            new Item { Id = 3, By = "user3", Title = "Story 3", Score = 200, Descendants = 15, Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Url = "http://example.com/3", Type = ItemType.Story, CachedAt = DateTime.UtcNow }
        };

        _mockApiService.Setup(s => s.GetTopStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _mockRepository.Setup(r => r.IsItemExpiredAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) => items[id - 1]);

        var result = await _storyService.GetBestStoriesAsync(3);

        Assert.Equal(2, result.Count());
        var storyArray = result.ToArray();
        Assert.Equal("Story 3", storyArray[0].Title); // Higher score comes first
        Assert.Equal("Story 1", storyArray[1].Title);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldFetchStoriesInParallel()
    {
        var storyIds = new[] { 1, 2, 3, 4, 5 };
        var items = storyIds.Select(id => new Item 
        { 
            Id = id, 
            By = $"user{id}",
            Title = $"Story {id}", 
            Score = id * 100, 
            Descendants = id * 10,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = $"http://example.com/{id}",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow
        }).ToArray();

        _mockApiService.Setup(s => s.GetTopStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _mockRepository.Setup(r => r.IsItemExpiredAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) => items.First(i => i.Id == id));

        var result = await _storyService.GetBestStoriesAsync(5);

        Assert.Equal(5, result.Count());
        
        // Verify that GetItemAsync was called for all story IDs (parallel execution)
        _mockApiService.Verify(s => s.GetItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldMapStoryToStoryDtoCorrectly()
    {
        var storyIds = new[] { 1 };
        var item = new Item 
        { 
            Id = 1, 
            By = "testuser",
            Title = "Test Story", 
            Score = 150, 
            Descendants = 25,
            Time = new DateTimeOffset(2023, 10, 15, 14, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
            Url = "https://example.com/test",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow
        };

        _mockApiService.Setup(s => s.GetTopStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _mockRepository.Setup(r => r.IsItemExpiredAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _storyService.GetBestStoriesAsync(1);

        var storyDto = result.First();
        Assert.Equal(item.Title, storyDto.Title);
        Assert.Equal(item.Url, storyDto.Uri);
        Assert.Equal(item.By, storyDto.PostedBy);
        Assert.Equal(item.Score, storyDto.Score);
        Assert.Equal(item.Descendants, storyDto.CommentCount);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldThrowExceptionWhenApiServiceFails()
    {
        _mockApiService.Setup(s => s.GetTopStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API service error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _storyService.GetBestStoriesAsync(5));
    }

    [Fact]
    public async Task GetStoryByIdAsync_ShouldReturnStoryFromCacheWhenNotExpired()
    {
        var storyId = 123;
        var item = new Item
        {
            Id = storyId,
            By = "testuser",
            Title = "Cached Story",
            Score = 100,
            Descendants = 25,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/cached",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        _mockRepository.Setup(r => r.IsItemExpiredAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.GetItemByIdAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _storyService.GetStoryByIdAsync(storyId);

        Assert.NotNull(result);
        Assert.Equal("Cached Story", result.Title);
        _mockApiService.Verify(s => s.GetItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStoryByIdAsync_ShouldFetchFromApiWhenExpired()
    {
        var storyId = 123;
        var item = new Item
        {
            Id = storyId,
            By = "testuser",
            Title = "Fresh Story",
            Score = 150,
            Descendants = 30,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/fresh",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.IsItemExpiredAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetItemAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _storyService.GetStoryByIdAsync(storyId);

        Assert.NotNull(result);
        Assert.Equal("Fresh Story", result.Title);
        _mockRepository.Verify(r => r.AddOrUpdateItemAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStoryByIdAsync_ShouldReturnNullWhenItemNotFound()
    {
        var storyId = 999;

        _mockRepository.Setup(r => r.IsItemExpiredAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetItemAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Item?)null);

        var result = await _storyService.GetStoryByIdAsync(storyId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStoryByIdAsync_ShouldReturnNullWhenItemIsNotStoryType()
    {
        var storyId = 123;
        var item = new Item
        {
            Id = storyId,
            By = "testuser",
            Title = "Comment",
            Score = 50,
            Descendants = 0,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Type = ItemType.Comment,
            CachedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.IsItemExpiredAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetItemAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _storyService.GetStoryByIdAsync(storyId);

        Assert.Null(result);
    }
}
