using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WatchTracker.Api.Diagnostics;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        var traceId = context.TraceIdentifier;
        logger.LogError(
            exception,
            "Unhandled request failure for {Method} {Path}. TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            traceId);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected server error occurred.",
            Detail = environment.IsDevelopment() ? exception.Message : null,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = traceId;

        context.Response.StatusCode = problem.Status.Value;
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
