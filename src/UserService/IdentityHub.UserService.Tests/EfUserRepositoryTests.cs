using IdentityHub.UserService.Domain.Entities;
using IdentityHub.UserService.Infrastructure.Persistence;
using IdentityHub.UserService.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.UserService.Tests;

public sealed class EfUserRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly UserDbContext _dbContext;
    private readonly EfUserRepository _repository;

    public EfUserRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbContext = new UserDbContext(
            new DbContextOptionsBuilder<UserDbContext>()
                .UseSqlite(_connection)
                .Options);
        _dbContext.Database.EnsureCreated();
        _repository = new EfUserRepository(_dbContext);
    }

    [Fact]
    public async Task AddAsync_AndGetByIdAsync_ShouldPersistUser()
    {
        var user = new User("Ana", "García", "ana@example.com");

        await _repository.AddAsync(user);

        var result = await _repository.GetByIdAsync(user.Id);
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("ana@example.com", result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldNormalizeLookupEmail()
    {
        var user = new User("Ana", "García", "ana@example.com");
        await _repository.AddAsync(user);

        var result = await _repository.GetByEmailAsync("  ANA@EXAMPLE.COM ");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPersistedUsers()
    {
        await _repository.AddAsync(new User("Ana", "García", "ana@example.com"));
        await _repository.AddAsync(new User("Luis", "Pérez", "luis@example.com"));

        var result = await _repository.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        var user = new User("Ana", "García", "ana@example.com");
        await _repository.AddAsync(user);

        user.Update("Beatriz", "López", "beatriz@example.com");
        await _repository.UpdateAsync(user);

        var result = await _repository.GetByIdAsync(user.Id);
        Assert.NotNull(result);
        Assert.Equal("beatriz@example.com", result.Email);
        Assert.Equal("Beatriz", result.FirstName);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUser()
    {
        var user = new User("Ana", "García", "ana@example.com");
        await _repository.AddAsync(user);

        await _repository.DeleteAsync(user);

        Assert.Null(await _repository.GetByIdAsync(user.Id));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
