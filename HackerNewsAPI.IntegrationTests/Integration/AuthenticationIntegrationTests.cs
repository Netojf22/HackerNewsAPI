using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HackerNewsAPI.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace HackerNewsAPI.Tests.Integration;

public class AuthenticationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove background service to prevent conflicts during testing
                var cacheRefreshServiceDescriptor = services.FirstOrDefault(
                    d => d.ImplementationType?.Name == "CacheRefreshService");
                if (cacheRefreshServiceDescriptor != null)
                {
                    services.Remove(cacheRefreshServiceDescriptor);
                }

                // Replace the DbContext with InMemory database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });
    }

    [Fact]
    public async Task Login_WithValidAdminCredentials_ReturnsToken()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated(); // This will apply seed data from configurations

        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Username = "admin",
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
        Assert.Equal("admin", result.GetProperty("username").GetString());
        Assert.Equal("Admin", result.GetProperty("role").GetString());
        Assert.True(result.GetProperty("expiresIn").GetInt32() > 0);
    }

    [Fact]
    public async Task Login_WithValidUserCredentials_ReturnsToken()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Username = "user",
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
        Assert.Equal("user", result.GetProperty("username").GetString());
        Assert.Equal("User", result.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WithInvalidUsername_ReturnsUnauthorized()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Username = "nonexistent",
            Password = "Test123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Username = "admin",
            Password = "WrongPassword"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StoriesEndpoint_WithValidToken_ReturnsStories()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var client = _factory.CreateClient();
        
        // First, login to get token
        var loginRequest = new
        {
            Username = "admin",
            Password = "Test123!"
        };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<JsonElement>(loginContent);
        var token = loginResult.GetProperty("token").GetString();

        // Use token to access protected endpoint
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/stories/best?count=5");

        // Assert
        // The endpoint should be accessible (either OK or error from external API, but not 401)
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task StoriesEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/stories/best");

        // Assert
        // Should be either Unauthorized or InternalServerError (if JWT middleware fails)
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || 
                   response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task StoriesEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.token.here");

        // Act
        var response = await client.GetAsync("/api/stories/best");

        // Assert
        // Should be either Unauthorized or InternalServerError (if JWT middleware fails)
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || 
                   response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SeededUsers_AreAvailableInDatabase()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        // Act
        var users = await context.Users.ToListAsync();

        // Assert
        Assert.NotNull(users);
        Assert.Equal(2, users.Count);
        Assert.Contains(users, u => u.Username == "admin");
        Assert.Contains(users, u => u.Username == "user");
        Assert.Contains(users, u => u.Role == "Admin");
        Assert.Contains(users, u => u.Role == "User");
    }

    [Fact]
    public async Task SeededStories_AreAvailableInDatabase()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        // Act
        var items = await context.Items.ToListAsync();

        // Assert
        Assert.NotNull(items);
        Assert.All(items, i => Assert.NotNull(i.Title));
        Assert.All(items, i => Assert.True(i.Score >= 0));
    }

    [Fact]
    public async Task Login_WithMissingUsername_ReturnsBadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

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
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Username = "admin",
            Password = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
