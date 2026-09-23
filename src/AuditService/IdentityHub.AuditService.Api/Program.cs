using System.Text;
using IdentityHub.AuditService.Application.Abstractions;
using IdentityHub.AuditService.Infrastructure.Messaging;
using IdentityHub.AuditService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IAuditRepository, MongoAuditRepository>();
builder.Services.AddHostedService<RabbitMqAuditConsumer>();

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

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var protectedApi = app.MapGroup("/api").RequireAuthorization();

protectedApi.MapGet("/audit", async (
    int? page,
    int? pageSize,
    IAuditRepository repository,
    CancellationToken cancellationToken) =>
{
    var currentPage = Math.Max(page ?? 1, 1);
    var currentPageSize = Math.Clamp(pageSize ?? 20, 1, 100);
    var events = await repository.GetPagedAsync(currentPage, currentPageSize, cancellationToken);
    return Results.Ok(new { Page = currentPage, PageSize = currentPageSize, Items = events });
});

protectedApi.MapGet("/audit/{id}", async (
    string id,
    IAuditRepository repository,
    CancellationToken cancellationToken) =>
{
    var auditEvent = await repository.GetByIdAsync(id, cancellationToken);
    return auditEvent is null ? Results.NotFound() : Results.Ok(auditEvent);
});

protectedApi.MapGet("/audit/user/{userId}", async (
    string userId,
    IAuditRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetByUserIdAsync(userId, cancellationToken)));

protectedApi.MapGet("/audit/type/{eventType}", async (
    string eventType,
    IAuditRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetByEventTypeAsync(eventType, cancellationToken)));

app.Run();
