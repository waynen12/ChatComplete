using System.Text.Json;
using Knowledge.Api.Services;
using KnowledgeEngine.Logging;
using Knowledge.Data.Interfaces;
using Knowledge.Entities;
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
        group
            .MapGet(
                "/models",
                async ([FromServices] IOllamaApiService ollamaService, CancellationToken ct) =>
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
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get available Ollama models";
                op.Description = "Returns a list of locally installed Ollama models via Ollama API";
                op.Tags = [new OpenApiTag { Name = "Ollama" }];
                return op;
            })
            .Produces<List<string>>()
            .WithTags("Ollama");

        // GET /api/ollama/models/details - Get detailed model information
        group
            .MapGet(
                "/models/details",
                async (
                    [FromServices] IOllamaRepository repository,
                    [FromServices] IOllamaApiService ollamaService,
                    CancellationToken ct
                ) =>
                {
                    try
                    {
                        // Step 1: Get models from both sources in parallel
                        var ollamaModelsTask = ollamaService.GetInstalledModelsAsync(ct);
                        var sqliteModelsTask = repository.GetInstalledModelsAsync(ct);

                        await Task.WhenAll(ollamaModelsTask, sqliteModelsTask);

                        var ollamaModels = await ollamaModelsTask;
                        var sqliteModels = await sqliteModelsTask;

                        // Step 2: Find models that exist in Ollama but not in SQLite
                        var sqliteModelNames = new HashSet<string>(
                            sqliteModels.Select(m => m.Name)
                        );
                        var missingModels = ollamaModels
                            .Where(om => !sqliteModelNames.Contains(om.Name))
                            .ToList();

                        // Step 3: Sync missing models to SQLite
                        foreach (var missingModel in missingModels)
                        {
                            try
                            {
                                // Get detailed model information from Ollama
                                var modelDetails = await ollamaService.GetModelDetailsAsync(
                                    missingModel.Name,
                                    ct
                                );

                                // Use the size from the tags response (more reliable) if available
                                var modelSize = missingModel.Size > 0 ? missingModel.Size : (modelDetails?.Size ?? 0);

                                if (modelDetails != null || missingModel.Size > 0)
                                {
                                    // Create a model record for the pre-existing model
                                    var modelRecord = new OllamaModelRecord
                                    {
                                        Name = missingModel.Name,
                                        Size = modelSize, // Use the more reliable size from tags
                                        Family = modelDetails?.Details?.Family ?? missingModel.Details?.Family,
                                        ParameterSize = modelDetails?.Details?.ParameterSize ?? missingModel.Details?.ParameterSize,
                                        QuantizationLevel = modelDetails?.Details?.QuantizationLevel ?? missingModel.Details?.QuantizationLevel,
                                        Format = modelDetails?.Details?.Format ?? missingModel.Details?.Format,
                                        Template = modelDetails?.Template,
                                        Parameters = modelDetails?.Parameters,
                                        ModifiedAt = missingModel.ModifiedAt,
                                        InstalledAt = DateTime.UtcNow,
                                        IsAvailable = true,
                                        Status = "Installed",
                                    };

                                    // Add to SQLite database
                                    await repository.UpsertModelAsync(modelRecord, ct);

                                    LoggerProvider.Logger.Information(
                                        "Synced pre-existing model to database: {ModelName}",
                                        missingModel.Name
                                    );
                                }
                            }
                            catch (Exception ex)
                            {
                                LoggerProvider.Logger.Warning(
                                    ex,
                                    "Failed to sync model {ModelName} - skipping",
                                    missingModel.Name
                                );
                                // Continue with other models even if one fails
                            }
                        }

                        // Step 4: Update existing models that have zero sizes with correct sizes
                        var existingModelsWithZeroSize = sqliteModels
                            .Where(sm => sm.Size == 0)
                            .ToList();

                        foreach (var existingModel in existingModelsWithZeroSize)
                        {
                            try
                            {
                                var ollamaModel = ollamaModels.FirstOrDefault(om => om.Name == existingModel.Name);
                                if (ollamaModel != null && ollamaModel.Size > 0)
                                {
                                    // Update the existing model with correct size and metadata
                                    var updatedRecord = new OllamaModelRecord
                                    {
                                        Name = existingModel.Name,
                                        DisplayName = existingModel.DisplayName,
                                        Size = ollamaModel.Size, // Update with correct size
                                        Family = ollamaModel.Details?.Family ?? existingModel.Family,
                                        ParameterSize = ollamaModel.Details?.ParameterSize ?? existingModel.ParameterSize,
                                        QuantizationLevel = ollamaModel.Details?.QuantizationLevel ?? existingModel.QuantizationLevel,
                                        Format = ollamaModel.Details?.Format ?? existingModel.Format,
                                        Template = existingModel.Template,
                                        Parameters = existingModel.Parameters,
                                        ModifiedAt = ollamaModel.ModifiedAt,
                                        InstalledAt = existingModel.InstalledAt,
                                        IsAvailable = existingModel.IsAvailable,
                                        Status = existingModel.Status,
                                        SupportsTools = existingModel.SupportsTools,
                                        LastUsedAt = existingModel.LastUsedAt
                                    };

                                    await repository.UpsertModelAsync(updatedRecord, ct);

                                    LoggerProvider.Logger.Information(
                                        "Updated existing model size: {ModelName} - {Size} bytes",
                                        existingModel.Name,
                                        ollamaModel.Size
                                    );
                                }
                            }
                            catch (Exception ex)
                            {
                                LoggerProvider.Logger.Warning(
                                    ex,
                                    "Failed to update model size for {ModelName} - skipping",
                                    existingModel.Name
                                );
                            }
                        }

                        // Step 5: Return the updated model list from SQLite
                        var updatedModels = await repository.GetInstalledModelsAsync(ct);

                        var totalUpdated = missingModels.Count + existingModelsWithZeroSize.Count;
                        if (totalUpdated > 0)
                        {
                            LoggerProvider.Logger.Information(
                                "Synced {NewCount} new models and updated {ExistingCount} existing models in database",
                                missingModels.Count,
                                existingModelsWithZeroSize.Count
                            );
                        }

                        return Results.Ok(updatedModels);
                    }
                    catch (Exception ex)
                    {
                        LoggerProvider.Logger.Error(
                            ex,
                            "Failed to fetch detailed model information"
                        );
                        return Results.Problem("Failed to fetch model details", statusCode: 500);
                    }
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get detailed Ollama model information";
                op.Description =
                    "Returns detailed information about locally installed Ollama models";
                op.Tags = [new OpenApiTag { Name = "Ollama" }];
                return op;
            })
            .Produces<List<OllamaModelRecord>>()
            .WithTags("Ollama");

        // POST /api/ollama/models/download - Start model download
        group
            .MapPost(
                "/models/download",
                async (
                    [FromBody] DownloadModelRequest request,
                    [FromServices] OllamaDownloadService downloadService,
                    CancellationToken ct
                ) =>
                {
                    if (string.IsNullOrWhiteSpace(request.ModelName))
                    {
                        return Results.BadRequest("Model name is required");
                    }

                    try
                    {
                        var started = await downloadService.StartDownloadAsync(
                            request.ModelName,
                            ct
                        );
                        if (started)
                        {
                            return Results.Accepted(
                                $"/api/ollama/models/download/{request.ModelName}/progress"
                            );
                        }
                        else
                        {
                            return Results.Conflict("Model is already being downloaded");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerProvider.Logger.Error(
                            ex,
                            "Failed to start model download for {ModelName}",
                            request.ModelName
                        );
                        return Results.Problem("Failed to start model download", statusCode: 500);
                    }
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Download an Ollama model";
                op.Description = "Starts downloading a model from Ollama registry";
                op.Tags = [new OpenApiTag { Name = "Ollama" }];
                return op;
            })
            .Accepts<DownloadModelRequest>("application/json")
            .Produces(202)
            .Produces(400)
            .Produces(409)
            .WithTags("Ollama");

        // GET /api/ollama/models/download/{modelName}/progress - Stream download progress (SSE)
        group
            .MapGet(
                "/models/download/{modelName}/progress",
                async (
                    string modelName,
                    [FromServices] OllamaDownloadService downloadService,
                    HttpContext context,
                    CancellationToken ct
                ) =>
                {
                    if (string.IsNullOrWhiteSpace(modelName))
                    {
                        return Results.BadRequest("Model name is required");
                    }

                    // Set SSE headers
                    context.Response.Headers.Append("Content-Type", "text/event-stream");
                    context.Response.Headers.Append("Cache-Control", "no-cache");
                    context.Response.Headers.Append("Connection", "keep-alive");
                    context.Response.Headers.Append("Access-Control-Allow-Origin", "*");

                    try
                    {
                        await foreach (
                            var progressEvent in downloadService.StreamDownloadProgressAsync(
                                modelName,
                                ct
                            )
                        )
                        {
                            await context.Response.WriteAsync(progressEvent, ct);
                            await context.Response.Body.FlushAsync(ct);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Client disconnected, this is normal
                    }
                    catch (Exception ex)
                    {
                        LoggerProvider.Logger.Error(
                            ex,
                            "Error streaming download progress for {ModelName}",
                            modelName
                        );
                        await context.Response.WriteAsync(
                            $"event: error\ndata: {JsonSerializer.Serialize(new { error = "Stream error" })}\n\n",
                            ct
                        );
                    }

                    return Results.Empty;
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Stream model download progress";
                op.Description =
                    "Server-Sent Events stream of download progress for a specific model";
                op.Tags = [new OpenApiTag { Name = "Ollama" }];
                return op;
            })
            .Produces(200, contentType: "text/event-stream")
            .WithTags("Ollama");

        // GET /api/ollama/models/download/{modelName}/status - Get download status
        group
            .MapGet(
                "/models/download/{modelName}/status",
                async (
                    string modelName,
                    [FromServices] OllamaDownloadService downloadService,
                    CancellationToken ct
                ) =>
                {
                    if (string.IsNullOrWhiteSpace(modelName))
                    {
                        return Results.BadRequest("Model name is required");
                    }

                    try
                    {
                        var status = await downloadService.GetDownloadStatusAsync(modelName, ct);
                        if (status == null)
                        {
                            return Results.NotFound($"No download found for model '{modelName}'");
                        }

                        return Results.Ok(status);
                    }
                    catch (Exception ex)
                    {
                        LoggerProvider.Logger.Error(
                            ex,
                            "Failed to get download status for {ModelName}",
                            modelName
                        );
                        return Results.Problem("Failed to get download status", statusCode: 500);
                    }
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get model download status";
                op.Description = "Gets the current download status for a specific model";
                op.Tags = [new OpenApiTag { Name = "Ollama" }];
                return op;
            })
            .Produces<OllamaDownloadRecord>()
            .Produces(404)
            .WithTags("Ollama");

        // DELETE /api/ollama/models/download/{modelName} - Cancel model download
        group
            .MapDelete(
                "/models/download/{modelName}",
                async (
                    string modelName,
                    [FromServices] OllamaDownloadService downloadService,
                    CancellationToken ct
                ) =>
                {
                    if (string.IsNullOrWhiteSpace(modelName))
                    {
                        return Results.BadRequest("Model name is required");
                    }

                    try
                    {
                        var cancelled = await downloadService.CancelDownloadAsync(modelName);
                        if (cancelled)
                        {
                            return Results.Ok(
                                new { message = $"Download cancelled for model '{modelName}'" }
                            );
                        }
                        else
                        {
                            return Results.NotFound(
                                $"No active download found for model '{modelName}'"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerProvider.Logger.Error(
                            ex,
                            "Failed to cancel download for {ModelName}",
                            modelName
                        );
                        return Results.Problem("Failed to cancel download", statusCode: 500);
                    }
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Cancel model download";
                op.Description = "Cancels an active model download";
                op.Tags = [new OpenApiTag { Name = "Ollama" }];
                return op;
            })
            .Produces(200)
            .Produces(404)
            .WithTags("Ollama");

        // DELETE /api/ollama/models/{modelName} - Delete installed model
        group
            .MapDelete(
                "/models/{modelName}",
                async (
                    string modelName,
                    [FromServices] IOllamaApiService ollamaService,
                    [FromServices] IOllamaRepository repository,
                    CancellationToken ct
                ) =>
                {
                    if (string.IsNullOrWhiteSpace(modelName))
                    {
                        return Results.BadRequest("Model name is required");
                    }

                    try
                    {
                        // Delete from Ollama
                        var deleted = await ollamaService.DeleteModelAsync(modelName, ct);
                        if (deleted)
                        {
                            // Remove from local cache
                            await repository.DeleteModelAsync(modelName, ct);
                            return Results.Ok(
                                new { message = $"Model '{modelName}' deleted successfully" }
                            );
                        }
                        else
                        {
                            return Results.NotFound(
                                $"Model '{modelName}' not found or could not be deleted"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerProvider.Logger.Error(
                            ex,
                            "Failed to delete model {ModelName}",
                            modelName
                        );
                        return Results.Problem("Failed to delete model", statusCode: 500);
                    }
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Delete an Ollama model";
                op.Description = "Deletes a model from the Ollama service and local cache";
                op.Tags = [new OpenApiTag { Name = "Ollama" }];
                return op;
            })
            .Produces(200)
            .Produces(404)
            .WithTags("Ollama");

        // GET /api/ollama/downloads - Get all active downloads
        group
            .MapGet(
                "/downloads",
                async (
                    [FromServices] OllamaDownloadService downloadService,
                    CancellationToken ct
                ) =>
                {
                    try
                    {
                        var downloads = await downloadService.GetActiveDownloadsAsync(ct);
                        return Results.Ok(downloads);
                    }
                    catch (Exception ex)
                    {
                        LoggerProvider.Logger.Error(ex, "Failed to fetch active downloads");
                        return Results.Problem("Failed to fetch active downloads", statusCode: 500);
                    }
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get all active downloads";
                op.Description = "Returns a list of all currently active model downloads";
                op.Tags = [new OpenApiTag { Name = "Ollama" }];
                return op;
            })
            .Produces<List<OllamaDownloadRecord>>()
            .WithTags("Ollama");

        return group;
    }
}

/// <summary>
/// Request model for downloading an Ollama model
/// </summary>
public class DownloadModelRequest
{
    /// <summary>
    /// The name of the model to download (e.g., "llama3.2:3b")
    /// </summary>
    public string ModelName { get; set; } = string.Empty;
}
