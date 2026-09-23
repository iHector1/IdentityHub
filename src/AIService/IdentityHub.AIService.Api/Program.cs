using System.Security.Claims;
using System.Text;
using IdentityHub.AIService.Api.Middleware;
using IdentityHub.AIService.Application.Services;
using IdentityHub.AIService.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", "AIService")
        .WriteTo.Console(new RenderedCompactJsonFormatter());
});

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<RagService>();
builder.Services.AddAiInfrastructure(builder.Configuration);

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is not configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
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
        if (!string.IsNullOrWhiteSpace(userId)) diagnosticContext.Set("UserId", userId);
    };
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserLogContextMiddleware>();

app.MapHealthChecks("/health").AllowAnonymous();

var protectedApi = app.MapGroup("/api/ai").RequireAuthorization();
protectedApi.MapPost("/index", async (RagService ragService, CancellationToken cancellationToken) =>
{
    var result = await ragService.IndexAsync(cancellationToken);
    return Results.Ok(new
    {
        result.DocumentsIndexed,
        result.ChunksIndexed,
        result.LatencyMs
    });
});

protectedApi.MapPost("/ask", async (
    AskRequest request,
    RagService ragService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
        return Results.BadRequest(new { error = "Question is required." });

    try
    {
        var result = await ragService.AskAsync(request.Question, cancellationToken);
        return Results.Ok(new
        {
            result.Answer,
            result.Sources,
            result.LatencyMs,
            result.Quality,
            Usage = new
            {
                result.Usage.InputTokens,
                result.Usage.OutputTokens,
                result.Usage.TotalTokens,
                result.Usage.EstimatedCost
            }
        });
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

app.Run();

public sealed record AskRequest(string Question);
