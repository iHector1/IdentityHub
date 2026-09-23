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
}
