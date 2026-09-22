namespace IdentityHub.AuthService.Application.Auth;

public record RegisterRequest(
    Guid UserId,
    string Email,
    string Password
);