using HackerNewsAPI.Infrastructure.Entities;

namespace HackerNewsAPI.Application.Interfaces;

public interface IHackerNewsApiService
{
    Task<Item?> GetItemAsync(int itemId, CancellationToken cancellationToken = default);
    Task<IEnumerable<int>> GetTopStoryIdsAsync(CancellationToken cancellationToken = default);
}
