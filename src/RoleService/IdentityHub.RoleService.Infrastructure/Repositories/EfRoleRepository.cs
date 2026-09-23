using IdentityHub.RoleService.Application.Abstractions;
using IdentityHub.RoleService.Domain.Entities;
using IdentityHub.RoleService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.RoleService.Infrastructure.Repositories;

public sealed class EfRoleRepository(RoleDbContext dbContext) : IRoleRepository
{
    public async Task<IReadOnlyCollection<Role>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Roles.AsNoTracking().OrderBy(role => role.Name).ToListAsync(cancellationToken);

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Roles.FirstOrDefaultAsync(role => role.Id == id, cancellationToken);

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        return dbContext.Roles.FirstOrDefaultAsync(role => role.Name == normalizedName, cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await dbContext.Roles.AddAsync(role, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        dbContext.Roles.Update(role);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
