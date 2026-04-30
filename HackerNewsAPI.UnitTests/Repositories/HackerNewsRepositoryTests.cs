using HackerNewsAPI.Infrastructure.Repositories;
using HackerNewsAPI.Infrastructure.Data;
using HackerNewsAPI.Infrastructure.Entities;
using HackerNewsAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsAPI.Tests.Repositories;

public class HackerNewsRepositoryTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<HackerNewsRepository>> _mockLogger;
    private readonly HackerNewsRepository _repository;

    public HackerNewsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<HackerNewsRepository>>();
        _repository = new HackerNewsRepository(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task GetItemByIdAsync_ShouldReturnItemWhenExists()
    {
        // Arrange
        var item = new Item
        {
            Id = 1,
            By = "testuser",
            Title = "Test Story",
            Score = 100,
            Descendants = 25,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/test",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow
        };
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetItemByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Story", result.Title);
    }

    [Fact]
    public async Task GetItemByIdAsync_ShouldReturnNullWhenNotExists()
    {
        // Act
        var result = await _repository.GetItemByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddOrUpdateItemAsync_ShouldAddNewItem()
    {
        // Arrange
        var item = new Item
        {
            Id = 1,
            By = "testuser",
            Title = "Test Story",
            Score = 100,
            Descendants = 25,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/test",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddOrUpdateItemAsync(item);

        // Assert
        var result = await _context.Items.FindAsync(1);
        Assert.NotNull(result);
        Assert.Equal("Test Story", result.Title);
    }

    [Fact]
    public async Task AddOrUpdateItemAsync_ShouldUpdateExistingItem()
    {
        // Arrange
        var item = new Item
        {
            Id = 1,
            By = "testuser",
            Title = "Test Story",
            Score = 100,
            Descendants = 25,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/test",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        var updatedItem = new Item
        {
            Id = 1,
            By = "testuser",
            Title = "Updated Story",
            Score = 150,
            Descendants = 30,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/updated",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddOrUpdateItemAsync(updatedItem);

        // Assert
        var result = await _context.Items.FindAsync(1);
        Assert.NotNull(result);
        Assert.Equal("Updated Story", result.Title);
        Assert.Equal(150, result.Score);
    }

    [Fact]
    public async Task IsItemExpiredAsync_ShouldReturnTrueWhenItemNotExists()
    {
        // Act
        var result = await _repository.IsItemExpiredAsync(999);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsItemExpiredAsync_ShouldReturnTrueWhenItemExpired()
    {
        // Arrange
        var item = new Item
        {
            Id = 1,
            By = "testuser",
            Title = "Test Story",
            Score = 100,
            Descendants = 25,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/test",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsItemExpiredAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsItemExpiredAsync_ShouldReturnFalseWhenItemNotExpired()
    {
        // Arrange
        var item = new Item
        {
            Id = 1,
            By = "testuser",
            Title = "Test Story",
            Score = 100,
            Descendants = 25,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/test",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow.AddMinutes(-2)
        };
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsItemExpiredAsync(1);

        // Assert
        Assert.False(result);
    }
}
