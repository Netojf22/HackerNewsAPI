using HackerNewsAPI.Application.Services;
using HackerNewsAPI.Domain.Entities;
using HackerNewsAPI.Domain.Interfaces;
using HackerNewsAPI.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsAPI.Tests.Services;

public class StoryServiceTests
{
    private readonly Mock<IHackerNewsRepository> _mockRepository;
    private readonly Mock<ILogger<StoryService>> _mockLogger;
    private readonly StoryService _storyService;

    public StoryServiceTests()
    {
        _mockRepository = new Mock<IHackerNewsRepository>();
        _mockLogger = new Mock<ILogger<StoryService>>();
        _storyService = new StoryService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldReturnStoriesOrderedByScoreDescending()
    {
        // Arrange
        var storyIds = new[] { 1, 2, 3 };
        var stories = new[]
        {
            new Story { Id = 1, Title = "Story 1", Score = 100, CommentCount = 10, PostedBy = "user1", UnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Uri = "http://example.com/1" },
            new Story { Id = 2, Title = "Story 2", Score = 300, CommentCount = 20, PostedBy = "user2", UnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Uri = "http://example.com/2" },
            new Story { Id = 3, Title = "Story 3", Score = 200, CommentCount = 15, PostedBy = "user3", UnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Uri = "http://example.com/3" }
        };

        _mockRepository.Setup(r => r.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _mockRepository.Setup(r => r.GetStoryDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) => stories.First(s => s.Id == id));

        // Act
        var result = await _storyService.GetBestStoriesAsync(3);

        // Assert
        Assert.Equal(3, result.Count());
        var storyArray = result.ToArray();
        Assert.Equal("Story 2", storyArray[0].Title); // Highest score
        Assert.Equal("Story 3", storyArray[1].Title); // Medium score
        Assert.Equal("Story 1", storyArray[2].Title); // Lowest score
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldReturnRequestedNumberOfStories()
    {
        // Arrange
        var storyIds = new[] { 1, 2, 3, 4, 5 };
        var stories = storyIds.Select(id => new Story 
        { 
            Id = id, 
            Title = $"Story {id}", 
            Score = id * 100, 
            CommentCount = id * 10,
            PostedBy = $"user{id}",
            UnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Uri = $"http://example.com/{id}"
        }).ToArray();

        _mockRepository.Setup(r => r.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _mockRepository.Setup(r => r.GetStoryDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) => stories.First(s => s.Id == id));

        // Act
        var result = await _storyService.GetBestStoriesAsync(3);

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldFilterOutNullStories()
    {
        // Arrange
        var storyIds = new[] { 1, 2, 3 };
        var stories = new[]
        {
            new Story { Id = 1, Title = "Story 1", Score = 100, CommentCount = 10, PostedBy = "user1", UnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Uri = "http://example.com/1" },
            (Story?)null,
            new Story { Id = 3, Title = "Story 3", Score = 200, CommentCount = 15, PostedBy = "user3", UnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Uri = "http://example.com/3" }
        };

        _mockRepository.Setup(r => r.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _mockRepository.Setup(r => r.GetStoryDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) => stories[id - 1]);

        // Act
        var result = await _storyService.GetBestStoriesAsync(3);

        // Assert
        Assert.Equal(2, result.Count());
        var storyArray = result.ToArray();
        Assert.Equal("Story 3", storyArray[0].Title); // Higher score comes first
        Assert.Equal("Story 1", storyArray[1].Title);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldMapStoryToStoryDtoCorrectly()
    {
        // Arrange
        var storyIds = new[] { 1 };
        var story = new Story 
        { 
            Id = 1, 
            Title = "Test Story", 
            Score = 150, 
            CommentCount = 25,
            PostedBy = "testuser",
            UnixTime = new DateTimeOffset(2023, 10, 15, 14, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
            Uri = "https://example.com/test"
        };

        _mockRepository.Setup(r => r.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _mockRepository.Setup(r => r.GetStoryDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);

        // Act
        var result = await _storyService.GetBestStoriesAsync(1);

        // Assert
        var storyDto = result.First();
        Assert.Equal(story.Title, storyDto.Title);
        Assert.Equal(story.Uri, storyDto.Uri);
        Assert.Equal(story.PostedBy, storyDto.PostedBy);
        Assert.Equal(story.Time, storyDto.Time);
        Assert.Equal(story.Score, storyDto.Score);
        Assert.Equal(story.CommentCount, storyDto.CommentCount);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldThrowExceptionWhenRepositoryFails()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Repository error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _storyService.GetBestStoriesAsync(5));
    }
}
