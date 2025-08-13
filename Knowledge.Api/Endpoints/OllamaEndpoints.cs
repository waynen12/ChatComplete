using Knowledge.Api.Services;
using KnowledgeEngine.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace Knowledge.Api.Endpoints;

/// <summary>
/// Ollama model management endpoints.
/// </summary>
public static class OllamaEndpoints
{
    /// <summary>
    /// Maps Ollama-related endpoints to the application.
    /// </summary>
    public static RouteGroupBuilder MapOllamaEndpoints(this RouteGroupBuilder group)
    {
        // GET /api/ollama/models - Fetch available Ollama models via API
        group.MapGet("/models", async ([FromServices] IOllamaApiService ollamaService, CancellationToken ct) =>
            {
                try
                {
                    var models = await ollamaService.GetInstalledModelsAsync(ct);
                    
                    // Return model names for backwards compatibility with existing frontend
                    var modelNames = models.Select(m => m.Name).ToList();
                    return Results.Ok(modelNames);
                }
                catch (Exception ex)
                {
                    LoggerProvider.Logger.Error(ex, "Failed to fetch Ollama models via API");
                    return Results.Problem("Failed to fetch Ollama models", statusCode: 500);
                }
            })
            .WithOpenApi(op =>
            {
                op.Summary = "Get available Ollama models";
                op.Description = "Returns a list of locally installed Ollama models via Ollama API";
                op.Tags = [ new OpenApiTag { Name = "Ollama" } ];
                return op;
            })
            .Produces<List<string>>()
            .WithTags("Ollama");

        return group;
    }
}