using IdentityHub.RoleService.Domain.Entities;
using IdentityHub.RoleService.Infrastructure.Persistence;
using IdentityHub.RoleService.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.RoleService.Tests;

public sealed class EfRoleRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RoleDbContext _dbContext;
    private readonly EfRoleRepository _repository;

    public EfRoleRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbContext = new RoleDbContext(
            new DbContextOptionsBuilder<RoleDbContext>()
                .UseSqlite(_connection)
                .Options);
        _dbContext.Database.EnsureCreated();
        _repository = new EfRoleRepository(_dbContext);
    }

    [Fact]
    public async Task AddAsync_AndGetByIdAsync_ShouldPersistRole()
    {
        var role = new Role("Administrator", "Full access");

        await _repository.AddAsync(role);

        var result = await _repository.GetByIdAsync(role.Id);
        Assert.NotNull(result);
        Assert.Equal(role.Id, result.Id);
        Assert.Equal("Administrator", result.Name);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnRolesOrderedByName()
    {
        await _repository.AddAsync(new Role("Writer", null));
        await _repository.AddAsync(new Role("Administrator", null));

        var result = await _repository.GetAllAsync();

        Assert.Equal(new[] { "Administrator", "Writer" }, result.Select(role => role.Name));
    }

    [Fact]
    public async Task GetByNameAsync_ShouldTrimLookupName()
    {
        var role = new Role("Administrator", null);
        await _repository.AddAsync(role);

        var result = await _repository.GetByNameAsync("  Administrator ");

        Assert.NotNull(result);
        Assert.Equal(role.Id, result.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        var role = new Role("Reader", "Read only");
        await _repository.AddAsync(role);

        role.Update("Writer", "Can edit");
        await _repository.UpdateAsync(role);

        var result = await _repository.GetByIdAsync(role.Id);
        Assert.NotNull(result);
        Assert.Equal("Writer", result.Name);
        Assert.Equal("Can edit", result.Description);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
