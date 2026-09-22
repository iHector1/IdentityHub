namespace IdentityHub.AuthService.Application.Auth;
public record AuthResponse(
    string Token,
    DateTime ExpiresAt
);