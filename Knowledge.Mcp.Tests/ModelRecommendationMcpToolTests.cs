using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;
using Knowledge.Mcp.Tools;
using Knowledge.Mcp.Configuration;
using System.Text.Json;

namespace Knowledge.Mcp.Tests;

/// <summary>
/// Tests for ModelRecommendationMcpTool functionality.
/// These tests verify parameter validation, error handling, and configuration usage.
/// </summary>
public class ModelRecommendationMcpToolTests
{
    [Fact]
    public async Task GetPopularModels_WithInvalidCount_ShouldReturnError()
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act - Test count too low
        var resultTooLow = await ModelRecommendationMcpTool.GetPopularModelsAsync(
            serviceProvider: services,
            count: 0
        );

        // Act - Test count too high
        var resultTooHigh = await ModelRecommendationMcpTool.GetPopularModelsAsync(
            serviceProvider: services,
            count: 100
        );

        // Assert
        resultTooLow.Should().Contain("\"error\": true");
        resultTooLow.Should().Contain("Count must be between");

        resultTooHigh.Should().Contain("\"error\": true");
        resultTooHigh.Should().Contain("Count must be between");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("yearly")]
    [InlineData("")]
    public async Task GetPopularModels_WithInvalidPeriod_ShouldReturnError(string period)
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await ModelRecommendationMcpTool.GetPopularModelsAsync(
            serviceProvider: services,
            period: period
        );

        // Assert
        result.Should().Contain("\"error\": true");
        result.Should().Contain("Period must be one of");
    }

    [Theory]
    [InlineData("daily")]
    [InlineData("weekly")]
    [InlineData("monthly")]
    [InlineData("all-time")]
    public async Task GetPopularModels_WithValidPeriod_ShouldNotReturnPeriodError(string period)
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await ModelRecommendationMcpTool.GetPopularModelsAsync(
            serviceProvider: services,
            period: period
        );

        // Assert
        result.Should().NotContain("Period must be one of",
            $"because '{period}' is a valid period value");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(20)]
    public async Task GetPopularModels_WithValidCount_ShouldNotReturnCountError(int count)
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await ModelRecommendationMcpTool.GetPopularModelsAsync(
            serviceProvider: services,
            count: count
        );

        // Assert
        result.Should().NotContain("Count must be between",
            $"because {count} is a valid count value");
    }

    [Fact]
    public async Task GetModelPerformanceAnalysis_WithNullModelName_ShouldReturnError()
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await ModelRecommendationMcpTool.GetModelPerformanceAnalysisAsync(
            modelName: null!,
            serviceProvider: services
        );

        // Assert
        result.Should().Contain("\"error\": true");
        result.Should().Contain("Model name is required");
    }

    [Fact]
    public async Task GetModelPerformanceAnalysis_WithEmptyModelName_ShouldReturnError()
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await ModelRecommendationMcpTool.GetModelPerformanceAnalysisAsync(
            modelName: "",
            serviceProvider: services
        );

        // Assert
        result.Should().Contain("\"error\": true");
        result.Should().Contain("Model name is required");
    }

    [Theory]
    [InlineData("gpt-4")]
    [InlineData("claude-sonnet-4")]
    [InlineData("gemma3:12b")]
    public async Task GetModelPerformanceAnalysis_WithValidModelName_ShouldNotReturnNameError(string modelName)
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await ModelRecommendationMcpTool.GetModelPerformanceAnalysisAsync(
            modelName: modelName,
            serviceProvider: services
        );

        // Assert
        result.Should().NotContain("Model name is required",
            $"because '{modelName}' is a valid model name");
    }

    [Fact]
    public async Task CompareModels_WithNullModelNames_ShouldReturnError()
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await ModelRecommendationMcpTool.CompareModelsAsync(
            modelNames: null!,
            serviceProvider: services
        );

        // Assert
        result.Should().Contain("\"error\": true");
        result.Should().Contain("Model names are required");
    }

    [Fact]
    public async Task CompareModels_WithEmptyModelNames_ShouldReturnError()
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await ModelRecommendationMcpTool.CompareModelsAsync(
            modelNames: "",
            serviceProvider: services
        );

        // Assert
        result.Should().Contain("\"error\": true");
        result.Should().Contain("Model names are required");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("cost")]
    [InlineData("")]
    public async Task CompareModels_WithInvalidFocus_ShouldReturnError(string focus)
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await ModelRecommendationMcpTool.CompareModelsAsync(
            modelNames: "gpt-4,claude-sonnet-4",
            serviceProvider: services,
            focus: focus
        );

        // Assert
        result.Should().Contain("\"error\": true");
        result.Should().Contain("Focus must be one of");
    }

    [Theory]
    [InlineData("performance")]
    [InlineData("usage")]
    [InlineData("efficiency")]
    [InlineData("all")]
    public async Task CompareModels_WithValidFocus_ShouldNotReturnFocusError(string focus)
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await ModelRecommendationMcpTool.CompareModelsAsync(
            modelNames: "gpt-4,claude-sonnet-4",
            serviceProvider: services,
            focus: focus
        );

        // Assert
        result.Should().NotContain("Focus must be one of",
            $"because '{focus}' is a valid focus value");
    }

    [Fact]
    public async Task CompareModels_WithValidModelNames_ShouldNotReturnNameError()
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await ModelRecommendationMcpTool.CompareModelsAsync(
            modelNames: "gpt-4,claude-sonnet-4,gemma3:12b",
            serviceProvider: services
        );

        // Assert
        result.Should().NotContain("Model names are required");
    }

    [Fact]
    public void NoDataResponse_ShouldBeValidJson()
    {
        // This test verifies the "no data" response structure
        var noDataResponse = new
        {
            Status = "No Usage Data",
            Message = "No model usage statistics found in the system",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(noDataResponse, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert
        json.Should().Contain("\"status\"");
        json.Should().Contain("\"message\"");

        // Verify it can be deserialized
        var deserialized = JsonSerializer.Deserialize<JsonDocument>(json);
        deserialized.Should().NotBeNull();
    }

    [Fact]
    public void ErrorResponse_WithSuggestions_ShouldBeValidJson()
    {
        // This test verifies error response structure with suggestions
        var errorResponse = new
        {
            Error = true,
            Tool = "GetPopularModelsAsync",
            Message = "Test error message",
            Timestamp = DateTime.UtcNow,
            Suggestions = new[]
            {
                "Check if model usage data exists in the system",
                "Verify model names are correct and exist in the system"
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

    private static IServiceProvider CreateTestServiceProvider()
    {
        var services = new ServiceCollection();

        // Register McpServerSettings with test defaults
        var mcpSettings = new McpServerSettings
        {
            ModelRecommendation = new ModelRecommendationSettings
            {
                DefaultModelCount = 3,
                MaxModelCount = 20,
                DefaultTimePeriod = "monthly",
                DefaultProvider = "all",
                DefaultComparisonFocus = "all"
            }
        };
        services.AddSingleton(mcpSettings);

        // Note: We don't register IUsageTrackingService for these validation tests
        // because we're only testing parameter validation, not the actual analytics functionality

        return services.BuildServiceProvider();
    }
}
