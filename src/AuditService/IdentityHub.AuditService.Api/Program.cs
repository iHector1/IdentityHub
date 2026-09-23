using IdentityHub.AuditService.Application.Abstractions;
using IdentityHub.AuditService.Infrastructure.Messaging;
using IdentityHub.AuditService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IAuditRepository, MongoAuditRepository>();
builder.Services.AddHostedService<RabbitMqAuditConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

app.MapGet("/api/audit", async (
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

app.MapGet("/api/audit/{id}", async (
    string id,
    IAuditRepository repository,
    CancellationToken cancellationToken) =>
{
    var auditEvent = await repository.GetByIdAsync(id, cancellationToken);
    return auditEvent is null ? Results.NotFound() : Results.Ok(auditEvent);
});

app.MapGet("/api/audit/user/{userId}", async (
    string userId,
    IAuditRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetByUserIdAsync(userId, cancellationToken)));

app.MapGet("/api/audit/type/{eventType}", async (
    string eventType,
    IAuditRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetByEventTypeAsync(eventType, cancellationToken)));

app.Run();
