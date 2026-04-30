using HackerNewsAPI.Domain.Entities;
using HackerNewsAPI.Infrastructure.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace HackerNewsAPI.Tests.Repositories;

public class HackerNewsRepositoryTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<HackerNewsRepository>> _mockLogger;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly HackerNewsRepository _repository;

    public HackerNewsRepositoryTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockLogger = new Mock<ILogger<HackerNewsRepository>>();
        _mockCache = new Mock<IMemoryCache>();
        var mockCacheEntry = new Mock<ICacheEntry>();
        _mockCache
            .Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(mockCacheEntry.Object);
        _repository = new HackerNewsRepository(_httpClient, _mockLogger.Object, _mockCache.Object);
    }

    [Fact]
    public async Task GetBestStoryIdsAsync_ShouldReturnStoryIdsFromApi()
    {
        // Arrange
        var expectedIds = new[] { 1, 2, 3, 4, 5 };
        var jsonResponse = JsonSerializer.Serialize(expectedIds);
        
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        // Act
        var result = await _repository.GetBestStoryIdsAsync();

        // Assert
        Assert.Equal(expectedIds, result);
    }

    [Fact]
    public async Task GetBestStoryIdsAsync_ShouldReturnCachedIdsWhenAvailable()
    {
        // Arrange
        var cachedIds = new[] { 1, 2, 3 };
        
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns((object key, out object value) =>
            {
                value = cachedIds;
                return true;
            });

        // Act
        var result = await _repository.GetBestStoryIdsAsync();

        // Assert
        Assert.Equal(cachedIds, result);
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetStoryDetailsAsync_ShouldReturnStoryFromApi()
    {
        // Arrange
        var storyId = 123;
        var expectedStory = new Story
        {
            Id = storyId,
            Title = "Test Story",
            Score = 100,
            CommentCount = 25,
            PostedBy = "testuser",
            UnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Uri = "https://example.com/test"
        };

        var jsonResponse = JsonSerializer.Serialize(expectedStory);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        // Act
        var result = await _repository.GetStoryDetailsAsync(storyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedStory.Id, result.Id);
        Assert.Equal(expectedStory.Title, result.Title);
        Assert.Equal(expectedStory.Score, result.Score);
        Assert.Equal(expectedStory.CommentCount, result.CommentCount);
        Assert.Equal(expectedStory.PostedBy, result.PostedBy);
        Assert.Equal(expectedStory.Uri, result.Uri);
    }

    [Fact]
    public async Task GetStoryDetailsAsync_ShouldReturnCachedStoryWhenAvailable()
    {
        // Arrange
        var storyId = 123;
        var cachedStory = new Story
        {
            Id = storyId,
            Title = "Cached Story",
            Score = 150,
            CommentCount = 30,
            PostedBy = "cacheduser",
            UnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Uri = "https://example.com/cached"
        };

        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns((object key, out object value) =>
            {
                value = cachedStory;
                return true;
            });

        // Act
        var result = await _repository.GetStoryDetailsAsync(storyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cachedStory.Id, result.Id);
        Assert.Equal(cachedStory.Title, result.Title);
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetStoryDetailsAsync_ShouldReturnNullWhenStoryNotFound()
    {
        // Arrange
        var storyId = 999;
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        // Act
        var result = await _repository.GetStoryDetailsAsync(storyId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStoryDetailsAsync_ShouldThrowExceptionWhenApiCallFails()
    {
        // Arrange
        var storyId = 123;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _repository.GetStoryDetailsAsync(storyId));
    }
}
