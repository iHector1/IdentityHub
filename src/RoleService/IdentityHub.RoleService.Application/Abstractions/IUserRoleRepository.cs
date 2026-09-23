using IdentityHub.RoleService.Domain.Entities;

namespace IdentityHub.RoleService.Application.Abstractions;

public interface IUserRoleRepository
{
    Task<UserRole?> GetAsync(Guid roleId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserRole>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserRole userRole, CancellationToken cancellationToken = default);
}
