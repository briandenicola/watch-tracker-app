namespace WatchTracker.Api.Authentication;

/// <summary>
/// API keys only read. A request authenticated by API key may use GET, HEAD or
/// OPTIONS on the REST API; anything that would change data gets 403. The MCP
/// endpoint is left to its own scope checks. Browser (JWT) requests pass
/// unchanged.
/// </summary>
public class ApiKeyReadOnlyMiddleware(RequestDelegate next)
{
    public const string Message = "API keys are read-only. Sign in to the app to change data.";

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var changes = !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method));
        if (changes
            && context.User.Identity?.AuthenticationType == ApiKeyAuthenticationHandler.SchemeName
            && !context.Request.Path.StartsWithSegments("/api/mcp"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = Message });
            return;
        }

        await next(context);
    }
}
