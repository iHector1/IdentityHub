using System.Text;
using IdentityHub.AuthService.Application.Abstractions;
using IdentityHub.AuthService.Application.Auth;
using IdentityHub.AuthService.Domain.Entities;
using IdentityHub.AuthService.Infrastructure.Persistence;
using IdentityHub.AuthService.Infrastructure.Repositories;
using IdentityHub.AuthService.Infrastructure.Security;
using IdentityHub.AuthService.Infrastructure.Clients;
using IdentityHub.AuthService.Infrastructure.Messaging;
using IdentityHub.AuthService.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;
using System.Security.Claims;

const string seedUserEmail = "hectorjosuegc@example.com";
const string seedUserPassword = "12345";
var seedUserId = Guid.Parse("3d7fbb77-07f1-4f86-8f01-b3e98e3d4a11");

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
        .Enrich.WithProperty("ServiceName", "AuthService")
        .WriteTo.Console(new RenderedCompactJsonFormatter());
});

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlServer")
));
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AuthDbContext>("sqlserver");

builder.Services.AddScoped<ICredentialRepository, EfCredentialRepository>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IIntegrationEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
{
    var userServiceUrl = builder.Configuration["Services:UserService"]
        ?? throw new InvalidOperationException("UserService URL is not configured.");

    client.BaseAddress = new Uri(userServiceUrl);
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await dbContext.Database.MigrateAsync();

    if (app.Environment.IsDevelopment() &&
        !await dbContext.Credentials.AnyAsync(credential => credential.Email == seedUserEmail))
    {
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        dbContext.Credentials.Add(new UserCredential(
            seedUserId,
            seedUserEmail,
            passwordHasher.Hash(seedUserPassword)));
        await dbContext.SaveChangesAsync();
        app.Logger.LogInformation("Development seed credential created for {UserId} {Email}", seedUserId, seedUserEmail);
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

app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    ICredentialRepository credentialRepository,
    IPasswordHasher passwordHasher,
    IUserServiceClient userServiceClient,
    ILogger<Program> logger) =>
{
    var user = await userServiceClient.GetByIdAsync(request.UserId);

    if (user is null)
    {
        return Results.BadRequest("The specified user does not exist.");
    }

    if (!user.IsActive)
    {
        return Results.BadRequest("The user is inactive.");
    }

    if (string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest("Email is required.");
    }

    var normalizedRequestEmail = request.Email.Trim().ToLowerInvariant();
    var normalizedUserEmail = user.Email.Trim().ToLowerInvariant();

    if (normalizedRequestEmail != normalizedUserEmail)
    {
        return Results.BadRequest("The email does not match the specified user.");
    }

    var existingCredential =
        await credentialRepository.GetByEmailAsync(normalizedRequestEmail);

    if (existingCredential is not null)
    {
        return Results.Conflict(
            $"Credentials for '{request.Email}' already exist."
        );
    }

    if (string.IsNullOrWhiteSpace(request.Password) ||
        request.Password.Length < 8)
    {
        return Results.BadRequest(
            "Password must contain at least 8 characters."
        );
    }

    var passwordHash = passwordHasher.Hash(request.Password);

    var credential = new UserCredential(
        request.UserId,
        normalizedRequestEmail,
        passwordHash
    );

    await credentialRepository.AddAsync(credential);
    logger.LogInformation("Credential created for user {UserId}", credential.UserId);

    return Results.Created(
        $"/api/auth/credentials/{credential.Id}",
        new
        {
            credential.Id,
            credential.UserId,
            credential.Email,
            credential.CreatedAt
        }
    );

});

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    ICredentialRepository credentialRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IIntegrationEventPublisher eventPublisher,
    IUserServiceClient userServiceClient,
    ILogger<Program> logger) =>

{
    var credential =
        await credentialRepository.GetByEmailAsync(request.Email);

    if (credential is null)
    {
        logger.LogWarning("Login failed for email {Email}", request.Email.Trim().ToLowerInvariant());
        await eventPublisher.PublishAsync(new IntegrationEvent(
            Guid.NewGuid(), "LoginFailed", "AuthService", DateTime.UtcNow, null,
            new Dictionary<string, object?> { ["Email"] = request.Email.Trim().ToLowerInvariant() }));
        return Results.Unauthorized();
    }

    var validPassword = passwordHasher.Verify(

        request.Password,

        credential.PasswordHash
    );

    if (!validPassword)
    {
        logger.LogWarning("Login failed for email {Email}", credential.Email);
        await eventPublisher.PublishAsync(new IntegrationEvent(
            Guid.NewGuid(), "LoginFailed", "AuthService", DateTime.UtcNow, null,
            new Dictionary<string, object?>
            {
                ["UserId"] = credential.UserId,
                ["Email"] = credential.Email
            }));
        return Results.Unauthorized();
    }

    var user = await userServiceClient.GetByIdAsync(credential.UserId);
    if (user is null || !user.IsActive)
    {
        logger.LogWarning("Login failed because user is inactive or missing {UserId}", credential.UserId);
        await eventPublisher.PublishAsync(new IntegrationEvent(
            Guid.NewGuid(), "LoginFailed", "AuthService", DateTime.UtcNow, null,
            new Dictionary<string, object?>
            {
                ["UserId"] = credential.UserId,
                ["Email"] = credential.Email,
                ["Reason"] = "InactiveUser"
            }));
        return Results.Unauthorized();
    }

    var token = jwtTokenGenerator.Generate(credential);

    await eventPublisher.PublishAsync(new IntegrationEvent(
        Guid.NewGuid(), "LoginSucceeded", "AuthService", DateTime.UtcNow, null,
        new Dictionary<string, object?>
        {
            ["UserId"] = credential.UserId,
            ["Email"] = credential.Email
        }));
    logger.LogInformation("Login succeeded for user {UserId}", credential.UserId);

    return Results.Ok(
        new AuthResponse(
         token,
         DateTime.UtcNow.AddMinutes(60)
        )
    );

});

app.MapGet("/api/auth/me", () =>
{
    return Results.Ok(new
    {
        Message = "You are authenticated."
    });

}).RequireAuthorization();

app.Run();
