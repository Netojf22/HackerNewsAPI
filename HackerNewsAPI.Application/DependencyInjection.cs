using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Application.Services;
using HackerNewsAPI.Domain.Interfaces;
using HackerNewsAPI.Infrastructure.Repositories;
using HackerNewsAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HackerNewsAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Register application services
        services.AddScoped<IStoryService, StoryService>();
        services.AddScoped<IAuthService, AuthService>();

        // Register Infrastructure services
        services.AddMemoryCache();
        
        // Register HttpClient for Hacker News API
        services.AddHttpClient<IHackerNewsRepository, HackerNewsRepository>(client =>
        {
            client.BaseAddress = new Uri("https://hacker-news.firebaseio.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register DbContext with InMemory database
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase("HackerNewsDb");
        });

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();

        // Create a scope to seed the database
        var serviceProvider = services.BuildServiceProvider();
        using (var scope = serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
        }

        return services;
    }
}
