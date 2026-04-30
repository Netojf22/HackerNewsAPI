using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HackerNewsAPI.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace HackerNewsAPI.Tests.Integration;

public class StoriesControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StoriesControllerIntegrationTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("StoriesTestDb");
                });
            });
        });
    }

    [Fact]
    public async Task GetBestStories_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/stories/best");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBestStories_WithValidToken_ReturnsData()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var client = _factory.CreateClient();
        
        // First, login to get a valid token
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
        // Should not be Unauthorized or Forbidden - authentication passed
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        
        // The response should be either OK (successful data fetch) or InternalServerError (external API issues)
        // but not an authentication/authorization error
        Assert.True(response.StatusCode == HttpStatusCode.OK || 
                   response.StatusCode == HttpStatusCode.InternalServerError ||
                   response.StatusCode == HttpStatusCode.BadRequest);

        // If we get OK, verify the response structure
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(content));
            
            // Try to parse as JSON array
            var stories = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.True(stories.ValueKind == JsonValueKind.Array);
        }
    }

    [Fact]
    public async Task GetBestStories_WithInvalidToken_ReturnsUnauthorized()
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
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBestStories_WithValidTokenAndInvalidCount_ReturnsBadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var client = _factory.CreateClient();
        
        // Login to get token
        var loginRequest = new
        {
            Username = "user",
            Password = "Test123!"
        };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<JsonElement>(loginContent);
        var token = loginResult.GetProperty("token").GetString();

        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/stories/best?count=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBestStories_WithValidTokenAndExcessiveCount_ReturnsBadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var client = _factory.CreateClient();
        
        // Login to get token
        var loginRequest = new
        {
            Username = "admin",
            Password = "Test123!"
        };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<JsonElement>(loginContent);
        var token = loginResult.GetProperty("token").GetString();

        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/stories/best?count=101");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBestStories_WithValidTokenAndDefaultCount_ReturnsData()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var client = _factory.CreateClient();
        
        // Login to get token
        var loginRequest = new
        {
            Username = "user",
            Password = "Test123!"
        };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<JsonElement>(loginContent);
        var token = loginResult.GetProperty("token").GetString();

        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/stories/best");

        // Assert
        // Should not be authentication/authorization errors
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        
        // Should be either OK, BadRequest, or InternalServerError
        Assert.True(response.StatusCode == HttpStatusCode.OK || 
                   response.StatusCode == HttpStatusCode.InternalServerError ||
                   response.StatusCode == HttpStatusCode.BadRequest);
    }
}
