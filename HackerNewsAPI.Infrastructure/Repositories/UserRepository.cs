using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HackerNewsAPI.Domain.Entities;
using HackerNewsAPI.Domain.Interfaces;
using HackerNewsAPI.Infrastructure.Data;
using HackerNewsAPI.Infrastructure.Entities;
using BCrypt.Net;

namespace HackerNewsAPI.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting user by username: {Username}", username);

            var userEntity = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

            if (userEntity == null)
            {
                _logger.LogDebug("User not found: {Username}", username);
                return null;
            }

            _logger.LogDebug("User found: {Username}", username);

            return MapToDomainUser(userEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by username: {Username}", username);
            throw;
        }
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting user by ID: {UserId}", id);

            var userEntity = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (userEntity == null)
            {
                _logger.LogDebug("User not found: {UserId}", id);
                return null;
            }

            _logger.LogDebug("User found: {UserId}", id);

            return MapToDomainUser(userEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            throw;
        }
    }

    private static User MapToDomainUser(UserEntity userEntity)
    {
        return new User
        {
            Id = userEntity.Id,
            Username = userEntity.Username,
            Email = userEntity.Email,
            PasswordHash = userEntity.PasswordHash,
            Role = userEntity.Role,
            CreatedAt = userEntity.CreatedAt,
            UpdatedAt = userEntity.UpdatedAt
        };
    }
}
