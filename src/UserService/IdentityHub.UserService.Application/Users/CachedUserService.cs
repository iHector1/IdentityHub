using System.Text.Json;
using IdentityHub.UserService.Application.Abstractions;
using IdentityHub.UserService.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace IdentityHub.UserService.Application.Users;

public sealed record CachedUser(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed class CachedUserService(
    IUserRepository userRepository,
    IDistributedCache cache,
    ILogger<CachedUserService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public async Task<CachedUser?> GetByIdAsync(Guid id)
    {
        var key = CreateKey(id);

        try
        {
            var cachedJson = await cache.GetStringAsync(key);
            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                var cachedUser = JsonSerializer.Deserialize<CachedUser>(cachedJson, JsonOptions);
                if (cachedUser is not null)
                {
                    logger.LogInformation("Cache hit for user {UserId}", id);
                    return cachedUser;
                }

                logger.LogWarning("Redis cache entry for user {UserId} could not be deserialized", id);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis cache read failed for user {UserId}", id);
        }

        logger.LogInformation("Cache miss for user {UserId}", id);
        var user = await userRepository.GetByIdAsync(id);
        if (user is null)
        {
            return null;
        }

        var result = FromUser(user);
        try
        {
            var json = JsonSerializer.Serialize(result, JsonOptions);
            await cache.SetStringAsync(key, json, CacheOptions);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis cache write failed for user {UserId}", id);
        }

        return result;
    }

    public async Task InvalidateAsync(Guid id)
    {
        try
        {
            await cache.RemoveAsync(CreateKey(id));
            logger.LogInformation("Cache invalidated for user {UserId}", id);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis cache invalidation failed for user {UserId}", id);
        }
    }

    private static string CreateKey(Guid id) => $"user:{id}";

    private static CachedUser FromUser(User user) => new(
        user.Id,
        user.FirstName,
        user.LastName,
        user.Email,
        user.IsActive,
        user.CreatedAt,
        user.UpdatedAt);
}
