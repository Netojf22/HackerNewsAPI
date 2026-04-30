using HackerNewsAPI.Domain.Entities;

namespace HackerNewsAPI.Domain.Interfaces;

public interface IHackerNewsRepository
{
    Task<IEnumerable<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken = default);
    Task<Story?> GetStoryDetailsAsync(int storyId, CancellationToken cancellationToken = default);
}
