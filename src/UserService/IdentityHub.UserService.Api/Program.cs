using System.Text;
using IdentityHub.UserService.Application.Abstractions;
using IdentityHub.UserService.Application.Users;
using IdentityHub.UserService.Domain.Entities;
using IdentityHub.UserService.Infrastructure.Repositories;
using IdentityHub.UserService.Infrastructure.Persistence;
using IdentityHub.UserService.Infrastructure.Messaging;
using IdentityHub.UserService.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;
using System.Security.Claims;

const string seedUserEmail = "hectorjosuegc@example.com";
var seedUserId = Guid.Parse("3d7fbb77-07f1-4f86-8f01-b3e98e3d4a11");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", "UserService")
        .WriteTo.Console(new RenderedCompactJsonFormatter());
});

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddOpenApi();
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlServer")
    ));
builder.Services.AddHealthChecks()
    .AddDbContextCheck<UserDbContext>("sqlserver");

builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<IIntegrationEventPublisher, RabbitMqEventPublisher>();

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
    var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await dbContext.Database.MigrateAsync();

    if (app.Environment.IsDevelopment() &&
        !await dbContext.Users.AnyAsync(user => user.Email == seedUserEmail))
    {
        dbContext.Users.Add(new User(
            seedUserId,
            "Hector",
            "Josue GC",
            seedUserEmail));
        await dbContext.SaveChangesAsync();
        app.Logger.LogInformation("Development seed user created {UserId} {Email}", seedUserId, seedUserEmail);
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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

app.MapGet("/api/users", async (IUserRepository userRepository) =>
{
    var users = await userRepository.GetAllAsync();
    return Results.Ok(users);
}).RequireAuthorization();

app.MapGet("/api/users/{id:guid}", async (Guid id, IUserRepository userRepository) =>
{
    var user = await userRepository.GetByIdAsync(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.MapGet("/internal/users/{id:guid}", async (
    Guid id,
    HttpRequest request,
    IConfiguration configuration,
    IUserRepository userRepository) =>
{
    var expectedApiKey = configuration["InternalServices:ApiKey"];
    var providedApiKey = request.Headers["X-Internal-Api-Key"].ToString();

    if (string.IsNullOrWhiteSpace(expectedApiKey) ||
        !string.Equals(providedApiKey, expectedApiKey, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    var user = await userRepository.GetByIdAsync(id);
    return user is null
        ? Results.NotFound()
        : Results.Ok(new { user.Id, user.Email, user.IsActive });
});

app.MapPost("/api/users", async (
    CreateUserRequest request,
    IUserRepository userRepository,
    IIntegrationEventPublisher eventPublisher,
    ILogger<Program> logger) =>
{
    var existingUser = await userRepository.GetByEmailAsync(request.Email);
    if (existingUser is not null)
    {
        return Results.Conflict($"A user with email '{request.Email}' already exists.");
    }

    var newUser = new User(request.FirstName, request.LastName, request.Email);
    await userRepository.AddAsync(newUser);
    await eventPublisher.PublishAsync(new IntegrationEvent(
        Guid.NewGuid(), "UserCreated", "UserService", DateTime.UtcNow, null,
        new Dictionary<string, object?>
        {
            ["UserId"] = newUser.Id,
            ["FirstName"] = newUser.FirstName,
            ["LastName"] = newUser.LastName,
            ["Email"] = newUser.Email
        }));
    logger.LogInformation("User created {UserId} {Email}", newUser.Id, newUser.Email);
    return Results.Created($"/api/users/{newUser.Id}", newUser);
});

app.MapPut("/api/users/{id:guid}", async (
    Guid id,
    UpdateUserRequest request,
    IUserRepository userRepository,
    ILogger<Program> logger) =>
{
    var user = await userRepository.GetByIdAsync(id);
    if (user is null)
    {
        return Results.NotFound();
    }

    var existingUser = await userRepository.GetByEmailAsync(request.Email);
    if (existingUser is not null && existingUser.Id != id)
    {
        return Results.Conflict($"A user with email '{request.Email}' already exists.");
    }

    user.Update(
        request.FirstName,
        request.LastName,
        request.Email
    );
    await userRepository.UpdateAsync(user);
    logger.LogInformation("User updated {UserId}", user.Id);
    return Results.Ok(user);
}).RequireAuthorization();

app.MapPut("/api/users/{id:guid}/status", async (
    Guid id,
    SetUserStatusRequest request,
    IUserRepository userRepository,
    IIntegrationEventPublisher eventPublisher,
    ILogger<Program> logger) =>
{
    var user = await userRepository.GetByIdAsync(id);
    if (user is null)
    {
        return Results.NotFound();
    }

    if (request.IsActive)
    {
        user.Activate();
    }
    else
    {
        user.Desactivate();
    }

    await userRepository.UpdateAsync(user);
    await eventPublisher.PublishAsync(new IntegrationEvent(
        Guid.NewGuid(),
        request.IsActive ? "UserActivated" : "UserDeactivated",
        "UserService",
        DateTime.UtcNow,
        user.Id.ToString(),
        new Dictionary<string, object?>
        {
            ["UserId"] = user.Id,
            ["Email"] = user.Email,
            ["IsActive"] = user.IsActive
        }));
    logger.LogInformation("User status changed {UserId} {IsActive}", user.Id, user.IsActive);
    return Results.Ok(user);
}).RequireAuthorization();

app.Run();
