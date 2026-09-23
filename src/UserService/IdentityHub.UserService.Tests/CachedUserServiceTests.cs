using System.Collections.Concurrent;
using System.Text.Json;
using IdentityHub.UserService.Application.Abstractions;
using IdentityHub.UserService.Application.Users;
using IdentityHub.UserService.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentityHub.UserService.Tests;

public sealed class CachedUserServiceTests
{
    [Fact]
    public async Task GetByIdAsync_CacheHit_ShouldNotCallRepository()
    {
        var user = CreateUser();
        var cache = new FakeDistributedCache();
        var cachedUser = new CachedUser(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt);
        await cache.SetStringAsync(
            $"user:{user.Id}",
            JsonSerializer.Serialize(cachedUser, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var repository = new TestUserRepository(user);
        var service = CreateService(repository, cache);

        var result = await service.GetByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(0, repository.GetByIdCalls);
    }

    [Fact]
    public async Task GetByIdAsync_CacheMiss_ShouldCallRepositoryAndSaveResult()
    {
        var user = CreateUser();
        var cache = new FakeDistributedCache();
        var repository = new TestUserRepository(user);
        var service = CreateService(repository, cache);

        var result = await service.GetByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(1, repository.GetByIdCalls);
        Assert.NotNull(await cache.GetStringAsync($"user:{user.Id}"));
    }

    [Fact]
    public async Task InvalidateAsync_ShouldRemoveUserFromCache()
    {
        var user = CreateUser();
        var cache = new FakeDistributedCache();
        await cache.SetStringAsync($"user:{user.Id}", "cached");
        var service = CreateService(new TestUserRepository(user), cache);

        await service.InvalidateAsync(user.Id);

        Assert.Null(await cache.GetStringAsync($"user:{user.Id}"));
    }

    [Fact]
    public async Task GetByIdAsync_WhenRedisFails_ShouldReturnRepositoryResult()
    {
        var user = CreateUser();
        var cache = new FakeDistributedCache
        {
            ThrowOnGet = true,
            ThrowOnSet = true
        };
        var repository = new TestUserRepository(user);
        var service = CreateService(repository, cache);

        var result = await service.GetByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(1, repository.GetByIdCalls);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheEntryIsInvalid_ShouldUseRepository()
    {
        var user = CreateUser();
        var cache = new FakeDistributedCache();
        await cache.SetStringAsync($"user:{user.Id}", "not-json");
        var repository = new TestUserRepository(user);
        var service = CreateService(repository, cache);

        var result = await service.GetByIdAsync(user.Id);

        Assert.Equal(user.Id, result!.Id);
        Assert.Equal(1, repository.GetByIdCalls);
    }

    [Fact]
    public async Task InvalidateAsync_WhenRedisFails_ShouldNotThrow()
    {
        var cache = new FakeDistributedCache { ThrowOnRemove = true };
        var service = CreateService(new TestUserRepository(CreateUser()), cache);

        await service.InvalidateAsync(Guid.NewGuid());
    }

    private static CachedUserService CreateService(
        TestUserRepository repository,
        FakeDistributedCache cache) =>
        new(repository, cache, NullLogger<CachedUserService>.Instance);

    private static User CreateUser() =>
        new("Ana", "García", "ana@example.com");

    private sealed class TestUserRepository(User? user) : IUserRepository
    {
        public int GetByIdCalls { get; private set; }

        public Task<IReadOnlyCollection<User>> GetAllAsync() =>
            Task.FromResult<IReadOnlyCollection<User>>(user is null ? Array.Empty<User>() : new[] { user });

        public Task<User?> GetByIdAsync(Guid id)
        {
            GetByIdCalls++;
            return Task.FromResult(user?.Id == id ? user : null);
        }

        public Task<User?> GetByEmailAsync(string email) => Task.FromResult<User?>(null);
        public Task AddAsync(User newUser) => Task.CompletedTask;
        public Task UpdateAsync(User updatedUser) => Task.CompletedTask;
    }

    private sealed class FakeDistributedCache : IDistributedCache
    {
        private readonly ConcurrentDictionary<string, byte[]> values = new();

        public bool ThrowOnGet { get; init; }
        public bool ThrowOnSet { get; init; }
        public bool ThrowOnRemove { get; init; }

        public byte[]? Get(string key)
        {
            if (ThrowOnGet) throw new InvalidOperationException("Redis unavailable.");
            return values.TryGetValue(key, out var value) ? value : null;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromResult(Get(key));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (ThrowOnSet) throw new InvalidOperationException("Redis unavailable.");
            values[key] = value;
        }

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key)
        {
            if (ThrowOnRemove) throw new InvalidOperationException("Redis unavailable.");
            values.TryRemove(key, out _);
        }
        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }
}
