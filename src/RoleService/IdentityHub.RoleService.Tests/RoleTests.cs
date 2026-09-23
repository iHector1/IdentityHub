using IdentityHub.RoleService.Domain.Entities;

namespace IdentityHub.RoleService.Tests;

public sealed class RoleTests
{
    [Fact]
    public void CreateRole_WithValidData_ShouldNormalizeNameAndSetActive()
    {
        var before = DateTime.UtcNow;

        var role = new Role("  Administrator  ", "  Full access  ");

        var after = DateTime.UtcNow;
        Assert.NotEqual(Guid.Empty, role.Id);
        Assert.Equal("Administrator", role.Name);
        Assert.Equal("Full access", role.Description);
        Assert.True(role.IsActive);
        Assert.InRange(role.CreatedAt, before, after);
        Assert.Null(role.UpdatedAt);
    }

    [Fact]
    public void CreateRole_WithEmptyName_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Role("  ", null));
    }

    [Fact]
    public void Update_ShouldNormalizeValuesAndSetUpdatedAt()
    {
        var role = new Role("Reader", "Read only");

        role.Update("  Writer ", "  Can edit  ");

        Assert.Equal("Writer", role.Name);
        Assert.Equal("Can edit", role.Description);
        Assert.NotNull(role.UpdatedAt);
    }

    [Fact]
    public void Update_WithBlankDescription_ShouldSetDescriptionToNull()
    {
        var role = new Role("Reader", "Read only");

        role.Update("Reader", "  ");

        Assert.Null(role.Description);
    }

    [Fact]
    public void DeactivateAndActivate_ShouldChangeStatusAndSetUpdatedAt()
    {
        var role = new Role("Reader", null);

        role.Deactivate();
        Assert.False(role.IsActive);
        Assert.NotNull(role.UpdatedAt);

        role.Activate();
        Assert.True(role.IsActive);
        Assert.NotNull(role.UpdatedAt);
    }
}
