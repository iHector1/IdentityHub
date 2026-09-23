using System.Text;
using IdentityHub.AuditService.Application.Abstractions;
using IdentityHub.AuditService.Infrastructure.Messaging;
using IdentityHub.AuditService.Infrastructure.Repositories;
using IdentityHub.AuditService.Api.Health;
using IdentityHub.AuditService.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        .Enrich.WithProperty("ServiceName", "AuditService")
        .WriteTo.Console(new RenderedCompactJsonFormatter());
});

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IAuditRepository, MongoAuditRepository>();
builder.Services.AddHostedService<RabbitMqAuditConsumer>();
builder.Services.AddHealthChecks()
    .AddCheck<MongoDbHealthCheck>("mongodb");

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
