namespace IdentityHub.AuthService.Application.Auth;

public record LoginRequest(
    string Email,
    string Password
);