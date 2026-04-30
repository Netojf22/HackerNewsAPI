using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Application.Services;
using HackerNewsAPI.Domain.Entities;
using HackerNewsAPI.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsAPI.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        _authService = new AuthService(_mockUserRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnUserWhenCredentialsAreValid()
    {
        // Arrange
        var username = "testuser";
        var password = "Test123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User
        {
            Id = 99,
            Username = username,
            Email = "test@example.com",
            PasswordHash = passwordHash,
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByUsernameAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.AuthenticateAsync(username, password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(username, result.Username);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("User", result.Role);
        Assert.Equal(99, result.Id);
        // Password hash should not be returned for security
        Assert.Equal(string.Empty, result.PasswordHash);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNullWhenUserNotFound()
    {
        // Arrange
        var username = "nonexistent";
        var password = "Test123!";

        _mockUserRepository.Setup(r => r.GetByUsernameAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.AuthenticateAsync(username, password);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNullWhenPasswordIsInvalid()
    {
        // Arrange
        var username = "testuser";
        var password = "WrongPassword";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword");
        var user = new User
        {
            Id = 98,
            Username = username,
            Email = "test@example.com",
            PasswordHash = passwordHash,
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByUsernameAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.AuthenticateAsync(username, password);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUserWhenExists()
    {
        // Arrange
        var userId = 97;
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("test@example.com", result.Email);
        // Password hash should not be returned for security
        Assert.Equal(string.Empty, result.PasswordHash);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnNullWhenNotExists()
    {
        // Arrange
        var userId = 999;

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.GetUserByIdAsync(userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_ShouldReturnUserWhenExists()
    {
        // Arrange
        var username = "testuser";
        var user = new User
        {
            Id = 96,
            Username = username,
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByUsernameAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.GetUserByUsernameAsync(username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(username, result.Username);
        Assert.Equal("test@example.com", result.Email);
        // Password hash should not be returned for security
        Assert.Equal(string.Empty, result.PasswordHash);
    }
}
