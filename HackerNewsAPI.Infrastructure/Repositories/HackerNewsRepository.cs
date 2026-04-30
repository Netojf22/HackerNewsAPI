using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HackerNewsAPI.Infrastructure.Interfaces;
using HackerNewsAPI.Infrastructure.Data;
using HackerNewsAPI.Infrastructure.Entities;

namespace HackerNewsAPI.Infrastructure.Repositories;

public class HackerNewsRepository : IHackerNewsRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HackerNewsRepository> _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public HackerNewsRepository(ApplicationDbContext context, ILogger<HackerNewsRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Item?> GetItemByIdAsync(int itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving item {ItemId} from database", itemId);
            return await _context.Items.FindAsync(new object[] { itemId }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item {ItemId} from database", itemId);
            throw;
        }
    }

    public async Task AddOrUpdateItemAsync(Item item, CancellationToken cancellationToken = default)
    {
        try
        {
            item.CachedAt = DateTime.UtcNow;
            
            var existingItem = await _context.Items.FindAsync(new object[] { item.Id }, cancellationToken);
            
            if (existingItem != null)
            {
                _logger.LogDebug("Updating item {ItemId} in database", item.Id);
                _context.Entry(existingItem).CurrentValues.SetValues(item);
            }
            else
            {
                _logger.LogDebug("Adding new item {ItemId} to database", item.Id);
                _context.Items.Add(item);
            }
            
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding or updating item {ItemId} in database", item.Id);
            throw;
        }
    }

    public async Task AddOrUpdateItemsAsync(IEnumerable<Item> items, CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var item in items)
            {
                item.CachedAt = DateTime.UtcNow;
                
                var existingItem = await _context.Items.FindAsync(new object[] { item.Id }, cancellationToken);
                
                if (existingItem != null)
                {
                    _logger.LogDebug("Updating item {ItemId} in database", item.Id);
                    _context.Entry(existingItem).CurrentValues.SetValues(item);
                }
                else
                {
                    _logger.LogDebug("Adding new item {ItemId} to database", item.Id);
                    _context.Items.Add(item);
                }
            }
            
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding or updating items in database");
            throw;
        }
    }

    public async Task<bool> IsItemExpiredAsync(int itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _context.Items.FindAsync(new object[] { itemId }, cancellationToken);
            
            if (item == null)
            {
                return true;
            }
            
            var isExpired = DateTime.UtcNow - item.CachedAt > CacheExpiration;
            
            if (isExpired)
            {
                _logger.LogDebug("Item {ItemId} is expired (cached at {CachedAt})", itemId, item.CachedAt);
            }
            
            return isExpired;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if item {ItemId} is expired", itemId);
            throw;
        }
    }

    public async Task<Dictionary<int, bool>> CheckItemsExpiredAsync(IEnumerable<int> itemIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await _context.Items
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i, cancellationToken);
            
            return itemIds.ToDictionary(
                id => id, 
                id => !items.ContainsKey(id) || DateTime.UtcNow - items[id].CachedAt > CacheExpiration
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking batch expiration for items");
            throw;
        }
    }
}
