using HackerNewsAPI.Domain.ValueObjects;

namespace HackerNewsAPI.Application.Interfaces;

public interface IStoryService
{
    Task<IEnumerable<StoryDto>> GetBestStoriesAsync(int count, CancellationToken cancellationToken = default);
}
