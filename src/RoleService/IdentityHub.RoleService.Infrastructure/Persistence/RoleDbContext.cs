using IdentityHub.RoleService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.RoleService.Infrastructure.Persistence;

public sealed class RoleDbContext(DbContextOptions<RoleDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Name).HasMaxLength(100).IsRequired();
            entity.Property(role => role.Description).HasMaxLength(500);
            entity.Property(role => role.IsActive).IsRequired();
            entity.Property(role => role.CreatedAt).IsRequired();
            entity.HasIndex(role => role.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(userRole => userRole.Id);
            entity.Property(userRole => userRole.UserId).IsRequired();
            entity.Property(userRole => userRole.RoleId).IsRequired();
            entity.Property(userRole => userRole.AssignedAt).IsRequired();
            entity.HasIndex(userRole => new { userRole.UserId, userRole.RoleId }).IsUnique();
        });
    }
}
