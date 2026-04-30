using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Domain.ValueObjects;
using HackerNewsAPI.Domain.Entities;
using Moq;
using System.Net;
using System.Text.Json;

namespace HackerNewsAPI.Tests.Controllers;

public class StoriesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IStoryService> _mockStoryService;

    public StoriesControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockStoryService = new Mock<IStoryService>();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(_mockStoryService.Object);
                // Configure test authentication
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        });
    }

    [Fact]
    public async Task GetBestStories_WithDefaultCount_ReturnsOkResult()
    {
        // Arrange
        var expectedStories = new List<StoryDto>
        {
            new() { Title = "Story 1", Uri = "https://example.com/1", PostedBy = "user1", Time = DateTime.UtcNow, Score = 100, CommentCount = 10 },
            new() { Title = "Story 2", Uri = "https://example.com/2", PostedBy = "user2", Time = DateTime.UtcNow, Score = 200, CommentCount = 20 }
        };

        _mockStoryService.Setup(s => s.GetBestStoriesAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStories);

        var client = _factory.CreateClient();
        // Set up authenticated request
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await client.GetAsync("/api/stories/best");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var stories = JsonSerializer.Deserialize<List<StoryDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(stories);
        Assert.Equal(2, stories.Count);
        Assert.Equal("Story 1", stories[0].Title);
        Assert.Equal("Story 2", stories[1].Title);
    }

    [Fact]
    public async Task GetBestStories_WithCustomCount_ReturnsOkResult()
    {
        // Arrange
        var expectedStories = new List<StoryDto>
        {
            new() { Title = "Story 1", Uri = "https://example.com/1", PostedBy = "user1", Time = DateTime.UtcNow, Score = 100, CommentCount = 10 }
        };

        _mockStoryService.Setup(s => s.GetBestStoriesAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStories);

        var client = _factory.CreateClient();
        // Set up authenticated request
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await client.GetAsync("/api/stories/best?count=5");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _mockStoryService.Verify(s => s.GetBestStoriesAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBestStories_WithZeroCount_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        // Set up authenticated request
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await client.GetAsync("/api/stories/best?count=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBestStories_WithNegativeCount_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        // Set up authenticated request
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await client.GetAsync("/api/stories/best?count=-5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBestStories_WithCountExceedingLimit_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        // Set up authenticated request
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await client.GetAsync("/api/stories/best?count=101");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBestStories_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockStoryService.Setup(s => s.GetBestStoriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        var client = _factory.CreateClient();
        // Set up authenticated request
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await client.GetAsync("/api/stories/best");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetBestStories_WithMaximumAllowedCount_ReturnsOkResult()
    {
        // Arrange
        var expectedStories = new List<StoryDto>
        {
            new() { Title = "Story 1", Uri = "https://example.com/1", PostedBy = "user1", Time = DateTime.UtcNow, Score = 100, CommentCount = 10 }
        };

        _mockStoryService.Setup(s => s.GetBestStoriesAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStories);

        var client = _factory.CreateClient();
        // Set up authenticated request
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await client.GetAsync("/api/stories/best?count=100");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _mockStoryService.Verify(s => s.GetBestStoriesAsync(100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBestStories_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthorizedFactory = new WebApplicationFactory<Program>();
        var client = unauthorizedFactory.CreateClient();
        // No authorization header set

        // Act
        var response = await client.GetAsync("/api/stories/best");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStoryById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var storyId = 123;
        var expectedStory = new Story
        {
            Title = "Test Story",
            Uri = "https://example.com/test",
            PostedBy = "testuser",
            Time = DateTime.UtcNow,
            Score = 100,
            CommentCount = 25
        };

        _mockStoryService.Setup(s => s.GetStoryByIdAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStory);

        var client = _factory.CreateClient();
        // Set up authenticated request
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await client.GetAsync($"/api/stories/{storyId}");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var story = JsonSerializer.Deserialize<Story>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(story);
        Assert.Equal("Test Story", story.Title);
    }

    [Fact]
    public async Task GetStoryById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var storyId = 999;
        _mockStoryService.Setup(s => s.GetStoryByIdAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Story?)null);

        var client = _factory.CreateClient();
        // Set up authenticated request
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await client.GetAsync($"/api/stories/{storyId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStoryById_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthorizedFactory = new WebApplicationFactory<Program>();
        var client = unauthorizedFactory.CreateClient();
        // No authorization header set

        // Act
        var response = await client.GetAsync("/api/stories/123");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
