using System.Security.Claims;
using Serilog.Context;

namespace IdentityHub.AIService.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString("D");

        context.Response.Headers[HeaderName] = correlationId;
        using (LogContext.PushProperty("CorrelationId", correlationId))
            await next(context);
    }
}

public sealed class UserLogContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            await next(context);
            return;
        }

        using (LogContext.PushProperty("UserId", userId))
            await next(context);
    }
}

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing {RequestMethod} {RequestPath}", context.Request.Method, context.Request.Path);
            if (context.Response.HasStarted) throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "An unexpected error occurred.",
                correlationId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString()
            });
        }
    }
}
