using System.ComponentModel;
using Knowledge.Analytics.Models;
using Knowledge.Analytics.Services;
using Knowledge.Contracts.Types;
using KnowledgeEngine.Agents.Plugins;
using Microsoft.SemanticKernel;
using Moq;

namespace KnowledgeManager.Tests.KnowledgeEngine;

public class ModelRecommendationAgentTests
{
    private readonly Mock<IUsageTrackingService> _mockUsageService;
    private readonly ModelRecommendationAgent _agent;

    public ModelRecommendationAgentTests()
    {
        _mockUsageService = new Mock<IUsageTrackingService>();
        _agent = new ModelRecommendationAgent(_mockUsageService.Object);
    }

    [Fact]
    public void ModelRecommendationAgent_HasCorrectKernelFunctions()
    {
        // Arrange & Act - Use reflection to get methods with KernelFunction attribute
        var kernelFunctionMethods = typeof(ModelRecommendationAgent)
            .GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(KernelFunctionAttribute), false).Any())
            .ToList();

        // Assert
        Assert.Equal(3, kernelFunctionMethods.Count);
        
        var methodNames = kernelFunctionMethods.Select(m => m.Name).ToHashSet();
        Assert.Contains("GetPopularModelsAsync", methodNames);
        Assert.Contains("GetModelPerformanceAnalysisAsync", methodNames);
        Assert.Contains("CompareModelsAsync", methodNames);
    }

    [Fact]
    public void GetPopularModelsAsync_HasCorrectAttributes()
    {
        // Arrange & Act
        var method = typeof(ModelRecommendationAgent).GetMethod("GetPopularModelsAsync");
        var kernelFunctionAttr = method?.GetCustomAttributes(typeof(KernelFunctionAttribute), false).FirstOrDefault();
        var descriptionAttr = method?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(method);
        Assert.NotNull(kernelFunctionAttr);
        Assert.NotNull(descriptionAttr);
        
        var description = ((DescriptionAttribute)descriptionAttr).Description;
        Assert.Contains("most popular AI models", description);
        Assert.Contains("usage statistics", description);
    }

    [Fact]
    public async Task GetPopularModelsAsync_WithNoData_ReturnsNoDataMessage()
    {
        // Arrange
        _mockUsageService.Setup(x => x.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ModelUsageStats>());

        // Act
        var result = await _agent.GetPopularModelsAsync();

        // Assert
        Assert.Contains("No model usage data is currently available", result);
        _mockUsageService.Verify(x => x.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPopularModelsAsync_WithValidData_ReturnsFormattedRecommendations()
    {
        // Arrange
        var testData = CreateSampleModelUsageStats();
        _mockUsageService.Setup(x => x.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testData);

        // Act
        var result = await _agent.GetPopularModelsAsync(count: 2);

        // Assert
        Assert.Contains("🏆 **Top 2 Most Popular Models**", result);
        Assert.Contains("🥇 **gpt-4o**", result); // Should be first due to higher conversation count
        Assert.Contains("🥈 **claude-3-sonnet**", result); // Should be second
        Assert.Contains("150 conversations", result);
        Assert.Contains("95.0% success rate", result);
        Assert.Contains("💡 **Recommendation:**", result);
    }

    [Fact]
    public async Task GetPopularModelsAsync_WithProviderFilter_FiltersCorrectly()
    {
        // Arrange
        var testData = CreateSampleModelUsageStats();
        _mockUsageService.Setup(x => x.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testData);

        // Act
        var result = await _agent.GetPopularModelsAsync(count: 5, provider: "OpenAI");

        // Assert
        Assert.Contains("🥇 **gpt-4o** (OpenAi)", result); // Fixed casing
        Assert.DoesNotContain("claude-3-sonnet", result);
        Assert.DoesNotContain("gemini-1.5-pro", result);
    }

    [Fact]
    public async Task GetPopularModelsAsync_WithInvalidProvider_ReturnsErrorMessage()
    {
        // Arrange
        var testData = CreateSampleModelUsageStats();
        _mockUsageService.Setup(x => x.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testData);

        // Act
        var result = await _agent.GetPopularModelsAsync(provider: "InvalidProvider");

        // Assert
        Assert.Contains("Invalid provider 'InvalidProvider'", result);
        Assert.Contains("Valid options are: OpenAI, Anthropic, Google, Ollama, or all", result);
    }

    [Fact]
    public async Task GetModelPerformanceAnalysisAsync_WithValidModel_ReturnsAnalysis()
    {
        // Arrange
        var testData = CreateSampleModelUsageStats();
        _mockUsageService.Setup(x => x.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testData);

        // Act
        var result = await _agent.GetModelPerformanceAnalysisAsync("gpt-4o");

        // Assert
        Assert.Contains("📊 **Performance Analysis: gpt-4o**", result);
        Assert.Contains("**📈 Usage Statistics:**", result);
        Assert.Contains("Total Conversations: **150**", result);
        Assert.Contains("Success Rate: **95.0%**", result);
        Assert.Contains("**⚡ Performance Metrics:**", result);
        Assert.Contains("Average Response Time: **2.50 seconds**", result);
        Assert.Contains("**🔧 Capabilities:**", result);
        Assert.Contains("**📋 Overall Assessment:", result);
    }

    [Fact]
    public async Task GetModelPerformanceAnalysisAsync_WithNonexistentModel_ReturnsNotFound()
    {
        // Arrange
        var testData = CreateSampleModelUsageStats();
        _mockUsageService.Setup(x => x.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testData);

        // Act
        var result = await _agent.GetModelPerformanceAnalysisAsync("nonexistent-model");

        // Assert
        Assert.Contains("No performance data found for model 'nonexistent-model'", result);
    }

    [Fact]
    public async Task CompareModelsAsync_WithValidModels_ReturnsComparison()
    {
        // Arrange
        var testData = CreateSampleModelUsageStats();
        _mockUsageService.Setup(x => x.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testData);

        // Act
        var result = await _agent.CompareModelsAsync("gpt-4o, claude-3-sonnet");

        // Assert
        Assert.Contains("⚖️ **Model Comparison**", result);
        Assert.Contains("| Model | Provider | Conversations | Success Rate | Avg Response Time | Total Tokens |", result);
        Assert.Contains("| gpt-4o | OpenAi |", result); // Fixed casing
        Assert.Contains("| claude-3-sonnet | Anthropic |", result);
        Assert.Contains("🏆 **Best Performance:**", result);
        Assert.Contains("📈 **Most Used:**", result);
    }

    [Fact]
    public async Task CompareModelsAsync_WithSingleModel_ReturnsError()
    {
        // Act
        var result = await _agent.CompareModelsAsync("gpt-4o");

        // Assert
        Assert.Contains("Please provide at least 2 model names to compare", result);
        _mockUsageService.Verify(x => x.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompareModelsAsync_WithInsufficientData_ReturnsError()
    {
        // Arrange
        var limitedData = new List<ModelUsageStats>
        {
            CreateModelUsageStats("gpt-4o", AiProvider.OpenAi, 150, 95.0, 2.5, 50000)
        };
        _mockUsageService.Setup(x => x.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(limitedData);

        // Act
        var result = await _agent.CompareModelsAsync("gpt-4o, claude-3-sonnet, gemini-1.5-pro");

        // Assert
        Assert.Contains("Found data for only 1 model(s)", result);
        Assert.Contains("Need at least 2 models with usage data", result);
    }

    [Fact]
    public async Task GetPopularModelsAsync_HandlesExceptions_ReturnsErrorMessage()
    {
        // Arrange
        _mockUsageService.Setup(x => x.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _agent.GetPopularModelsAsync();

        // Assert
        Assert.Contains("Error retrieving model recommendations", result);
        Assert.Contains("Database connection failed", result);
    }

    [Fact]
    public async Task GetModelPerformanceAnalysisAsync_HandlesExceptions_ReturnsErrorMessage()
    {
        // Arrange
        _mockUsageService.Setup(x => x.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service unavailable"));

        // Act
        var result = await _agent.GetModelPerformanceAnalysisAsync("gpt-4o");

        // Assert
        Assert.Contains("Error analyzing model performance", result);
        Assert.Contains("Service unavailable", result);
    }

    [Fact]
    public async Task CompareModelsAsync_HandlesExceptions_ReturnsErrorMessage()
    {
        // Arrange
        _mockUsageService.Setup(x => x.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network timeout"));

        // Act
        var result = await _agent.CompareModelsAsync("gpt-4o, claude-3-sonnet");

        // Assert
        Assert.Contains("Error comparing models", result);
        Assert.Contains("Network timeout", result);
    }

    [Fact]
    public async Task GetPopularModelsAsync_SortsModelsByPopularity_ConversationCountFirst()
    {
        // Arrange - Create data where success rate and token count might be misleading
        var testData = new List<ModelUsageStats>
        {
            CreateModelUsageStats("high-success-low-usage", AiProvider.OpenAi, 10, 99.0, 1.0, 100000),
            CreateModelUsageStats("medium-usage-medium-success", AiProvider.Anthropic, 50, 85.0, 2.0, 30000),
            CreateModelUsageStats("high-usage-lower-success", AiProvider.Google, 100, 80.0, 3.0, 60000)
        };
        
        _mockUsageService.Setup(x => x.GetModelUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testData);

        // Act
        var result = await _agent.GetPopularModelsAsync(count: 3);

        // Assert - Should rank by conversation count first
        var lines = result.Split('\n');
        var goldMedalLine = lines.FirstOrDefault(l => l.Contains("🥇"));
        var silverMedalLine = lines.FirstOrDefault(l => l.Contains("🥈"));
        var bronzeMedalLine = lines.FirstOrDefault(l => l.Contains("🥉"));

        Assert.NotNull(goldMedalLine);
        Assert.NotNull(silverMedalLine);
        Assert.NotNull(bronzeMedalLine);
        
        Assert.Contains("high-usage-lower-success", goldMedalLine);
        Assert.Contains("medium-usage-medium-success", silverMedalLine);
        Assert.Contains("high-success-low-usage", bronzeMedalLine);
    }

    private static List<ModelUsageStats> CreateSampleModelUsageStats()
    {
        return new List<ModelUsageStats>
        {
            CreateModelUsageStats("gpt-4o", AiProvider.OpenAi, 150, 95.0, 2.5, 50000),
            CreateModelUsageStats("claude-3-sonnet", AiProvider.Anthropic, 120, 97.5, 3.2, 45000),
            CreateModelUsageStats("gemini-1.5-pro", AiProvider.Google, 80, 92.0, 1.8, 32000),
            CreateModelUsageStats("llama3:8b", AiProvider.Ollama, 45, 88.5, 4.1, 28000)
        };
    }

    private static ModelUsageStats CreateModelUsageStats(
        string modelName, 
        AiProvider provider,
        int conversationCount,
        double successRate,
        double avgResponseTimeSeconds,
        int totalTokens)
    {
        var successfulRequests = (int)(conversationCount * 1.2 * successRate / 100); // Assume ~1.2 requests per conversation
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
            SupportsTools = provider != AiProvider.Ollama, // Assume Ollama models don't support tools for testing
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests
        };
    }
}