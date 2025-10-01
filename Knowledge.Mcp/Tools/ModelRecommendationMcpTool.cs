using System.ComponentModel;
using System.Text.Json;
using Knowledge.Analytics.Services;
using KnowledgeEngine.Agents.Plugins;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Knowledge.Mcp.Tools;

/// <summary>
/// Model Recommendation MCP Tool - Provides external access to AI model analytics and recommendations
///
/// This tool wraps the existing ModelRecommendationAgent functionality to make model intelligence
/// accessible to external MCP clients like monitoring dashboards, CI/CD pipelines, and developer tools.
///
/// Key Features:
/// - Get most popular models based on actual usage statistics
/// - Detailed performance analysis for specific models
/// - Side-by-side model comparisons
/// - Provider-specific filtering (OpenAI, Anthropic, Google AI, Ollama)
/// - Cost efficiency and optimization recommendations
///
/// Dependencies:
/// - ModelRecommendationAgent: Existing model analytics logic
/// - IUsageTrackingService: Real usage data from SQLite database
/// - Analytics database: Conversation, token, and performance metrics
///
/// Usage Examples:
/// - CI/CD: Check model health before deployment
/// - Monitoring: Track model performance trends
/// - Cost Optimization: Identify expensive vs efficient models
/// - Developer Tools: Get model recommendations for specific tasks
/// </summary>
[McpServerToolType]
public sealed class ModelRecommendationMcpTool
{
    /// <summary>
    /// Get the most popular AI models based on actual usage statistics and performance metrics.
    ///
    /// This method provides data-driven model recommendations based on real system usage,
    /// helping users choose models that are proven to work well in this environment.
    ///
    /// Popularity Ranking Factors:
    /// 1. Conversation count (primary factor)
    /// 2. Total token usage (secondary factor)
    /// 3. Success rate (tie-breaker)
    /// 4. Average response time (performance consideration)
    ///
    /// Use Cases:
    /// - New users wanting model recommendations
    /// - Performance optimization decisions
    /// - Cost analysis and budgeting
    /// - Model migration planning
    /// </summary>
    /// <param name="count">Number of top models to return (1-20)</param>
    /// <param name="period">Time period filter (daily, weekly, monthly, all-time)</param>
    /// <param name="provider">Filter by specific provider or 'all' for comprehensive view</param>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <returns>Ranked list of popular models with usage statistics</returns>
    [McpServerTool]
    [Description(
        "Get most popular AI models based on actual usage statistics and performance metrics"
    )]
    public static async Task<string> GetPopularModelsAsync(
        [Description("Service provider for dependency injection")] IServiceProvider serviceProvider,
        [Description("Number of top models to return (default: 3, max: 20)")] int count = 3,
        [Description("Time period filter: daily, weekly, monthly, all-time (default: monthly)")]
            string period = "monthly",
        [Description(
            "Filter by provider: OpenAI, Anthropic, Google, Ollama, or all (default: all)"
        )]
            string provider = "all"
    )
    {
        try
        {
            // Validate parameters
            if (count < 1 || count > 20)
            {
                return CreateErrorResponse(
                    "Count must be between 1 and 20",
                    nameof(GetPopularModelsAsync)
                );
            }

            var validPeriods = new[] { "daily", "weekly", "monthly", "all-time" };
            if (!validPeriods.Contains(period.ToLowerInvariant()))
            {
                return CreateErrorResponse(
                    $"Period must be one of: {string.Join(", ", validPeriods)}",
                    nameof(GetPopularModelsAsync)
                );
            }

            // Resolve services from DI container
            var usageTrackingService = serviceProvider.GetRequiredService<IUsageTrackingService>();

            // Create model recommendation agent with real usage data
            var modelAgent = new ModelRecommendationAgent(usageTrackingService);

            // Execute model popularity analysis
            var results = await modelAgent.GetPopularModelsAsync(count, period, provider);

            // Check if the response indicates no data and provide helpful guidance
            if (results.Contains("No model usage data") || results.Contains("no model usage data"))
            {
                var helpfulResponse = new
                {
                    Status = "No Usage Data",
                    Message = "No model usage statistics found in the system",
                    Explanation = "Model analytics require actual model usage to generate statistics. You need to use the ChatComplete system with different AI models first.",
                    HowToGenerateData = new[]
                    {
                        "Use the main ChatComplete API with different models (OpenAI, Anthropic, Google AI, Ollama)",
                        "Send chat requests with various providers to generate usage statistics",
                        "Try different models within each provider to build comparison data",
                        "Usage data is automatically tracked and stored in the SQLite database"
                    },
                    NextSteps = new[]
                    {
                        "Make some chat requests using: POST /api/chat with different 'provider' values",
                        "Wait for usage data to be recorded (happens automatically)",
                        "Run this command again to see model statistics"
                    },
                    Timestamp = DateTime.UtcNow
                };
                
                return JsonSerializer.Serialize(helpfulResponse, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            Console.WriteLine(
                $"🏆 MCP ModelRecommendation completed - Provider: {provider}, Count: {count}"
            );
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MCP ModelRecommendation error: {ex.Message}");
            return CreateErrorResponse(
                $"Failed to get popular models: {ex.Message}",
                nameof(GetPopularModelsAsync)
            );
        }
    }

    /// <summary>
    /// Get detailed performance analysis for a specific model.
    ///
    /// Provides comprehensive analytics for a single model including success rates,
    /// response times, usage patterns, and performance trends over time.
    ///
    /// Performance Metrics:
    /// - Success rate percentage
    /// - Average response time
    /// - Total conversations and tokens
    /// - Error patterns and failure modes
    /// - Usage trends and patterns
    ///
    /// Use Cases:
    /// - Debug model performance issues
    /// - Validate model reliability before scaling
    /// - Performance optimization analysis
    /// - SLA compliance monitoring
    /// </summary>
    /// <param name="modelName">Name of the model to analyze (e.g., "gpt-4", "claude-sonnet-4")</param>
    /// <param name="provider">Optional provider filter for models with same names across providers</param>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <returns>Detailed performance analysis and metrics</returns>
    [McpServerTool]
    [Description(
        "Get detailed performance analysis for a specific model including success rates and response times"
    )]
    public static async Task<string> GetModelPerformanceAnalysisAsync(
        [Description("Name of the model to analyze (e.g., 'gpt-4', 'claude-sonnet-4')")]
            string modelName,
        [Description("Service provider for dependency injection")] IServiceProvider serviceProvider,
        [Description("Optional provider filter (OpenAI, Anthropic, Google, Ollama)")]
            string? provider = null
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(modelName))
            {
                return CreateErrorResponse(
                    "Model name is required",
                    nameof(GetModelPerformanceAnalysisAsync)
                );
            }

            // Resolve services from DI container
            var usageTrackingService = serviceProvider.GetRequiredService<IUsageTrackingService>();

            // Create model recommendation agent
            var modelAgent = new ModelRecommendationAgent(usageTrackingService);

            // Execute performance analysis
            var results = await modelAgent.GetModelPerformanceAnalysisAsync(modelName, provider);

            Console.WriteLine($"📊 MCP ModelPerformanceAnalysis completed - Model: {modelName}");
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MCP ModelPerformanceAnalysis error: {ex.Message}");
            return CreateErrorResponse(
                $"Failed to analyze model performance: {ex.Message}",
                nameof(GetModelPerformanceAnalysisAsync)
            );
        }
    }

    /// <summary>
    /// Compare multiple models side by side based on performance metrics and usage patterns.
    ///
    /// Provides comprehensive comparison across multiple models to help with decision making
    /// for model selection, migration planning, and optimization initiatives.
    ///
    /// Comparison Dimensions:
    /// - Performance: Response time, success rate, reliability
    /// - Usage: Conversation volume, token consumption, user adoption
    /// - Efficiency: Cost per token, resource utilization
    /// - Quality: User satisfaction, task completion rates
    ///
    /// Use Cases:
    /// - Model migration planning
    /// - A/B testing analysis
    /// - Cost optimization decisions
    /// - Provider evaluation
    /// </summary>
    /// <param name="modelNames">Comma-separated list of model names to compare</param>
    /// <param name="focus">Comparison focus area: performance, usage, efficiency, or all</param>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <returns>Side-by-side model comparison with recommendations</returns>
    [McpServerTool]
    [Description(
        "Compare multiple models side by side based on performance metrics and usage patterns"
    )]
    public static async Task<string> CompareModelsAsync(
        [Description(
            "Comma-separated list of model names to compare (e.g., 'gpt-4,claude-sonnet-4,gemini-1.5-pro')"
        )]
            string modelNames,
        [Description("Service provider for dependency injection")] IServiceProvider serviceProvider,
        [Description("Comparison focus: performance, usage, efficiency, or all (default: all)")]
            string focus = "all"
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(modelNames))
            {
                return CreateErrorResponse("Model names are required", nameof(CompareModelsAsync));
            }

            var validFocusAreas = new[] { "performance", "usage", "efficiency", "all" };
            if (!validFocusAreas.Contains(focus.ToLowerInvariant()))
            {
                return CreateErrorResponse(
                    $"Focus must be one of: {string.Join(", ", validFocusAreas)}",
                    nameof(CompareModelsAsync)
                );
            }

            // Resolve services from DI container
            var usageTrackingService = serviceProvider.GetRequiredService<IUsageTrackingService>();

            // Create model recommendation agent
            var modelAgent = new ModelRecommendationAgent(usageTrackingService);

            // Execute model comparison
            var results = await modelAgent.CompareModelsAsync(modelNames, focus);

            Console.WriteLine($"⚖️ MCP ModelComparison completed - Models: {modelNames}");
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MCP ModelComparison error: {ex.Message}");
            return CreateErrorResponse(
                $"Failed to compare models: {ex.Message}",
                nameof(CompareModelsAsync)
            );
        }
    }

    /// <summary>
    /// Creates structured error responses for MCP clients with helpful guidance.
    /// </summary>
    /// <param name="errorMessage">Descriptive error message</param>
    /// <param name="toolName">Name of the tool that encountered the error</param>
    /// <returns>JSON-formatted error response</returns>
    private static string CreateErrorResponse(string errorMessage, string toolName)
    {
        var errorResponse = new
        {
            Error = true,
            Tool = toolName,
            Message = errorMessage,
            Timestamp = DateTime.UtcNow,
            Suggestions = new[]
            {
                "Check if model usage data exists in the system",
                "Verify model names are correct and exist in the system",
                "Ensure the analytics database is accessible",
                "Try with different parameter values or time periods",
            },
        };

        return JsonSerializer.Serialize(
            errorResponse,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }
        );
    }
}
