using Knowledge.Analytics.Models;
using Knowledge.Analytics.Services;
using Knowledge.Contracts.Types;
using Knowledge.Data.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Knowledge.Api.Endpoints;

/// <summary>
/// Analytics endpoints for usage tracking and model performance monitoring.
/// </summary>
public static class AnalyticsEndpoints
{
    /// <summary>
    /// Maps analytics-related endpoints to the application.
    /// </summary>
    public static RouteGroupBuilder MapAnalyticsEndpoints(this RouteGroupBuilder group)
    {
        // GET /api/analytics/models
        group
            .MapGet(
                "/models",
                async (
                    [FromServices] ICachedAnalyticsService cachedAnalyticsService,
                    CancellationToken ct
                ) =>
                {
                    var modelStats = await cachedAnalyticsService.GetModelUsageStatsAsync(ct);
                    return Results.Ok(modelStats);
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get model usage statistics";
                op.Description = "Returns aggregated usage statistics for all AI models including token usage, response times, and success rates.";
                return op;
            })
            .Produces<IEnumerable<ModelUsageStats>>()
            .WithTags("Analytics");

        // GET /api/analytics/conversations
        group
            .MapGet(
                "/conversations",
                async (
                    [FromQuery] string? knowledgeId,
                    [FromQuery] AiProvider? provider,
                    [FromQuery] int days,
                    [FromServices] ICachedAnalyticsService cachedAnalyticsService,
                    CancellationToken ct
                ) =>
                {
                    days = Math.Max(1, Math.Min(days == 0 ? 30 : days, 365)); // Default 30 days, max 365
                    var usageHistory = await cachedAnalyticsService.GetUsageHistoryAsync(days, ct);

                    // Filter by knowledgeId if provided
                    if (!string.IsNullOrEmpty(knowledgeId))
                    {
                        usageHistory = usageHistory.Where(u => u.KnowledgeId == knowledgeId);
                    }

                    // Filter by provider if provided
                    if (provider.HasValue)
                    {
                        usageHistory = usageHistory.Where(u => u.Provider == provider.Value);
                    }

                    return Results.Ok(usageHistory);
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get conversation usage analytics";
                op.Description = "Returns detailed usage metrics for conversations with optional filtering by knowledge base, provider, and time period.";
                
                // Add parameter descriptions
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "knowledgeId",
                    In = ParameterLocation.Query,
                    Description = "Filter by specific knowledge base ID",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "string" }
                });
                
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "provider",
                    In = ParameterLocation.Query,
                    Description = "Filter by AI provider (OpenAi, Google, Anthropic, Ollama)",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "string", Enum = new List<IOpenApiAny>
                    {
                        new OpenApiString("OpenAi"),
                        new OpenApiString("Google"), 
                        new OpenApiString("Anthropic"),
                        new OpenApiString("Ollama")
                    }}
                });
                
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "days",
                    In = ParameterLocation.Query,
                    Description = "Number of days to look back (1-365, default: 30)",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "integer", Minimum = 1, Maximum = 365, Default = new OpenApiInteger(30) }
                });
                
                return op;
            })
            .Produces<IEnumerable<UsageMetric>>()
            .WithTags("Analytics");

        // GET /api/analytics/knowledge-bases
        group
            .MapGet(
                "/knowledge-bases",
                async (
                    [FromServices] ICachedAnalyticsService cachedAnalyticsService,
                    CancellationToken ct
                ) =>
                {
                    var knowledgeStats = await cachedAnalyticsService.GetKnowledgeUsageStatsAsync(ct);
                    return Results.Ok(knowledgeStats);
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get knowledge base usage statistics";
                op.Description = "Returns usage statistics for all knowledge bases including document counts, chunk counts, and query activity.";
                return op;
            })
            .Produces<IEnumerable<KnowledgeUsageStats>>()
            .WithTags("Analytics");

        // GET /api/analytics/usage-trends
        group
            .MapGet(
                "/usage-trends",
                async (
                    [FromQuery] int days,
                    [FromQuery] AiProvider? provider,
                    [FromServices] ICachedAnalyticsService cachedAnalyticsService,
                    CancellationToken ct
                ) =>
                {
                    days = Math.Max(1, Math.Min(days == 0 ? 30 : days, 365)); // Default 30 days, max 365
                    var usageHistory = await cachedAnalyticsService.GetUsageHistoryAsync(days, ct);

                    // Filter by provider if provided
                    if (provider.HasValue)
                    {
                        usageHistory = usageHistory.Where(u => u.Provider == provider.Value);
                    }

                    // Group by date and aggregate metrics
                    var trends = usageHistory
                        .GroupBy(u => u.Timestamp.Date)
                        .Select(g => new
                        {
                            Date = g.Key,
                            TotalRequests = g.Count(),
                            SuccessfulRequests = g.Count(u => u.WasSuccessful),
                            TotalTokens = g.Sum(u => u.TotalTokens),
                            AverageResponseTime = g.Average(u => u.ResponseTime.TotalMilliseconds),
                            UniqueConversations = g.Select(u => u.ConversationId).Distinct().Count(),
                            ProviderBreakdown = g.GroupBy(u => u.Provider)
                                .ToDictionary(pg => pg.Key.ToString(), pg => pg.Count())
                        })
                        .OrderBy(t => t.Date)
                        .ToList();

                    return Results.Ok(trends);
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get usage trends over time";
                op.Description = "Returns time-series usage trends with daily aggregations including request counts, token usage, and provider breakdowns.";
                
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "days",
                    In = ParameterLocation.Query,
                    Description = "Number of days to analyze (1-365, default: 30)",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "integer", Minimum = 1, Maximum = 365, Default = new OpenApiInteger(30) }
                });
                
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "provider",
                    In = ParameterLocation.Query,
                    Description = "Filter by specific AI provider",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "string", Enum = new List<IOpenApiAny>
                    {
                        new OpenApiString("OpenAi"),
                        new OpenApiString("Google"), 
                        new OpenApiString("Anthropic"),
                        new OpenApiString("Ollama")
                    }}
                });
                
                return op;
            })
            .WithTags("Analytics");

        // GET /api/analytics/conversation/{conversationId}
        group
            .MapGet(
                "/conversation/{conversationId}",
                async (
                    [FromRoute] string conversationId,
                    [FromServices] ICachedAnalyticsService cachedAnalyticsService,
                    CancellationToken ct
                ) =>
                {
                    var usageMetric = await cachedAnalyticsService.GetUsageByConversationAsync(conversationId, ct);
                    if (usageMetric == null)
                    {
                        return Results.NotFound(new { error = "Conversation not found or no usage data available" });
                    }
                    return Results.Ok(usageMetric);
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get usage data for specific conversation";
                op.Description = "Returns detailed usage metrics for a specific conversation ID.";
                return op;
            })
            .Produces<UsageMetric>()
            .Produces(404)
            .WithTags("Analytics");

        // GET /api/analytics/providers/accounts
        group
            .MapGet(
                "/providers/accounts",
                async (
                    [FromServices] ICachedProviderAggregationService cachedAggregationService,
                    CancellationToken ct
                ) =>
                {
                    var accountInfos = await cachedAggregationService.GetAllAccountInfoAsync(ct);
                    return Results.Ok(accountInfos);
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get account information for all configured providers";
                op.Description = "Returns billing and account details from configured external AI providers (OpenAI, Anthropic, Google AI).";
                return op;
            })
            .Produces<List<ProviderAccountInfo>>()
            .WithTags("Analytics");

        // GET /api/analytics/providers/usage
        group
            .MapGet(
                "/providers/usage",
                async (
                    [FromQuery] int days,
                    [FromServices] ICachedProviderAggregationService cachedAggregationService,
                    CancellationToken ct
                ) =>
                {
                    days = Math.Max(1, Math.Min(days == 0 ? 30 : days, 365)); // Default 30 days, max 365
                    var usageInfos = await cachedAggregationService.GetAllUsageInfoAsync(days, ct);
                    return Results.Ok(usageInfos);
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get usage information from external providers";
                op.Description = "Returns usage metrics and costs from configured external AI providers with breakdown by model.";
                
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "days",
                    In = ParameterLocation.Query,
                    Description = "Number of days to look back (1-365, default: 30)",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "integer", Minimum = 1, Maximum = 365, Default = new OpenApiInteger(30) }
                });
                
                return op;
            })
            .Produces<List<ProviderUsageInfo>>()
            .WithTags("Analytics");

        // GET /api/analytics/providers/summary
        group
            .MapGet(
                "/providers/summary",
                async (
                    [FromQuery] int days,
                    [FromServices] ICachedProviderAggregationService cachedAggregationService,
                    CancellationToken ct
                ) =>
                {
                    days = Math.Max(1, Math.Min(days == 0 ? 30 : days, 365)); // Default 30 days, max 365
                    var summary = await cachedAggregationService.GetProviderSummaryAsync(days, ct);
                    return Results.Ok(summary);
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get comprehensive provider summary";
                op.Description = "Returns aggregated summary of all providers including configuration status, total costs, and usage statistics.";
                
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "days",
                    In = ParameterLocation.Query,
                    Description = "Number of days to analyze (1-365, default: 30)",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "integer", Minimum = 1, Maximum = 365, Default = new OpenApiInteger(30) }
                });
                
                return op;
            })
            .Produces<ProviderSummary>()
            .WithTags("Analytics");

        // GET /api/analytics/providers/status
        group
            .MapGet(
                "/providers/status",
                async ([FromServices] ICachedProviderAggregationService cachedAggregationService, CancellationToken ct) =>
                {
                    var configured = await cachedAggregationService.GetConfiguredProvidersAsync(ct);
                    var unconfigured = await cachedAggregationService.GetUnconfiguredProvidersAsync(ct);
                    
                    return Results.Ok(new
                    {
                        TotalProviders = configured.Count + unconfigured.Count,
                        ConfiguredProviders = configured,
                        UnconfiguredProviders = unconfigured,
                        ConfigurationStatus = configured.ToDictionary(p => p, _ => "configured")
                            .Concat(unconfigured.ToDictionary(p => p, _ => "not configured"))
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    });
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get provider configuration status";
                op.Description = "Returns the configuration status of all available external AI providers.";
                return op;
            })
            .WithTags("Analytics");

        // GET /api/analytics/cost-breakdown
        group
            .MapGet(
                "/cost-breakdown",
                async (
                    [FromQuery] int days,
                    [FromServices] IProviderUsageRepository providerUsageRepo,
                    CancellationToken ct
                ) =>
                {
                    days = Math.Max(1, Math.Min(days == 0 ? 30 : days, 365)); // Default 30 days, max 365
                    var endDate = DateTime.UtcNow.Date;
                    var startDate = endDate.AddDays(-days);
                    
                    var costBreakdown = await providerUsageRepo.GetCostBreakdownAsync(startDate, endDate, ct);
                    
                    var results = costBreakdown.Select(cb => new
                    {
                        Provider = cb.Provider,
                        TotalCost = cb.TotalCost,
                        Period = new { StartDate = startDate, EndDate = endDate, Days = days }
                    });

                    return Results.Ok(results);
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get cost breakdown by provider";
                op.Description = "Returns total costs spent per provider for the specified time period.";
                
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "days",
                    In = ParameterLocation.Query,
                    Description = "Number of days to analyze (1-365, default: 30)",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "integer", Minimum = 1, Maximum = 365, Default = new OpenApiInteger(30) }
                });
                
                return op;
            })
            .WithTags("Analytics");

        // GET /api/analytics/knowledge-correlation
        group
            .MapGet(
                "/knowledge-correlation",
                async (
                    [FromQuery] string? knowledgeId,
                    [FromQuery] int days,
                    [FromServices] ICachedAnalyticsService cachedAnalyticsService,
                    CancellationToken ct
                ) =>
                {
                    days = Math.Max(1, Math.Min(days == 0 ? 30 : days, 365)); // Default 30 days, max 365
                    var usageHistory = await cachedAnalyticsService.GetUsageHistoryAsync(days, ct);

                    // Filter by knowledge ID if provided
                    var filteredUsage = string.IsNullOrEmpty(knowledgeId) 
                        ? usageHistory 
                        : usageHistory.Where(u => u.KnowledgeId == knowledgeId);

                    // Group by knowledge base and model to show correlations
                    var correlations = filteredUsage
                        .Where(u => !string.IsNullOrEmpty(u.KnowledgeId))
                        .GroupBy(u => new { u.KnowledgeId, u.ModelName, u.Provider })
                        .Select(g => new
                        {
                            KnowledgeId = g.Key.KnowledgeId,
                            ModelName = g.Key.ModelName,
                            Provider = g.Key.Provider,
                            ConversationCount = g.Select(u => u.ConversationId).Distinct().Count(),
                            TotalTokens = g.Sum(u => u.TotalTokens),
                            AverageResponseTime = g.Average(u => u.ResponseTime.TotalMilliseconds),
                            SuccessRate = g.Count(u => u.WasSuccessful) / (double)g.Count() * 100,
                            LastUsed = g.Max(u => u.Timestamp),
                            UsageFrequency = g.Count()
                        })
                        .OrderByDescending(c => c.ConversationCount)
                        .ToList();

                    return Results.Ok(new
                    {
                        Period = new { Days = days, EndDate = DateTime.UtcNow.Date },
                        TotalKnowledgeBases = correlations.Select(c => c.KnowledgeId).Distinct().Count(),
                        TotalModels = correlations.Select(c => new { c.ModelName, c.Provider }).Distinct().Count(),
                        Correlations = correlations
                    });
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get knowledge base and model correlation analytics";
                op.Description = "Returns detailed analytics showing which models are used with which knowledge bases, including usage patterns and performance metrics.";
                
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "knowledgeId",
                    In = ParameterLocation.Query,
                    Description = "Filter by specific knowledge base ID (optional)",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "string" }
                });
                
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "days",
                    In = ParameterLocation.Query,
                    Description = "Number of days to analyze (1-365, default: 30)",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "integer", Minimum = 1, Maximum = 365, Default = new OpenApiInteger(30) }
                });
                
                return op;
            })
            .WithTags("Analytics");

        // GET /api/analytics/sync/status
        group
            .MapGet(
                "/sync/status",
                ([FromServices] IBackgroundSyncService backgroundSyncService) =>
                {
                    var status = backgroundSyncService.GetSyncStatus();
                    return Results.Ok(status);
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get background sync service status";
                op.Description = "Returns the current status of the background sync service including last sync times, next sync schedule, and sync statistics.";
                return op;
            })
            .Produces<BackgroundSyncStatus>()
            .WithTags("Analytics");

        // POST /api/analytics/sync/trigger
        group
            .MapPost(
                "/sync/trigger",
                async (
                    [FromQuery] string? type,
                    [FromServices] IBackgroundSyncService backgroundSyncService,
                    CancellationToken ct
                ) =>
                {
                    try
                    {
                        switch (type?.ToLower())
                        {
                            case "accounts":
                                await backgroundSyncService.SyncProviderAccountsAsync(ct);
                                return Results.Ok(new { message = "Account sync triggered successfully", type = "accounts" });
                            case "usage":
                                await backgroundSyncService.SyncProviderUsageAsync(ct);
                                return Results.Ok(new { message = "Usage sync triggered successfully", type = "usage" });
                            case "all":
                            case null:
                                await backgroundSyncService.SyncAllProvidersAsync(ct);
                                return Results.Ok(new { message = "Full sync triggered successfully", type = "all" });
                            default:
                                return Results.BadRequest(new { error = "Invalid sync type. Use 'accounts', 'usage', or 'all'" });
                        }
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem(
                            title: "Sync Failed",
                            detail: ex.Message,
                            statusCode: 500
                        );
                    }
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Manually trigger background sync";
                op.Description = "Triggers an immediate sync of provider data. Use 'type' parameter to specify 'accounts', 'usage', or 'all'.";
                
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "type",
                    In = ParameterLocation.Query,
                    Description = "Type of sync to trigger: 'accounts', 'usage', or 'all' (default)",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "string", Enum = new List<IOpenApiAny>
                    {
                        new OpenApiString("accounts"),
                        new OpenApiString("usage"),
                        new OpenApiString("all")
                    }}
                });
                
                return op;
            })
            .WithTags("Analytics");

        // GET /api/analytics/ollama/usage - Get comprehensive Ollama usage info
        group
            .MapGet(
                "/ollama/usage",
                async (
                    [FromQuery] int days,
                    [FromServices] IOllamaAnalyticsService ollamaService,
                    CancellationToken ct
                ) =>
                {
                    days = Math.Max(1, Math.Min(days == 0 ? 30 : days, 365)); // Default 30 days, max 365
                    var usageInfo = await ollamaService.GetUsageInfoAsync(days, ct);
                    return Results.Ok(usageInfo);
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get comprehensive Ollama usage information";
                op.Description = "Returns detailed usage metrics, model statistics, and resource usage for local Ollama models.";
                
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "days",
                    In = ParameterLocation.Query,
                    Description = "Number of days to analyze (1-365, default: 30)",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "integer", Minimum = 1, Maximum = 365, Default = new OpenApiInteger(30) }
                });
                
                return op;
            })
            .WithTags("Analytics", "Ollama");

        // GET /api/analytics/ollama/models - Get Ollama model inventory
        group
            .MapGet(
                "/ollama/models",
                async (
                    [FromServices] IOllamaAnalyticsService ollamaService,
                    CancellationToken ct
                ) =>
                {
                    var inventory = await ollamaService.GetModelInventoryAsync(ct);
                    return Results.Ok(inventory);
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get Ollama model inventory";
                op.Description = "Returns detailed inventory of installed Ollama models including sizes, families, and capabilities.";
                return op;
            })
            .WithTags("Analytics", "Ollama");

        // GET /api/analytics/ollama/downloads - Get Ollama download statistics
        group
            .MapGet(
                "/ollama/downloads",
                async (
                    [FromQuery] int days,
                    [FromServices] IOllamaAnalyticsService ollamaService,
                    CancellationToken ct
                ) =>
                {
                    days = Math.Max(1, Math.Min(days == 0 ? 30 : days, 365)); // Default 30 days, max 365
                    var downloadStats = await ollamaService.GetDownloadStatsAsync(days, ct);
                    return Results.Ok(downloadStats);
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get Ollama download statistics";
                op.Description = "Returns download activity, success rates, and recent download history.";
                
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "days",
                    In = ParameterLocation.Query,
                    Description = "Number of days to analyze (1-365, default: 30)",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "integer", Minimum = 1, Maximum = 365, Default = new OpenApiInteger(30) }
                });
                
                return op;
            })
            .WithTags("Analytics", "Ollama");

        // GET /api/analytics/ollama/performance - Get Ollama model performance metrics
        group
            .MapGet(
                "/ollama/performance",
                async (
                    [FromQuery] int days,
                    [FromServices] IOllamaAnalyticsService ollamaService,
                    CancellationToken ct
                ) =>
                {
                    days = Math.Max(1, Math.Min(days == 0 ? 30 : days, 365)); // Default 30 days, max 365
                    var performance = await ollamaService.GetModelPerformanceAsync(days, ct);
                    return Results.Ok(performance);
                }
            )
            .WithOpenApi(op =>
            {
                op.Summary = "Get Ollama model performance metrics";
                op.Description = "Returns performance statistics by model including response times, success rates, and token throughput.";
                
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "days",
                    In = ParameterLocation.Query,
                    Description = "Number of days to analyze (1-365, default: 30)",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "integer", Minimum = 1, Maximum = 365, Default = new OpenApiInteger(30) }
                });
                
                return op;
            })
            .WithTags("Analytics", "Ollama");

        return group;
    }
}