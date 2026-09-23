using IdentityHub.RoleService.Application.Abstractions;
using IdentityHub.RoleService.Domain.Entities;
using IdentityHub.RoleService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.RoleService.Infrastructure.Repositories;

public sealed class EfUserRoleRepository(RoleDbContext dbContext) : IUserRoleRepository
{
    public Task<UserRole?> GetAsync(Guid roleId, Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.UserRoles.FirstOrDefaultAsync(
            userRole => userRole.RoleId == roleId && userRole.UserId == userId,
            cancellationToken);

    public async Task<IReadOnlyCollection<UserRole>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.UserRoles.AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .OrderBy(userRole => userRole.AssignedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        await dbContext.UserRoles.AddAsync(userRole, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        dbContext.UserRoles.Remove(userRole);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
