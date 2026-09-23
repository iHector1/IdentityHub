namespace IdentityHub.RoleService.Application.Abstractions;

public interface IUserServiceClient
{
    Task<UserServiceUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record UserServiceUser(Guid Id, string Email, bool IsActive);
