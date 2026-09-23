using IdentityHub.UserService.Domain.Entities;
using IdentityHub.UserService.Infrastructure.Repositories;

namespace IdentityHub.UserService.Tests;

public sealed class InMemoryUserRepositoryTests
{
    [Fact]
    public async Task AddAsync_GetByIdAsync_AndGetAllAsync_ShouldReturnUser()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Ana", "García", "ana@example.com");

        await repository.AddAsync(user);

        Assert.Same(user, await repository.GetByIdAsync(user.Id));
        Assert.Single(await repository.GetAllAsync());
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldCompareWithoutCaseSensitivity()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Ana", "García", "ana@example.com");
        await repository.AddAsync(user);

        var result = await repository.GetByEmailAsync("ANA@EXAMPLE.COM");

        Assert.Same(user, result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReplaceStoredUser()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Ana", "García", "ana@example.com");
        await repository.AddAsync(user);
        user.Update("Beatriz", "López", "beatriz@example.com");

        await repository.UpdateAsync(user);

        Assert.Equal("beatriz@example.com", (await repository.GetByIdAsync(user.Id))!.Email);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveStoredUser()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Ana", "García", "ana@example.com");
        await repository.AddAsync(user);

        await repository.DeleteAsync(user);

        Assert.Null(await repository.GetByIdAsync(user.Id));
    }
}
