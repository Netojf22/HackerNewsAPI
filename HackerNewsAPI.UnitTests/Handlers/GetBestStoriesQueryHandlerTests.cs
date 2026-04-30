using HackerNewsAPI.Application.Handlers;
using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Application.Queries;
using HackerNewsAPI.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsAPI.Tests.Handlers;

public class GetBestStoriesQueryHandlerTests
{
    private readonly Mock<IStoryService> _mockStoryService;
    private readonly Mock<ILogger<GetBestStoriesQueryHandler>> _mockLogger;
    private readonly GetBestStoriesQueryHandler _handler;

    public GetBestStoriesQueryHandlerTests()
    {
        _mockStoryService = new Mock<IStoryService>();
        _mockLogger = new Mock<ILogger<GetBestStoriesQueryHandler>>();
        _handler = new GetBestStoriesQueryHandler(_mockStoryService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnStoriesFromService()
    {
        // Arrange
        var query = new GetBestStoriesQuery(5);
        var expectedStories = new[]
        {
            new StoryDto { Title = "Story 1", Score = 100, CommentCount = 10, PostedBy = "user1", Time = DateTime.UtcNow, Uri = "http://example.com/1" },
            new StoryDto { Title = "Story 2", Score = 200, CommentCount = 20, PostedBy = "user2", Time = DateTime.UtcNow, Uri = "http://example.com/2" }
        };

        _mockStoryService.Setup(s => s.GetBestStoriesAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStories);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockStoryService.Verify(s => s.GetBestStoriesAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPassCorrectCountToService()
    {
        // Arrange
        var query = new GetBestStoriesQuery(10);
        var expectedStories = Array.Empty<StoryDto>();

        _mockStoryService.Setup(s => s.GetBestStoriesAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStories);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockStoryService.Verify(s => s.GetBestStoriesAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPropagateServiceExceptions()
    {
        // Arrange
        var query = new GetBestStoriesQuery(5);
        _mockStoryService.Setup(s => s.GetBestStoriesAsync(5, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(query, CancellationToken.None));
    }
}
