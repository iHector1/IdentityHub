using System.Text;
using IdentityHub.RoleService.Application.Abstractions;
using IdentityHub.RoleService.Application.Roles;
using IdentityHub.RoleService.Domain.Entities;
using IdentityHub.RoleService.Infrastructure.Clients;
using IdentityHub.RoleService.Infrastructure.Messaging;
using IdentityHub.RoleService.Infrastructure.Persistence;
using IdentityHub.RoleService.Infrastructure.Repositories;
using IdentityHub.RoleService.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", "RoleService")
        .WriteTo.Console(new RenderedCompactJsonFormatter());
});

builder.Services.AddOpenApi();
builder.Services.AddDbContext<RoleDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
builder.Services.AddHealthChecks()
    .AddDbContextCheck<RoleDbContext>("sqlserver");
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

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
        diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value ?? "/");
        diagnosticContext.Set("StatusCode", httpContext.Response.StatusCode);

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
            diagnosticContext.Set("UserId", userId);
    };
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserLogContextMiddleware>();

app.MapHealthChecks("/health").AllowAnonymous();

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
    CancellationToken cancellationToken,
    ILogger<Program> logger) =>
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
        logger.LogInformation("Role created {RoleId} {RoleName}", role.Id, role.Name);

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
    CancellationToken cancellationToken,
    ILogger<Program> logger) =>
{
    var role = await repository.GetByIdAsync(id, cancellationToken);
    if (role is null) return Results.NotFound();

    role.Deactivate();
    await repository.UpdateAsync(role, cancellationToken);
    logger.LogInformation("Role deactivated {RoleId}", role.Id);
    return Results.NoContent();
});

protectedApi.MapPost("/roles/{roleId:guid}/users/{userId:guid}", async (
    Guid roleId,
    Guid userId,
    IRoleRepository roleRepository,
    IUserRoleRepository userRoleRepository,
    IUserServiceClient userServiceClient,
    IIntegrationEventPublisher eventPublisher,
    CancellationToken cancellationToken,
    ILogger<Program> logger) =>
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
    logger.LogInformation("Role assigned {RoleId} to user {UserId}", roleId, userId);

    return Results.Created($"/api/roles/{roleId}/users/{userId}", userRole);
});

protectedApi.MapDelete("/roles/{roleId:guid}/users/{userId:guid}", async (
    Guid roleId,
    Guid userId,
    IUserRoleRepository repository,
    IIntegrationEventPublisher eventPublisher,
    CancellationToken cancellationToken,
    ILogger<Program> logger) =>
{
    var userRole = await repository.GetAsync(roleId, userId, cancellationToken);
    if (userRole is null) return Results.NotFound();

    await repository.DeleteAsync(userRole, cancellationToken);
    await eventPublisher.PublishAsync(new IntegrationEvent(
        Guid.NewGuid(), "RoleRemoved", "RoleService", DateTime.UtcNow, null,
        new Dictionary<string, object?> { ["RoleId"] = roleId, ["UserId"] = userId }), cancellationToken);
    logger.LogInformation("Role removed {RoleId} from user {UserId}", roleId, userId);
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
