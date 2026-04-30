using HackerNewsAPI.Domain.ValueObjects;
using HackerNewsAPI.Domain.Entities;

namespace HackerNewsAPI.Application.Interfaces;

public interface IStoryService
{
    Task<Story?> GetStoryByIdAsync(int storyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StoryDto>> GetBestStoriesAsync(int count, CancellationToken cancellationToken = default);
}
