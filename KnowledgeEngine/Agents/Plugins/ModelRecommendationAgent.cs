using System.ComponentModel;
using System.Text;
using Knowledge.Analytics.Services;
using Knowledge.Contracts.Types;
using Microsoft.SemanticKernel;

namespace KnowledgeEngine.Agents.Plugins;

/// <summary>
/// Agent plugin for providing model recommendations based on usage statistics and performance metrics.
/// </summary>
public sealed class ModelRecommendationAgent
{
    private readonly IUsageTrackingService _usageTrackingService;

    public ModelRecommendationAgent(IUsageTrackingService usageTrackingService)
    {
        _usageTrackingService = usageTrackingService;
    }

    [KernelFunction]
    [Description(
        "Get the most popular AI models currently in use on this system based on actual usage statistics and performance metrics. "
            + "Use this when users ask about 'popular models', 'most used models', 'best models', 'recommended models', or 'models on the system'."
    )]
    public async Task<string> GetPopularModelsAsync(
        [Description("Number of top models to return")] int count = 3,
        [Description("Time period filter: daily, weekly, monthly, all-time")]
            string period = "monthly",
        [Description("Filter by provider: OpenAI, Anthropic, Google, Ollama, or all")]
            string provider = "all"
    )
    {
        Console.WriteLine(
            $"🏆 ModelRecommendationAgent.GetPopularModelsAsync called - count: {count}, period: {period}, provider: {provider}"
        );

        try
        {
            // Get model usage statistics
            var modelStats = await _usageTrackingService.GetModelUsageStatsAsync();

            if (!modelStats.Any())
            {
                return "No model usage data is currently available. Please use the system to generate some usage statistics.";
            }

            // Filter by provider if specified
            if (!string.Equals(provider, "all", StringComparison.OrdinalIgnoreCase))
            {
                if (Enum.TryParse<AiProvider>(provider, true, out var providerEnum))
                {
                    modelStats = modelStats.Where(m => m.Provider == providerEnum);
                }
                else
                {
                    return $"Invalid provider '{provider}'. Valid options are: OpenAI, Anthropic, Google, Ollama, or all.";
                }
            }

            if (!modelStats.Any())
            {
                return provider == "all"
                    ? "No model usage data found for any provider."
                    : $"No model usage data found for {provider} provider.";
            }

            // Apply period filtering (for future enhancement - currently showing all-time)
            // TODO: Implement period filtering when time-based filtering is added to usage tracking

            // Sort by popularity metrics (conversation count, then total tokens)
            var topModels = modelStats
                .OrderByDescending(m => m.ConversationCount)
                .ThenByDescending(m => m.TotalTokens)
                .ThenByDescending(m => m.SuccessRate)
                .Take(count)
                .ToList();

            return FormatModelRecommendations(topModels, count, period, provider);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ModelRecommendationAgent error: {ex.Message}");
            return $"Error retrieving model recommendations: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description(
        "Get detailed performance analysis for a specific model, including success rates, response times, and usage patterns."
    )]
    public async Task<string> GetModelPerformanceAnalysisAsync(
        [Description("Name of the model to analyze")] string modelName,
        [Description("Provider of the model (optional)")] string? provider = null
    )
    {
        Console.WriteLine(
            $"📊 ModelRecommendationAgent.GetModelPerformanceAnalysisAsync called - model: {modelName}, provider: {provider}"
        );

        try
        {
            var modelStats = await _usageTrackingService.GetModelUsageStatsAsync();

            // Filter by model name and optionally by provider
            var targetModels = modelStats.Where(m =>
                string.Equals(m.ModelName, modelName, StringComparison.OrdinalIgnoreCase)
            );

            if (
                !string.IsNullOrEmpty(provider)
                && Enum.TryParse<AiProvider>(provider, true, out var providerEnum)
            )
            {
                targetModels = targetModels.Where(m => m.Provider == providerEnum);
            }

            var modelList = targetModels.ToList();

            if (!modelList.Any())
            {
                return $"No performance data found for model '{modelName}'"
                    + (!string.IsNullOrEmpty(provider) ? $" from {provider}" : "");
            }

            return FormatModelPerformanceAnalysis(modelList);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"❌ ModelRecommendationAgent performance analysis error: {ex.Message}"
            );
            return $"Error analyzing model performance: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description(
        "Compare multiple models side by side based on performance metrics, cost efficiency, and usage patterns."
    )]
    public async Task<string> CompareModelsAsync(
        [Description("Comma-separated list of model names to compare")] string modelNames,
        [Description("Comparison focus: performance, usage, efficiency, or all")]
            string focus = "all"
    )
    {
        Console.WriteLine(
            $"⚖️ ModelRecommendationAgent.CompareModelsAsync called - models: {modelNames}, focus: {focus}"
        );

        try
        {
            var modelsToCompare = modelNames
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(name => name.Trim())
                .ToList();

            if (modelsToCompare.Count < 2)
            {
                return "Please provide at least 2 model names to compare, separated by commas.";
            }

            var modelStats = await _usageTrackingService.GetModelUsageStatsAsync();
            var comparisonData =
                new List<(string ModelName, Knowledge.Analytics.Models.ModelUsageStats Stats)>();

            foreach (var modelName in modelsToCompare)
            {
                var modelStat = modelStats.FirstOrDefault(m =>
                    string.Equals(m.ModelName, modelName, StringComparison.OrdinalIgnoreCase)
                );

                if (modelStat != null)
                {
                    comparisonData.Add((modelName, modelStat));
                }
            }

            if (comparisonData.Count < 2)
            {
                return $"Found data for only {comparisonData.Count} model(s). Need at least 2 models with usage data for comparison.";
            }

            return FormatModelComparison(comparisonData, focus);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ModelRecommendationAgent comparison error: {ex.Message}");
            return $"Error comparing models: {ex.Message}";
        }
    }

    private static string FormatModelRecommendations(
        IList<Knowledge.Analytics.Models.ModelUsageStats> models,
        int requestedCount,
        string period,
        string provider
    )
    {
        var response = new StringBuilder();

        response.AppendLine(
            $"🏆 **Top {models.Count} Most Popular Models** ({period}, {provider})"
        );
        response.AppendLine();

        for (int i = 0; i < models.Count; i++)
        {
            var model = models[i];
            var rank = i + 1;
            var medal = rank switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => $"{rank}.",
            };

            response.AppendLine($"{medal} **{model.ModelName}** ({model.Provider})");
            response.AppendLine(
                $"   • **{model.ConversationCount:N0} conversations** used this model"
            );
            response.AppendLine($"   • **{model.TotalTokens:N0} total tokens** processed");
            response.AppendLine(
                $"   • **{model.SuccessRate:F1}% success rate** ({model.SuccessfulRequests}/{model.TotalRequests} requests)"
            );
            response.AppendLine(
                $"   • **{model.AverageResponseTime.TotalSeconds:F1}s avg response time**"
            );
            response.AppendLine($"   • **Last used:** {GetRelativeTimeString(model.LastUsed)}");

            if (model.SupportsTools.HasValue)
            {
                response.AppendLine(
                    $"   • **Tool support:** {(model.SupportsTools.Value ? "✅ Yes" : "❌ No")}"
                );
            }

            response.AppendLine();
        }

        // Add recommendation summary
        if (models.Count > 0)
        {
            var topModel = models[0];
            response.AppendLine("💡 **Recommendation:**");
            response.AppendLine(
                $"**{topModel.ModelName}** is currently the most popular choice with {topModel.ConversationCount} conversations and a {topModel.SuccessRate:F1}% success rate."
            );

            if (models.Count > 1)
            {
                var secondModel = models[1];
                if (topModel.SuccessRate < secondModel.SuccessRate)
                {
                    response.AppendLine(
                        $"However, **{secondModel.ModelName}** has a higher success rate ({secondModel.SuccessRate:F1}%) if reliability is your priority."
                    );
                }
            }
        }

        return response.ToString();
    }

    private static string FormatModelPerformanceAnalysis(
        IList<Knowledge.Analytics.Models.ModelUsageStats> models
    )
    {
        var response = new StringBuilder();

        if (models.Count == 1)
        {
            var model = models[0];
            response.AppendLine(
                $"📊 **Performance Analysis: {model.ModelName}** ({model.Provider})"
            );
            response.AppendLine();

            response.AppendLine("**📈 Usage Statistics:**");
            response.AppendLine($"• Total Conversations: **{model.ConversationCount:N0}**");
            response.AppendLine($"• Total Requests: **{model.TotalRequests:N0}**");
            response.AppendLine(
                $"• Success Rate: **{model.SuccessRate:F1}%** ({model.SuccessfulRequests} successful, {model.FailedRequests} failed)"
            );
            response.AppendLine();

            response.AppendLine("**⚡ Performance Metrics:**");
            response.AppendLine(
                $"• Average Response Time: **{model.AverageResponseTime.TotalSeconds:F2} seconds**"
            );
            response.AppendLine($"• Total Tokens Processed: **{model.TotalTokens:N0}**");
            response.AppendLine(
                $"• Average Tokens per Request: **{model.AverageTokensPerRequest:N0}**"
            );
            response.AppendLine();

            response.AppendLine("**🔧 Capabilities:**");
            response.AppendLine(
                $"• Tool Support: **{(model.SupportsTools?.ToString() ?? "Unknown")}**"
            );
            response.AppendLine($"• Last Used: **{GetRelativeTimeString(model.LastUsed)}**");
            response.AppendLine();

            // Performance assessment
            var performanceGrade = model.SuccessRate switch
            {
                >= 95 => "🟢 Excellent",
                >= 90 => "🟡 Good",
                >= 80 => "🟠 Fair",
                _ => "🔴 Poor",
            };

            response.AppendLine($"**📋 Overall Assessment: {performanceGrade}**");

            if (model.SuccessRate >= 95)
            {
                response.AppendLine(
                    "This model demonstrates excellent reliability and performance."
                );
            }
            else if (model.FailedRequests > 0)
            {
                response.AppendLine(
                    $"Consider investigating the {model.FailedRequests} failed requests to improve reliability."
                );
            }
        }
        else
        {
            // Multiple models with same name from different providers
            response.AppendLine(
                $"📊 **Performance Analysis: {models[0].ModelName}** (Multiple Providers)"
            );
            response.AppendLine();

            foreach (var model in models)
            {
                response.AppendLine($"**{model.Provider} Provider:**");
                response.AppendLine(
                    $"• {model.ConversationCount:N0} conversations, {model.SuccessRate:F1}% success rate"
                );
                response.AppendLine(
                    $"• {model.AverageResponseTime.TotalSeconds:F2}s avg response time"
                );
                response.AppendLine();
            }
        }

        return response.ToString();
    }

    private static string FormatModelComparison(
        IList<(string ModelName, Knowledge.Analytics.Models.ModelUsageStats Stats)> models,
        string focus
    )
    {
        var response = new StringBuilder();

        response.AppendLine($"⚖️ **Model Comparison** - Focus: {focus}");
        response.AppendLine();

        // Comparison table
        response.AppendLine(
            "| Model | Provider | Conversations | Success Rate | Avg Response Time | Total Tokens |"
        );
        response.AppendLine(
            "|-------|----------|---------------|--------------|-------------------|--------------|"
        );

        foreach (var (modelName, stats) in models)
        {
            response.AppendLine(
                $"| {stats.ModelName} | {stats.Provider} | {stats.ConversationCount:N0} | {stats.SuccessRate:F1}% | {stats.AverageResponseTime.TotalSeconds:F2}s | {stats.TotalTokens:N0} |"
            );
        }
        response.AppendLine();

        // Winner analysis based on focus
        if (
            string.Equals(focus, "performance", StringComparison.OrdinalIgnoreCase)
            || string.Equals(focus, "all", StringComparison.OrdinalIgnoreCase)
        )
        {
            var bestPerformance = models
                .OrderByDescending(m => m.Stats.SuccessRate)
                .ThenBy(m => m.Stats.AverageResponseTime.TotalSeconds)
                .First();

            response.AppendLine(
                $"🏆 **Best Performance:** {bestPerformance.Stats.ModelName} ({bestPerformance.Stats.Provider})"
            );
            response.AppendLine(
                $"   • Highest success rate: {bestPerformance.Stats.SuccessRate:F1}%"
            );
            response.AppendLine(
                $"   • Response time: {bestPerformance.Stats.AverageResponseTime.TotalSeconds:F2}s"
            );
            response.AppendLine();
        }

        if (
            string.Equals(focus, "usage", StringComparison.OrdinalIgnoreCase)
            || string.Equals(focus, "all", StringComparison.OrdinalIgnoreCase)
        )
        {
            var mostUsed = models.OrderByDescending(m => m.Stats.ConversationCount).First();

            response.AppendLine(
                $"📈 **Most Used:** {mostUsed.Stats.ModelName} ({mostUsed.Stats.Provider})"
            );
            response.AppendLine($"   • {mostUsed.Stats.ConversationCount:N0} conversations");
            response.AppendLine($"   • {mostUsed.Stats.TotalTokens:N0} total tokens");
            response.AppendLine();
        }

        return response.ToString();
    }

    private static string GetRelativeTimeString(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime.ToUniversalTime();

        return timeSpan.TotalDays switch
        {
            < 1 when timeSpan.TotalHours < 1 => $"{(int)timeSpan.TotalMinutes} minutes ago",
            < 1 => $"{(int)timeSpan.TotalHours} hours ago",
            < 7 => $"{(int)timeSpan.TotalDays} days ago",
            < 30 => $"{(int)(timeSpan.TotalDays / 7)} weeks ago",
            < 365 => $"{(int)(timeSpan.TotalDays / 30)} months ago",
            _ => $"{(int)(timeSpan.TotalDays / 365)} years ago",
        };
    }
}
