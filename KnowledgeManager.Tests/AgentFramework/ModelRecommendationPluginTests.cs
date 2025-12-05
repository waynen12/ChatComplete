using Knowledge.Analytics.Models;
using Knowledge.Analytics.Services;
using Knowledge.Contracts.Types;
using KnowledgeEngine.Agents.AgentFramework;
using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace KnowledgeManager.Tests.AgentFramework;

public class ModelRecommendationPluginTests
{
    private static ModelUsageStats CreateModelUsageStats(
        string modelName,
        AiProvider provider,
        int conversationCount,
        double successRate,
        double avgResponseTimeSeconds,
        int totalTokens)
    {
        var successfulRequests = (int)(conversationCount * 1.2 * successRate / 100);
        var totalRequests = (int)(conversationCount * 1.2);
        var failedRequests = totalRequests - successfulRequests;

        return new ModelUsageStats
        {
            ModelName = modelName,
            Provider = provider,
            ConversationCount = conversationCount,
            TotalTokens = totalTokens,
            AverageTokensPerRequest = totalTokens / Math.Max(totalRequests, 1),
            AverageResponseTime = TimeSpan.FromSeconds(avgResponseTimeSeconds),
            LastUsed = DateTime.UtcNow.AddHours(-Random.Shared.Next(1, 48)),
            SupportsTools = provider != AiProvider.Ollama,
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests
        };
    }

    [Fact]
    public void ModelRecommendationPlugin_ShouldHave_ThreePublicMethods()
    {
        // Arrange & Act
        var publicMethods = typeof(ModelRecommendationPlugin)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(m => m.DeclaringType == typeof(ModelRecommendationPlugin))
            .Where(m => !m.IsSpecialName) // Exclude property getters/setters
            .ToList();

        // Assert
        Assert.Equal(3, publicMethods.Count);
        Assert.Contains(publicMethods, m => m.Name == "GetPopularModelsAsync");
        Assert.Contains(publicMethods, m => m.Name == "GetModelPerformanceAnalysisAsync");
        Assert.Contains(publicMethods, m => m.Name == "CompareModelsAsync");
    }

    [Fact]
    public void AgentToolRegistration_ShouldDiscover_AllThreeFunctions()
    {
        // Arrange
        var mockUsageService = new Mock<IUsageTrackingService>();
        var plugin = new ModelRecommendationPlugin(mockUsageService.Object);

        // Act
        var tools = AgentToolRegistration.CreateToolsFromPlugin(plugin);

        // Assert
        Assert.Equal(3, tools.Count);
    }

    [Fact]
    public async Task GetPopularModelsAsync_ShouldReturnFormattedReport()
    {
        // Arrange
        var mockUsageService = new Mock<IUsageTrackingService>();
        var plugin = new ModelRecommendationPlugin(mockUsageService.Object);

        var mockStats = new List<ModelUsageStats>
        {
            CreateModelUsageStats("gpt-4", AiProvider.OpenAi, 100, 98.5, 2.5, 50000),
            CreateModelUsageStats("claude-sonnet-4", AiProvider.Anthropic, 50, 99.0, 3.0, 25000)
        };

        mockUsageService
            .Setup(s => s.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockStats);

        // Act
        var result = await plugin.GetPopularModelsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Top", result);
        Assert.Contains("gpt-4", result);
        Assert.Contains("claude-sonnet-4", result);
        Assert.Contains("🥇", result); // Gold medal for top model
    }

    [Fact]
    public async Task GetPopularModelsAsync_WithNoData_ShouldReturnMessage()
    {
        // Arrange
        var mockUsageService = new Mock<IUsageTrackingService>();
        var plugin = new ModelRecommendationPlugin(mockUsageService.Object);

        mockUsageService
            .Setup(s => s.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ModelUsageStats>());

        // Act
        var result = await plugin.GetPopularModelsAsync();

        // Assert
        Assert.Contains("No model usage data", result);
    }

    [Fact]
    public async Task GetPopularModelsAsync_WithProviderFilter_ShouldFilterResults()
    {
        // Arrange
        var mockUsageService = new Mock<IUsageTrackingService>();
        var plugin = new ModelRecommendationPlugin(mockUsageService.Object);

        var mockStats = new List<ModelUsageStats>
        {
            CreateModelUsageStats("gpt-4", AiProvider.OpenAi, 100, 98.5, 2.5, 50000),
            CreateModelUsageStats("claude-sonnet-4", AiProvider.Anthropic, 50, 99.0, 3.0, 25000)
        };

        mockUsageService
            .Setup(s => s.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockStats);

        // Act
        var result = await plugin.GetPopularModelsAsync(count: 5, period: "monthly", provider: "OpenAI");

        // Assert
        Assert.Contains("gpt-4", result);
        Assert.DoesNotContain("claude-sonnet-4", result);
    }

    [Fact]
    public async Task GetModelPerformanceAnalysisAsync_ShouldReturnAnalysis()
    {
        // Arrange
        var mockUsageService = new Mock<IUsageTrackingService>();
        var plugin = new ModelRecommendationPlugin(mockUsageService.Object);

        var mockStats = new List<ModelUsageStats>
        {
            CreateModelUsageStats("gpt-4", AiProvider.OpenAi, 100, 98.5, 2.5, 50000)
        };

        mockUsageService
            .Setup(s => s.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockStats);

        // Act
        var result = await plugin.GetModelPerformanceAnalysisAsync("gpt-4");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Performance Analysis", result);
        Assert.Contains("gpt-4", result);
        Assert.Contains("Success Rate", result); // Check for label instead of exact percentage
        Assert.Contains("Usage Statistics", result);
    }

    [Fact]
    public async Task GetModelPerformanceAnalysisAsync_WithNoData_ShouldReturnMessage()
    {
        // Arrange
        var mockUsageService = new Mock<IUsageTrackingService>();
        var plugin = new ModelRecommendationPlugin(mockUsageService.Object);

        mockUsageService
            .Setup(s => s.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ModelUsageStats>());

        // Act
        var result = await plugin.GetModelPerformanceAnalysisAsync("nonexistent-model");

        // Assert
        Assert.Contains("No performance data found", result);
        Assert.Contains("nonexistent-model", result);
    }

    [Fact]
    public async Task CompareModelsAsync_ShouldReturnComparison()
    {
        // Arrange
        var mockUsageService = new Mock<IUsageTrackingService>();
        var plugin = new ModelRecommendationPlugin(mockUsageService.Object);

        var mockStats = new List<ModelUsageStats>
        {
            CreateModelUsageStats("gpt-4", AiProvider.OpenAi, 100, 98.5, 2.5, 50000),
            CreateModelUsageStats("claude-sonnet-4", AiProvider.Anthropic, 50, 99.0, 3.0, 25000)
        };

        mockUsageService
            .Setup(s => s.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockStats);

        // Act
        var result = await plugin.CompareModelsAsync("gpt-4, claude-sonnet-4");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Model Comparison", result);
        Assert.Contains("gpt-4", result);
        Assert.Contains("claude-sonnet-4", result);
        Assert.Contains("Best Performance", result);
    }

    [Fact]
    public async Task CompareModelsAsync_WithLessThanTwoModels_ShouldReturnError()
    {
        // Arrange
        var mockUsageService = new Mock<IUsageTrackingService>();
        var plugin = new ModelRecommendationPlugin(mockUsageService.Object);

        // Act
        var result = await plugin.CompareModelsAsync("gpt-4");

        // Assert
        Assert.Contains("at least 2 model names", result);
    }

    [Fact]
    public async Task CompareModelsAsync_WithInsufficientData_ShouldReturnError()
    {
        // Arrange
        var mockUsageService = new Mock<IUsageTrackingService>();
        var plugin = new ModelRecommendationPlugin(mockUsageService.Object);

        var mockStats = new List<ModelUsageStats>
        {
            CreateModelUsageStats("gpt-4", AiProvider.OpenAi, 100, 98.5, 2.5, 50000)
        };

        mockUsageService
            .Setup(s => s.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockStats);

        // Act
        var result = await plugin.CompareModelsAsync("gpt-4, nonexistent-model");

        // Assert
        Assert.Contains("Found data for only", result);
        Assert.Contains("Need at least 2 models", result);
    }
}
