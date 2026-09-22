namespace IdentityHub.AuthService.Application.Abstractions;

public interface IUserServiceClient
{
    Task<UserInfo?> GetByIdAsync(Guid userId);
}

public record UserInfo(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive
);
