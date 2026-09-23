using IdentityHub.AuthService.Application.Auth;
using IdentityHub.AuthService.Domain.Entities;

namespace IdentityHub.AuthService.Tests;

public sealed class UserCredentialTests
{
    [Fact]
    public void CreateCredential_WithValidData_ShouldSetIdentityAndNormalizeEmail()
    {
        var userId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var credential = new UserCredential(userId, "  USER@Example.COM ", "hashed-password");

        var after = DateTime.UtcNow;
        Assert.NotEqual(Guid.Empty, credential.Id);
        Assert.Equal(userId, credential.UserId);
        Assert.Equal("user@example.com", credential.Email);
        Assert.Equal("hashed-password", credential.PasswordHash);
        Assert.InRange(credential.CreatedAt, before, after);
    }

    [Fact]
    public void AuthRequests_ShouldExposeTheirRequiredValues()
    {
        var userId = Guid.NewGuid();
        var register = new RegisterRequest(userId, "user@example.com", "password");
        var login = new LoginRequest("user@example.com", "password");
        var response = new AuthResponse("token", DateTime.UtcNow);

        Assert.Equal(userId, register.UserId);
        Assert.Equal("password", register.Password);
        Assert.Equal("user@example.com", login.Email);
        Assert.Equal("token", response.Token);
    }
}
