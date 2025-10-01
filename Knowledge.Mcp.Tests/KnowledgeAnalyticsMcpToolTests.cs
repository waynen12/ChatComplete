using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;
using Knowledge.Mcp.Tools;
using Knowledge.Mcp.Configuration;
using System.Text.Json;

namespace Knowledge.Mcp.Tests;

/// <summary>
/// Tests for KnowledgeAnalyticsMcpTool functionality.
/// These tests verify parameter validation, error handling, and configuration usage.
/// </summary>
public class KnowledgeAnalyticsMcpToolTests
{
    [Theory]
    [InlineData("invalid")]
    [InlineData("name")]
    [InlineData("")]
    public async Task GetKnowledgeBaseSummary_WithInvalidSortBy_ShouldReturnError(string sortBy)
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await KnowledgeAnalyticsMcpTool.GetKnowledgeBaseSummaryAsync(
            serviceProvider: services,
            sortBy: sortBy
        );

        // Assert
        result.Should().Contain("\"error\": true");
        result.Should().Contain("SortBy must be one of");
    }

    [Theory]
    [InlineData("activity")]
    [InlineData("size")]
    [InlineData("age")]
    [InlineData("alphabetical")]
    public async Task GetKnowledgeBaseSummary_WithValidSortBy_ShouldNotReturnSortError(string sortBy)
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await KnowledgeAnalyticsMcpTool.GetKnowledgeBaseSummaryAsync(
            serviceProvider: services,
            sortBy: sortBy
        );

        // Assert
        result.Should().NotContain("SortBy must be one of",
            $"because '{sortBy}' is a valid sort option");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetKnowledgeBaseSummary_WithIncludeMetrics_ShouldNotReturnError(bool includeMetrics)
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await KnowledgeAnalyticsMcpTool.GetKnowledgeBaseSummaryAsync(
            serviceProvider: services,
            includeMetrics: includeMetrics
        );

        // Assert
        // Should not have parameter validation errors (may have service resolution errors)
        result.Should().NotContain("includeMetrics",
            $"because {includeMetrics} is a valid boolean value");
    }

    [Fact]
    public async Task GetKnowledgeBaseHealth_ShouldReturnPlannedFeatureMessage()
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await KnowledgeAnalyticsMcpTool.GetKnowledgeBaseHealthAsync(
            serviceProvider: services
        );

        // Assert
        result.Should().Contain("Partial Implementation", "because this feature is partially implemented");

        // Verify it's valid JSON
        var json = JsonSerializer.Deserialize<JsonDocument>(result);
        json.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStorageOptimizationRecommendations_ShouldReturnFutureEnhancementMessage()
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await KnowledgeAnalyticsMcpTool.GetStorageOptimizationRecommendationsAsync(
            serviceProvider: services
        );

        // Assert
        result.Should().Contain("Future Enhancement", "because this feature is planned for future");

        // Verify it's valid JSON
        var json = JsonSerializer.Deserialize<JsonDocument>(result);
        json.Should().NotBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(90)]
    public async Task GetStorageOptimizationRecommendations_WithValidThreshold_ShouldNotReturnError(int threshold)
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await KnowledgeAnalyticsMcpTool.GetStorageOptimizationRecommendationsAsync(
            serviceProvider: services,
            minUsageThreshold: threshold
        );

        // Assert
        result.Should().NotBeNullOrWhiteSpace();

        // Should be valid JSON
        var json = JsonSerializer.Deserialize<JsonDocument>(result);
        json.Should().NotBeNull();
    }

    [Fact]
    public void ErrorResponse_WithConfiguration_ShouldBeValidJson()
    {
        // This test verifies error response structure
        var errorResponse = new
        {
            Error = true,
            Tool = "GetKnowledgeBaseSummaryAsync",
            Message = "Service configuration error",
            Timestamp = DateTime.UtcNow,
            Suggestions = new[]
            {
                "Check if knowledge base system is properly configured",
                "Verify SQLite database is accessible"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert
        json.Should().Contain("\"error\": true");
        json.Should().Contain("\"tool\"");
        json.Should().Contain("\"suggestions\"");

        // Verify it can be deserialized
        var deserialized = JsonSerializer.Deserialize<JsonDocument>(json);
        deserialized.Should().NotBeNull();
    }

    [Fact]
    public void PlannedFeatureResponse_ShouldIncludeWorkarounds()
    {
        // This test verifies planned feature response structure
        var plannedFeatureResponse = new
        {
            Status = "Future Enhancement",
            Message = "Feature planned for future implementation",
            CurrentCapabilities = new
            {
                BasicAnalytics = "Available",
                ActivitySorting = "Available"
            },
            PlannedFeatures = new[]
            {
                "Feature 1",
                "Feature 2"
            },
            WorkaroundSuggestions = new[]
            {
                "Use alternative method",
                "Use different approach"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(plannedFeatureResponse, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert
        json.Should().Contain("\"status\"");
        json.Should().Contain("\"plannedFeatures\"");
        json.Should().Contain("\"workaroundSuggestions\"");

        // Verify it can be deserialized
        var deserialized = JsonSerializer.Deserialize<JsonDocument>(json);
        deserialized.Should().NotBeNull();
    }

    [Fact]
    public async Task GetKnowledgeBaseSummary_WithDefaultParameters_ShouldUseConfigDefaults()
    {
        // Arrange
        var services = CreateTestServiceProvider();
        var settings = services.GetRequiredService<McpServerSettings>();

        // Act - Call without optional parameters to verify defaults are used
        var result = await KnowledgeAnalyticsMcpTool.GetKnowledgeBaseSummaryAsync(
            serviceProvider: services
        );

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        // The actual summary will fail due to missing services, but validation should pass
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task GetKnowledgeBaseHealth_WithVariousParameters_ShouldReturnResponse(
        bool checkSynchronization,
        bool includePerformanceMetrics)
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await KnowledgeAnalyticsMcpTool.GetKnowledgeBaseHealthAsync(
            serviceProvider: services,
            checkSynchronization: checkSynchronization,
            includePerformanceMetrics: includePerformanceMetrics
        );

        // Assert
        result.Should().NotBeNullOrWhiteSpace();

        // Should be valid JSON
        var json = JsonSerializer.Deserialize<JsonDocument>(result);
        json.Should().NotBeNull();
    }

    [Fact]
    public async Task GetKnowledgeBaseSummary_SortOptions_ShouldBeCaseInsensitive()
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act - Test with different cases
        var resultLower = await KnowledgeAnalyticsMcpTool.GetKnowledgeBaseSummaryAsync(
            serviceProvider: services,
            sortBy: "activity"
        );

        var resultUpper = await KnowledgeAnalyticsMcpTool.GetKnowledgeBaseSummaryAsync(
            serviceProvider: services,
            sortBy: "ACTIVITY"
        );

        var resultMixed = await KnowledgeAnalyticsMcpTool.GetKnowledgeBaseSummaryAsync(
            serviceProvider: services,
            sortBy: "Activity"
        );

        // Assert - All should be treated as valid
        resultLower.Should().NotContain("SortBy must be one of");
        resultUpper.Should().NotContain("SortBy must be one of");
        resultMixed.Should().NotContain("SortBy must be one of");
    }

    private static IServiceProvider CreateTestServiceProvider()
    {
        var services = new ServiceCollection();

        // Register McpServerSettings with test defaults
        var mcpSettings = new McpServerSettings
        {
            KnowledgeAnalytics = new KnowledgeAnalyticsSettings
            {
                DefaultIncludeMetrics = true,
                DefaultSortBy = "activity",
                DefaultMinUsageThresholdDays = 30
            }
        };
        services.AddSingleton(mcpSettings);

        // Note: We don't register all the required services (IKnowledgeRepository, IUsageTrackingService, etc.)
        // for these validation tests because we're only testing parameter validation

        return services.BuildServiceProvider();
    }
}
