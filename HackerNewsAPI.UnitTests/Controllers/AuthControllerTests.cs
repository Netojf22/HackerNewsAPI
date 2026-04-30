using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Domain.Entities;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace HackerNewsAPI.Tests.Controllers;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IAuthService> _mockAuthService;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockAuthService = new Mock<IAuthService>();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(_mockAuthService.Object);
            });
        });
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkResultWithToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockAuthService.Setup(s => s.AuthenticateAsync("testuser", "Test123!"))
            .ReturnsAsync(user);

        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Username = "testuser",
            Password = "Test123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.TryGetProperty("token", out var token));
        Assert.NotNull(token.GetString());
        Assert.Equal("testuser", result.GetProperty("username").GetString());
        Assert.Equal("User", result.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        _mockAuthService.Setup(s => s.AuthenticateAsync("testuser", "WrongPassword"))
            .ReturnsAsync((User?)null);

        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Username = "testuser",
            Password = "WrongPassword"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingUsername_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Username = "",
            Password = "Test123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Username = "testuser",
            Password = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockAuthService.Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Username = "testuser",
            Password = "Test123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
