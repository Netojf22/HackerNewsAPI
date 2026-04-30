using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Domain.ValueObjects;

namespace HackerNewsAPI.Application.Services;

public class CacheRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheRefreshService> _logger;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(4);

    public CacheRefreshService(
        IServiceProvider serviceProvider,
        ILogger<CacheRefreshService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cache refresh service starting");

        // Initial cache warm-up
        await WarmCacheAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_refreshInterval, stoppingToken);
                await RefreshCacheAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Cache refresh service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache refresh");
                // Continue running despite errors
            }
        }
    }

    private async Task WarmCacheAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Warming up cache with top stories");
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var storyService = scope.ServiceProvider.GetRequiredService<IStoryService>();
            await storyService.GetBestStoriesAsync(50, cancellationToken);
            _logger.LogInformation("Cache warm-up completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warm-up");
        }
    }

    private async Task RefreshCacheAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Refreshing cache");
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var storyService = scope.ServiceProvider.GetRequiredService<IStoryService>();
            // Refresh top 20 stories to keep cache fresh
            await storyService.GetBestStoriesAsync(20, cancellationToken);
            _logger.LogDebug("Cache refresh completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache refresh");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cache refresh service stopping");
        await base.StopAsync(cancellationToken);
    }
}
