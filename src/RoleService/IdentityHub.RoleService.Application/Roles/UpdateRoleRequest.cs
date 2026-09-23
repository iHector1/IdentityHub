namespace IdentityHub.RoleService.Application.Roles;

public sealed record UpdateRoleRequest(string Name, string? Description, bool? IsActive);
