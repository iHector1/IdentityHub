using System.Collections.Concurrent;
using IdentityHub.UserService.Application.Abstractions;
using IdentityHub.UserService.Domain.Entities;

namespace IdentityHub.UserService.Infrastructure.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public Task<IReadOnlyCollection<User>> GetAllAsync()
    {
        IReadOnlyCollection<User> users = _users.Values.ToList();
        return Task.FromResult(users);
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        var user = _users.Values.FirstOrDefault(user =>
            string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task AddAsync(User user)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(User user)
    {
        _users.TryRemove(user.Id, out _);
        return Task.CompletedTask;
    }
}