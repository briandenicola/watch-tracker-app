using System.Security.Claims;

namespace WatchTracker.Api.Authentication;

/// <summary>
/// What an API key may change over the REST API. Any key may use GET, HEAD or
/// OPTIONS; POST, PUT, PATCH and DELETE need a key with the "write" scope
/// (Read + write in Settings). No key may create, change or revoke API keys:
/// that is done signed in. The MCP endpoint is left to its own scope checks,
/// and browser (JWT) requests pass unchanged.
/// </summary>
public class ApiKeyReadOnlyMiddleware(RequestDelegate next)
{
    public const string Message = "This API key can only read. Give it Read + write access in Settings, or sign in to the app to change data.";
    public const string KeysMessage = "API keys can't manage API keys. Sign in to the app to create, change or revoke them.";

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var changes = !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method));
        if (changes
            && context.User.Identity?.AuthenticationType == ApiKeyAuthenticationHandler.SchemeName
            && !context.Request.Path.StartsWithSegments("/api/mcp"))
        {
            string? refusal = null;
            if (context.Request.Path.StartsWithSegments("/api/apikeys"))
                refusal = KeysMessage;
            else if (!ApiKeyScopes.Has(context.User.FindFirstValue(ApiKeyScopes.ClaimType), ApiKeyScopes.Write))
                refusal = Message;
            if (refusal is not null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = refusal });
                return;
            }
        }

        await next(context);
    }
}
