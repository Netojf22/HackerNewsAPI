using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using System.Security.Claims;
using HackerNewsAPI.Application.Queries;
using HackerNewsAPI.Domain.ValueObjects;
using Moq;
using System.Net;
using System.Text.Json;

namespace HackerNewsAPI.Tests.Controllers;

public class StoriesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IMediator> _mockMediator;

    public StoriesControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockMediator = new Mock<IMediator>();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(_mockMediator.Object);
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

        _mockMediator.Setup(m => m.Send(It.IsAny<GetBestStoriesQuery>(), It.IsAny<CancellationToken>()))
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

        _mockMediator.Setup(m => m.Send(It.IsAny<GetBestStoriesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStories);

        var client = _factory.CreateClient();
        // Set up authenticated request
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await client.GetAsync("/api/stories/best?count=5");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _mockMediator.Verify(m => m.Send(It.Is<GetBestStoriesQuery>(q => q.Count == 5), It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task GetBestStories_WhenMediatorThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockMediator.Setup(m => m.Send(It.IsAny<GetBestStoriesQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Mediator error"));

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

        _mockMediator.Setup(m => m.Send(It.IsAny<GetBestStoriesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStories);

        var client = _factory.CreateClient();
        // Set up authenticated request
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await client.GetAsync("/api/stories/best?count=100");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _mockMediator.Verify(m => m.Send(It.Is<GetBestStoriesQuery>(q => q.Count == 100), It.IsAny<CancellationToken>()), Times.Once);
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
}
