namespace Knowledge.Mcp.Endpoints;

/// <summary>
/// Health check endpoints for MCP server monitoring.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps health check endpoints to the application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    public static void MapHealthEndpoints(this WebApplication app)
    {
        // Basic health endpoint
        app.MapGet("/health", () =>
        {
            return Results.Ok(new
            {
                status = "healthy",
                service = "knowledge-mcp",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        })
        .WithName("Health")
        .WithTags("Health")
        .Produces(200);

        // Ping endpoint (alternative)
        app.MapGet("/ping", () => Results.Ok(new { message = "pong" }))
            .WithName("Ping")
            .WithTags("Health")
            .Produces(200);
    }
}
