using Microsoft.Extensions.Logging;
using HackerNewsAPI.Domain.Entities;
using HackerNewsAPI.Domain.Interfaces;
using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Domain.ValueObjects;

namespace HackerNewsAPI.Application.Services;

public class StoryService : IStoryService
{
    private readonly IHackerNewsRepository _hackerNewsRepository;
    private readonly ILogger<StoryService> _logger;

    public StoryService(IHackerNewsRepository hackerNewsRepository, ILogger<StoryService> logger)
    {
        _hackerNewsRepository = hackerNewsRepository ?? throw new ArgumentNullException(nameof(hackerNewsRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<StoryDto>> GetBestStoriesAsync(int count, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting top {Count} best stories", count);

        try
        {
            var storyIds = await _hackerNewsRepository.GetBestStoryIdsAsync(cancellationToken);
            
            var storyTasks = storyIds.Take(count).Select(id => _hackerNewsRepository.GetStoryDetailsAsync(id, cancellationToken));
            var stories = await Task.WhenAll(storyTasks);
            
            var validStories = stories.Where(s => s != null).OrderByDescending(s => s!.Score);
            
            var storyDtos = validStories.Select(story => new StoryDto
            {
                Title = story!.Title,
                Uri = story.Uri,
                PostedBy = story.PostedBy,
                Time = story.Time,
                Score = story.Score,
                CommentCount = story.CommentCount
            });

            _logger.LogInformation("Successfully retrieved {Count} stories", storyDtos.Count());
            return storyDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting best stories");
            throw;
        }
    }
}
