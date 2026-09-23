using IdentityHub.RoleService.Domain.Entities;

namespace IdentityHub.RoleService.Tests;

public sealed class UserRoleTests
{
    [Fact]
    public void CreateUserRole_WithValidIds_ShouldSetIdentityAndTimestamp()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var userRole = new UserRole(userId, roleId);

        var after = DateTime.UtcNow;
        Assert.NotEqual(Guid.Empty, userRole.Id);
        Assert.Equal(userId, userRole.UserId);
        Assert.Equal(roleId, userRole.RoleId);
        Assert.InRange(userRole.AssignedAt, before, after);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CreateUserRole_WithEmptyId_ShouldThrowArgumentException(bool emptyUserId, bool emptyRoleId)
    {
        var userId = emptyUserId ? Guid.Empty : Guid.NewGuid();
        var roleId = emptyRoleId ? Guid.Empty : Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new UserRole(userId, roleId));
    }
}
