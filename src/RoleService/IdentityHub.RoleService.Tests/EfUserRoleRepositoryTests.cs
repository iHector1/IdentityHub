using IdentityHub.RoleService.Domain.Entities;
using IdentityHub.RoleService.Infrastructure.Persistence;
using IdentityHub.RoleService.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.RoleService.Tests;

public sealed class EfUserRoleRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RoleDbContext _dbContext;
    private readonly EfUserRoleRepository _repository;

    public EfUserRoleRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbContext = new RoleDbContext(
            new DbContextOptionsBuilder<RoleDbContext>()
                .UseSqlite(_connection)
                .Options);
        _dbContext.Database.EnsureCreated();
        _repository = new EfUserRoleRepository(_dbContext);
    }

    [Fact]
    public async Task AddAsync_GetAsync_AndGetByUserIdAsync_ShouldPersistAssignment()
    {
        var userId = Guid.NewGuid();
        var userRole = new UserRole(userId, Guid.NewGuid());

        await _repository.AddAsync(userRole);

        var result = await _repository.GetAsync(userRole.RoleId, userId);
        var assignments = await _repository.GetByUserIdAsync(userId);
        Assert.NotNull(result);
        Assert.Equal(userRole.Id, result.Id);
        Assert.Single(assignments);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateUserAndRole_ShouldFailUniqueIndex()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        await _repository.AddAsync(new UserRole(userId, roleId));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            _repository.AddAsync(new UserRole(userId, roleId)));
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveAssignment()
    {
        var userRole = new UserRole(Guid.NewGuid(), Guid.NewGuid());
        await _repository.AddAsync(userRole);

        await _repository.DeleteAsync(userRole);

        Assert.Null(await _repository.GetAsync(userRole.RoleId, userRole.UserId));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
