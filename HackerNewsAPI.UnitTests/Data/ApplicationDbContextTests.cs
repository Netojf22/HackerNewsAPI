using HackerNewsAPI.Infrastructure.Data;
using HackerNewsAPI.Infrastructure.Entities;
using HackerNewsAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HackerNewsAPI.Tests.Data;

public class ApplicationDbContextTests
{
    [Fact]
    public void ApplicationDbContext_CanBeCreated()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new ApplicationDbContext(options);

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Users);
        Assert.NotNull(context.Items);
    }

    [Fact]
    public async Task ApplicationDbContext_DatabaseEnsureCreated_CreatesDatabase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new ApplicationDbContext(options);
        var created = context.Database.EnsureCreated();

        // Assert
        Assert.True(created);
    }

    [Fact]
    public async Task ApplicationDbContext_SeedData_PopulatesUsers()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        var users = await context.Users.ToListAsync();

        // Assert
        Assert.NotNull(users);
        Assert.Equal(2, users.Count);
        Assert.Contains(users, u => u.Username == "admin");
        Assert.Contains(users, u => u.Username == "user");
    }

    [Fact]
    public async Task ApplicationDbContext_CanAddUser()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        
        var newUser = new UserEntity
        {
            Username = "newuser",
            Email = "newuser@example.com",
            PasswordHash = "hashedpassword",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(newUser);
        await context.SaveChangesAsync();

        // Assert
        var addedUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
        Assert.NotNull(addedUser);
        Assert.Equal("newuser@example.com", addedUser.Email);
    }

    [Fact]
    public async Task ApplicationDbContext_CanAddItem()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        
        var newItem = new Item
        {
            Id = 123,
            By = "testuser",
            Title = "New Story",
            Score = 100,
            Descendants = 10,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/new",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow
        };

        context.Items.Add(newItem);
        await context.SaveChangesAsync();

        // Assert
        var addedItem = await context.Items.FirstOrDefaultAsync(i => i.Title == "New Story");
        Assert.NotNull(addedItem);
        Assert.Equal("https://example.com/new", addedItem.Url);
    }
}
