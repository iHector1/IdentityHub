using System.Text;
using IdentityHub.RoleService.Application.Abstractions;
using IdentityHub.RoleService.Application.Roles;
using IdentityHub.RoleService.Domain.Entities;
using IdentityHub.RoleService.Infrastructure.Clients;
using IdentityHub.RoleService.Infrastructure.Messaging;
using IdentityHub.RoleService.Infrastructure.Persistence;
using IdentityHub.RoleService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<RoleDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
builder.Services.AddScoped<IRoleRepository, EfRoleRepository>();
builder.Services.AddScoped<IUserRoleRepository, EfUserRoleRepository>();
builder.Services.AddScoped<IIntegrationEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
{
    var userServiceUrl = builder.Configuration["Services:UserService"]
        ?? throw new InvalidOperationException("UserService URL is not configured.");
    client.BaseAddress = new Uri(userServiceUrl);
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is not configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RoleDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var protectedApi = app.MapGroup("/api").RequireAuthorization();

protectedApi.MapGet("/roles", async (IRoleRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetAllAsync(cancellationToken)));

protectedApi.MapGet("/roles/{id:guid}", async (
    Guid id,
    IRoleRepository repository,
    CancellationToken cancellationToken) =>
{
    var role = await repository.GetByIdAsync(id, cancellationToken);
    return role is null ? Results.NotFound() : Results.Ok(role);
});

protectedApi.MapPost("/roles", async (
    CreateRoleRequest request,
    IRoleRepository repository,
    IIntegrationEventPublisher eventPublisher,
    CancellationToken cancellationToken) =>
{
    try
    {
        if (await repository.GetByNameAsync(request.Name, cancellationToken) is not null)
            return Results.Conflict($"A role with name '{request.Name}' already exists.");

        var role = new Role(request.Name, request.Description);
        await repository.AddAsync(role, cancellationToken);
        await eventPublisher.PublishAsync(new IntegrationEvent(
            Guid.NewGuid(), "RoleCreated", "RoleService", DateTime.UtcNow, null,
            new Dictionary<string, object?> { ["RoleId"] = role.Id, ["Name"] = role.Name }), cancellationToken);

        return Results.Created($"/api/roles/{role.Id}", role);
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(exception.Message);
    }
});

protectedApi.MapPut("/roles/{id:guid}", async (
    Guid id,
    UpdateRoleRequest request,
    IRoleRepository repository,
    CancellationToken cancellationToken) =>
{
    var role = await repository.GetByIdAsync(id, cancellationToken);
    if (role is null) return Results.NotFound();

    try
    {
        var duplicate = await repository.GetByNameAsync(request.Name, cancellationToken);
        if (duplicate is not null && duplicate.Id != role.Id)
            return Results.Conflict($"A role with name '{request.Name}' already exists.");

        role.Update(request.Name, request.Description);
        if (request.IsActive is true) role.Activate();
        if (request.IsActive is false) role.Deactivate();
        await repository.UpdateAsync(role, cancellationToken);
        return Results.Ok(role);
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(exception.Message);
    }
});

protectedApi.MapDelete("/roles/{id:guid}", async (
    Guid id,
    IRoleRepository repository,
    CancellationToken cancellationToken) =>
{
    var role = await repository.GetByIdAsync(id, cancellationToken);
    if (role is null) return Results.NotFound();

    role.Deactivate();
    await repository.UpdateAsync(role, cancellationToken);
    return Results.NoContent();
});

protectedApi.MapPost("/roles/{roleId:guid}/users/{userId:guid}", async (
    Guid roleId,
    Guid userId,
    IRoleRepository roleRepository,
    IUserRoleRepository userRoleRepository,
    IUserServiceClient userServiceClient,
    IIntegrationEventPublisher eventPublisher,
    CancellationToken cancellationToken) =>
{
    UserServiceUser? user;
    try
    {
        user = await userServiceClient.GetByIdAsync(userId, cancellationToken);
    }
    catch (HttpRequestException)
    {
        return Results.Problem("UserService is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (user is null) return Results.BadRequest("The specified user does not exist.");
    if (!user.IsActive) return Results.BadRequest("The specified user is inactive.");

    var role = await roleRepository.GetByIdAsync(roleId, cancellationToken);
    if (role is null) return Results.NotFound("The specified role does not exist.");
    if (!role.IsActive) return Results.BadRequest("The specified role is inactive.");
    if (await userRoleRepository.GetAsync(roleId, userId, cancellationToken) is not null)
        return Results.Conflict("The user already has this role.");

    var userRole = new UserRole(userId, roleId);
    await userRoleRepository.AddAsync(userRole, cancellationToken);
    await eventPublisher.PublishAsync(new IntegrationEvent(
        Guid.NewGuid(), "RoleAssigned", "RoleService", DateTime.UtcNow, null,
        new Dictionary<string, object?> { ["RoleId"] = roleId, ["UserId"] = userId }), cancellationToken);

    return Results.Created($"/api/roles/{roleId}/users/{userId}", userRole);
});

protectedApi.MapDelete("/roles/{roleId:guid}/users/{userId:guid}", async (
    Guid roleId,
    Guid userId,
    IUserRoleRepository repository,
    IIntegrationEventPublisher eventPublisher,
    CancellationToken cancellationToken) =>
{
    var userRole = await repository.GetAsync(roleId, userId, cancellationToken);
    if (userRole is null) return Results.NotFound();

    await repository.DeleteAsync(userRole, cancellationToken);
    await eventPublisher.PublishAsync(new IntegrationEvent(
        Guid.NewGuid(), "RoleRemoved", "RoleService", DateTime.UtcNow, null,
        new Dictionary<string, object?> { ["RoleId"] = roleId, ["UserId"] = userId }), cancellationToken);
    return Results.NoContent();
});

protectedApi.MapGet("/roles/users/{userId:guid}", async (
    Guid userId,
    IUserRoleRepository userRoleRepository,
    IRoleRepository roleRepository,
    CancellationToken cancellationToken) =>
{
    var assignments = await userRoleRepository.GetByUserIdAsync(userId, cancellationToken);
    var roles = new List<object>();
    foreach (var assignment in assignments)
    {
        var role = await roleRepository.GetByIdAsync(assignment.RoleId, cancellationToken);
        if (role is not null)
            roles.Add(new { role.Id, role.Name, role.Description, role.IsActive, assignment.AssignedAt });
    }

    return Results.Ok(roles);
});

app.Run();
