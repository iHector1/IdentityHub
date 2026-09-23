using IdentityHub.UserService.Application.Users;
using IdentityHub.UserService.Domain.Entities;

namespace IdentityHub.UserService.Tests;

public sealed class UserTests
{
    [Fact]
    public void CreateUser_WithValidData_ShouldNormalizeValuesAndSetDefaults()
    {
        var before = DateTime.UtcNow;

        var user = new User("  Ana ", " García  ", "  ANA@Example.COM ");

        var after = DateTime.UtcNow;
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Ana", user.FirstName);
        Assert.Equal("García", user.LastName);
        Assert.Equal("ana@example.com", user.Email);
        Assert.True(user.IsActive);
        Assert.InRange(user.CreatedAt, before, after);
        Assert.Null(user.UpdatedAt);
    }

    [Theory]
    [InlineData("", "Last", "user@example.com")]
    [InlineData("First", "", "user@example.com")]
    [InlineData("First", "Last", "")]
    [InlineData("   ", "Last", "user@example.com")]
    public void CreateUser_WithRequiredValueMissing_ShouldThrowArgumentException(
        string firstName,
        string lastName,
        string email)
    {
        Assert.Throws<ArgumentException>(() => new User(firstName, lastName, email));
    }

    [Fact]
    public void Update_WithValidData_ShouldNormalizeValuesAndSetUpdatedAt()
    {
        var user = new User("Ana", "García", "ana@example.com");

        user.Update("  Beatriz ", " López ", "  BEATRIZ@EXAMPLE.COM ");

        Assert.Equal("Beatriz", user.FirstName);
        Assert.Equal("López", user.LastName);
        Assert.Equal("beatriz@example.com", user.Email);
        Assert.NotNull(user.UpdatedAt);
    }

    [Fact]
    public void Desactivate_ShouldSetInactiveAndUpdatedAt()
    {
        var user = new User("Ana", "García", "ana@example.com");

        user.Desactivate();

        Assert.False(user.IsActive);
        Assert.NotNull(user.UpdatedAt);
    }

    [Fact]
    public void Activate_ShouldSetActiveAndUpdatedAt()
    {
        var user = new User("Ana", "García", "ana@example.com");
        user.Desactivate();

        user.Activate();

        Assert.True(user.IsActive);
        Assert.NotNull(user.UpdatedAt);
    }

    [Fact]
    public void UserRequests_ShouldExposeTheirRequiredValues()
    {
        var create = new CreateUserRequest("Ana", "García", "ana@example.com");
        var update = new UpdateUserRequest("Beatriz", "López", "beatriz@example.com");
        var status = new SetUserStatusRequest(false);

        Assert.Equal("Ana", create.FirstName);
        Assert.Equal("beatriz@example.com", update.Email);
        Assert.False(status.IsActive);
    }
}
