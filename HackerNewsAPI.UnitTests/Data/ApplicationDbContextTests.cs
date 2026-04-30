using HackerNewsAPI.Infrastructure.Data;
using HackerNewsAPI.Infrastructure.Entities;
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
        Assert.NotNull(context.Stories);
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
    public async Task ApplicationDbContext_SeedData_PopulatesStories()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        var stories = await context.Stories.ToListAsync();

        // Assert
        Assert.NotNull(stories);
        Assert.Equal(3, stories.Count);
        Assert.All(stories, s => Assert.NotNull(s.Title));
        Assert.All(stories, s => Assert.True(s.Score >= 0));
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
    public async Task ApplicationDbContext_CanAddStory()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        
        var newStory = new StoryEntity
        {
            Title = "New Story",
            Uri = "https://example.com/new",
            PostedBy = "testuser",
            Time = DateTime.UtcNow,
            Score = 100,
            CommentCount = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Stories.Add(newStory);
        await context.SaveChangesAsync();

        // Assert
        var addedStory = await context.Stories.FirstOrDefaultAsync(s => s.Title == "New Story");
        Assert.NotNull(addedStory);
        Assert.Equal("https://example.com/new", addedStory.Uri);
    }
}
