using IdentityHub.AuthService.Infrastructure.Security;

namespace IdentityHub.AuthService.Tests;

public sealed class BcryptPasswordHasherTests
{
    [Fact]
    public void Hash_ShouldNotReturnTheOriginalPassword()
    {
        var password = "Strong-password-123!";
        var hasher = new BcryptPasswordHasher();

        var hash = hasher.Hash(password);

        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        var hasher = new BcryptPasswordHasher();
        var hash = hasher.Hash("Strong-password-123!");

        Assert.True(hasher.Verify("Strong-password-123!", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_ShouldReturnFalse()
    {
        var hasher = new BcryptPasswordHasher();
        var hash = hasher.Hash("Strong-password-123!");

        Assert.False(hasher.Verify("wrong-password", hash));
    }
}
