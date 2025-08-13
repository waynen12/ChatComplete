using Knowledge.Api.Constants;
using Knowledge.Contracts;
using KnowledgeEngine.Logging;
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Persistence.IndexManagers;
using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace Knowledge.Api.Endpoints;

/// <summary>
/// Health check and monitoring endpoints.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps health-related endpoints to the application.
    /// </summary>
    public static RouteGroupBuilder MapHealthEndpoints(this RouteGroupBuilder group)
    {
        // Basic ping endpoint for simple health checks
        group.MapGet(ApiConstants.Routes.Ping, () => Results.Ok(ApiConstants.Messages.Pong))
            .WithTags(ApiConstants.Tags.Health)
            .WithOpenApi()
            .Produces<string>()
            .WithName("BasicHealth");

        // Comprehensive health check endpoint
        group.MapGet(ApiConstants.Routes.Health, async (
            [FromServices] IVectorStoreStrategy vectorStore,
            [FromServices] IIndexManager indexManager,
            CancellationToken ct) =>
        {
            var healthStatus = new HealthCheckDto
            {
                Status = ApiConstants.HealthStatus.Healthy,
                Timestamp = DateTime.UtcNow,
                Environment = Environment.GetEnvironmentVariable(ApiConstants.EnvironmentVariables.AspNetCoreEnvironment),
                Container = Environment.GetEnvironmentVariable(ApiConstants.EnvironmentVariables.DotNetRunningInContainer) == "true",
                Checks = new Dictionary<string, HealthCheckComponent>()
            };

            try
            {
                // Check vector store connectivity
                try
                {
                    var collections = await vectorStore.ListCollectionsAsync(ct);
                    healthStatus.Checks[ApiConstants.HealthComponents.VectorStore] = new HealthCheckComponent 
                    { 
                        Status = ApiConstants.HealthStatus.Healthy, 
                        Collections = collections?.Count() ?? 0 
                    };
                }
                catch (Exception ex)
                {
                    healthStatus.Checks[ApiConstants.HealthComponents.VectorStore] = new HealthCheckComponent 
                    { 
                        Status = ApiConstants.HealthStatus.Unhealthy, 
                        Error = ex.Message 
                    };
                }

                // Check disk space (data directory)
                try
                {
                    var dataPath = ApiConstants.Paths.AppData;
                    if (Directory.Exists(dataPath))
                    {
                        var drive = new DriveInfo(Path.GetPathRoot(dataPath) ?? "/");
                        var freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                        healthStatus.Checks["DiskSpace"] = new HealthCheckComponent
                        { 
                            Status = freeSpaceGB > 1.0 ? "healthy" : "warning",
                            AvailableGB = Math.Round(freeSpaceGB, 2),
                            TotalGB = Math.Round(drive.TotalSize / (1024.0 * 1024.0 * 1024.0), 2)
                        };
                    }
                    else
                    {
                        healthStatus.Checks["DiskSpace"] = new HealthCheckComponent 
                        { 
                            Status = "unknown", 
                            Message = "Data directory not found" 
                        };
                    }
                }
                catch (Exception ex)
                {
                    healthStatus.Checks["DiskSpace"] = new HealthCheckComponent 
                    { 
                        Status = "error", 
                        Error = ex.Message 
                    };
                }

                // Check memory usage
                try
                {
                    var process = System.Diagnostics.Process.GetCurrentProcess();
                    var memoryMB = process.WorkingSet64 / (1024.0 * 1024.0);
                    healthStatus.Checks["Memory"] = new HealthCheckComponent
                    { 
                        Status = memoryMB < 1000 ? "healthy" : "warning",
                        WorkingSetMB = Math.Round(memoryMB, 2)
                    };
                }
                catch (Exception ex)
                {
                    healthStatus.Checks["Memory"] = new HealthCheckComponent 
                    { 
                        Status = "error", 
                        Error = ex.Message 
                    };
                }

                // Determine overall status
                var hasUnhealthyChecks = healthStatus.Checks.Values
                    .Any(check => check.Status == "unhealthy");
                
                if (hasUnhealthyChecks)
                {
                    healthStatus.Status = "degraded";
                    return Results.Json(healthStatus, statusCode: 503);
                }
                
                return Results.Ok(healthStatus);
            }
            catch (Exception ex)
            {
                return Results.Json(new HealthCheckDto
                {
                    Status = "unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Error = ex.Message
                }, statusCode: 503);
            }
        })
        .WithOpenApi(op =>
        {
            op.Summary = "Comprehensive health check";
            op.Description = "Returns detailed health status including vector store, disk space, and memory usage";
            op.Tags = [ new OpenApiTag { Name = "Health" } ];
            return op;
        })
        .Produces<HealthCheckDto>()
        .Produces<HealthCheckDto>(503)
        .WithTags("Health");

        return group;
    }
}