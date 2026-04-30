using HackerNewsAPI.Infrastructure.Entities;

namespace HackerNewsAPI.Infrastructure.Interfaces;

public interface IHackerNewsRepository
{
    Task<Item?> GetItemByIdAsync(int itemId, CancellationToken cancellationToken = default);
    Task AddOrUpdateItemAsync(Item item, CancellationToken cancellationToken = default);
    Task<bool> IsItemExpiredAsync(int itemId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, bool>> CheckItemsExpiredAsync(IEnumerable<int> itemIds, CancellationToken cancellationToken = default);
}
