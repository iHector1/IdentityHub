namespace IdentityHub.AuthService.Application.Abstractions;

public interface IUserServiceClient
{
    Task<UserInfo?> GetByIdAsync(Guid userId);
}

public record UserInfo(
    Guid Id,
    string Email,
    bool IsActive
);
