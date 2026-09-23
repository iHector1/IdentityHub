using IdentityHub.AuthService.Domain.Entities;
using IdentityHub.AuthService.Infrastructure.Persistence;
using IdentityHub.AuthService.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.AuthService.Tests;

public sealed class EfCredentialRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AuthDbContext _dbContext;
    private readonly EfCredentialRepository _repository;

    public EfCredentialRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbContext = new AuthDbContext(
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlite(_connection)
                .Options);
        _dbContext.Database.EnsureCreated();
        _repository = new EfCredentialRepository(_dbContext);
    }

    [Fact]
    public async Task AddAsync_AndGetByEmailAsync_ShouldPersistAndNormalizeLookup()
    {
        var credential = new UserCredential(Guid.NewGuid(), "user@example.com", "hash");

        await _repository.AddAsync(credential);

        var result = await _repository.GetByEmailAsync("  USER@EXAMPLE.COM ");
        Assert.NotNull(result);
        Assert.Equal(credential.Id, result.Id);
        Assert.Equal(credential.UserId, result.UserId);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenCredentialDoesNotExist_ShouldReturnNull()
    {
        var result = await _repository.GetByEmailAsync("missing@example.com");

        Assert.Null(result);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
