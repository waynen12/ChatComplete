using Microsoft.AspNetCore.Http;

namespace Knowledge.Mcp.Middleware;

/// <summary>
/// Middleware that adds WWW-Authenticate header to 401 Unauthorized responses
/// as required by MCP Authorization specification
/// </summary>
public class WWWAuthenticateHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _authorizationUri;

    public WWWAuthenticateHeaderMiddleware(RequestDelegate next, string serverUrl)
    {
        _next = next;
        _authorizationUri = $"{serverUrl}/.well-known/oauth-authorization-server";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Register callback to modify headers before response starts
        context.Response.OnStarting(() =>
        {
            // If response is 401 Unauthorized, enhance WWW-Authenticate header
            if (context.Response.StatusCode == 401)
            {
                // Replace basic "Bearer" header from JWT middleware with enhanced version
                // that includes authorization_uri per MCP specification
                context.Response.Headers["WWW-Authenticate"] =
                    $"Bearer realm=\"mcp-server\", authorization_uri=\"{_authorizationUri}\"";
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }
}

/// <summary>
/// Extension method to register WWW-Authenticate header middleware
/// </summary>
public static class WWWAuthenticateHeaderMiddlewareExtensions
{
    public static IApplicationBuilder UseWWWAuthenticateHeader(
        this IApplicationBuilder app,
        string serverUrl)
    {
        return app.UseMiddleware<WWWAuthenticateHeaderMiddleware>(serverUrl);
    }
}
