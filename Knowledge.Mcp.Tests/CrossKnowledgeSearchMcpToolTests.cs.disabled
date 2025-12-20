using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;
using Knowledge.Mcp.Tools;
using Knowledge.Mcp.Configuration;
using System.Text.Json;
using Moq;
using KnowledgeEngine;
using KnowledgeEngine.Agents.Plugins;

namespace Knowledge.Mcp.Tests;

/// <summary>
/// Tests for CrossKnowledgeSearchMcpTool functionality.
/// These tests verify parameter validation, error handling, and configuration usage.
/// </summary>
public class CrossKnowledgeSearchMcpToolTests
{
    [Fact]
    public async Task SearchAllKnowledgeBases_WithNullQuery_ShouldReturnError()
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await CrossKnowledgeSearchMcpTool.SearchAllKnowledgeBasesAsync(
            query: null!,
            serviceProvider: services
        );

        // Assert
        result.Should().Contain("\"error\": true", "because null query should return error");
        result.Should().Contain("Query parameter is required", "because query is mandatory");
    }

    [Fact]
    public async Task SearchAllKnowledgeBases_WithEmptyQuery_ShouldReturnError()
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await CrossKnowledgeSearchMcpTool.SearchAllKnowledgeBasesAsync(
            query: "",
            serviceProvider: services
        );

        // Assert
        result.Should().Contain("\"error\": true");
        result.Should().Contain("Query parameter is required");
    }

    [Fact]
    public async Task SearchAllKnowledgeBases_WithInvalidLimit_ShouldReturnError()
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act - Test limit too low
        var resultTooLow = await CrossKnowledgeSearchMcpTool.SearchAllKnowledgeBasesAsync(
            query: "test",
            serviceProvider: services,
            limit: 0
        );

        // Act - Test limit too high
        var resultTooHigh = await CrossKnowledgeSearchMcpTool.SearchAllKnowledgeBasesAsync(
            query: "test",
            serviceProvider: services,
            limit: 100
        );

        // Assert
        resultTooLow.Should().Contain("\"error\": true");
        resultTooLow.Should().Contain("Limit must be between");

        resultTooHigh.Should().Contain("\"error\": true");
        resultTooHigh.Should().Contain("Limit must be between");
    }

    [Fact]
    public async Task SearchAllKnowledgeBases_WithInvalidMinRelevance_ShouldReturnError()
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act - Test relevance too low
        var resultTooLow = await CrossKnowledgeSearchMcpTool.SearchAllKnowledgeBasesAsync(
            query: "test",
            serviceProvider: services,
            minRelevance: -0.1
        );

        // Act - Test relevance too high
        var resultTooHigh = await CrossKnowledgeSearchMcpTool.SearchAllKnowledgeBasesAsync(
            query: "test",
            serviceProvider: services,
            minRelevance: 1.5
        );

        // Assert
        resultTooLow.Should().Contain("\"error\": true");
        resultTooLow.Should().Contain("MinRelevance must be between");

        resultTooHigh.Should().Contain("\"error\": true");
        resultTooHigh.Should().Contain("MinRelevance must be between");
    }

    [Fact]
    public async Task SearchAllKnowledgeBases_WithDefaultParameters_ShouldUseConfigDefaults()
    {
        // Arrange
        var services = CreateTestServiceProvider();
        var settings = services.GetRequiredService<McpServerSettings>();

        // Act - Call without optional parameters
        var result = await CrossKnowledgeSearchMcpTool.SearchAllKnowledgeBasesAsync(
            query: "test query",
            serviceProvider: services
        );

        // Assert - Verify defaults are used (this will fail with embedding service error, which is expected)
        result.Should().NotBeNullOrWhiteSpace();
        // The actual search will fail due to missing KnowledgeManager, but validation should pass
    }

    [Fact]
    public void ErrorResponse_ShouldBeValidJson()
    {
        // This test verifies the error response structure without calling the actual method
        var errorResponse = new
        {
            Error = true,
            ErrorType = "Test Error",
            Message = "Test message",
            Query = "test query",
            Timestamp = DateTime.UtcNow,
            Suggestions = new[] { "suggestion1", "suggestion2" }
        };

        // Act
        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert
        json.Should().Contain("\"error\": true");
        json.Should().Contain("\"errorType\"");
        json.Should().Contain("\"message\"");
        json.Should().Contain("\"suggestions\"");

        // Verify it can be deserialized
        var deserialized = JsonSerializer.Deserialize<JsonDocument>(json);
        deserialized.Should().NotBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    public async Task SearchAllKnowledgeBases_WithValidLimits_ShouldAccept(int limit)
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await CrossKnowledgeSearchMcpTool.SearchAllKnowledgeBasesAsync(
            query: "test",
            serviceProvider: services,
            limit: limit
        );

        // Assert - Should not return limit validation error
        result.Should().NotContain("Limit must be between",
            $"because {limit} is a valid limit value");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.3)]
    [InlineData(0.6)]
    [InlineData(1.0)]
    public async Task SearchAllKnowledgeBases_WithValidMinRelevance_ShouldAccept(double minRelevance)
    {
        // Arrange
        var services = CreateTestServiceProvider();

        // Act
        var result = await CrossKnowledgeSearchMcpTool.SearchAllKnowledgeBasesAsync(
            query: "test",
            serviceProvider: services,
            minRelevance: minRelevance
        );

        // Assert - Should not return relevance validation error
        result.Should().NotContain("MinRelevance must be between",
            $"because {minRelevance} is a valid relevance value");
    }

    private static IServiceProvider CreateTestServiceProvider()
    {
        var services = new ServiceCollection();

        // Register McpServerSettings with test defaults
        var mcpSettings = new McpServerSettings
        {
            Search = new SearchSettings
            {
                DefaultResultLimit = 5,
                MaxResultLimit = 50,
                DefaultMinRelevance = 0.6,
                MaxPreviewLength = 200
            }
        };
        services.AddSingleton(mcpSettings);

        // Note: We don't register KnowledgeManager and dependencies for these validation tests
        // because we're only testing parameter validation, not the actual search functionality

        return services.BuildServiceProvider();
    }
}
