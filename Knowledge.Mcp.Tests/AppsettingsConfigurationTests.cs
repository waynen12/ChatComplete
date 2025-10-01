using Microsoft.Extensions.Configuration;
using FluentAssertions;
using Xunit;
using System.IO;
using Knowledge.Mcp.Configuration;

namespace Knowledge.Mcp.Tests;

/// <summary>
/// Tests for appsettings.json configuration loading in the MCP server.
/// These tests ensure the configuration file is properly copied and loaded.
/// </summary>
public class AppsettingsConfigurationTests
{
    [Fact]
    public void AppsettingsJson_ShouldExistInTestData()
    {
        // Arrange
        var testConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "appsettings.test.json");

        // Act & Assert
        File.Exists(testConfigPath).Should().BeTrue("because appsettings.test.json is required for tests");
    }

    [Fact]
    public void Configuration_ShouldLoadDatabasePath()
    {
        // Arrange
        var configuration = CreateTestConfiguration();

        // Act
        var databasePath = configuration["DatabasePath"];

        // Assert
        databasePath.Should().NotBeNullOrWhiteSpace("because DatabasePath must be configured");
        databasePath.Should().Contain("knowledge.db", "because it should point to the SQLite database");
    }

    [Fact]
    public void Configuration_ShouldLoadMcpServerSettings()
    {
        // Arrange
        var configuration = CreateTestConfiguration();

        // Act
        var mcpSettings = new McpServerSettings();
        configuration.GetSection(McpServerSettings.SectionName).Bind(mcpSettings);

        // Assert - Search settings
        mcpSettings.Search.Should().NotBeNull();
        mcpSettings.Search.DefaultResultLimit.Should().BeGreaterThan(0);
        mcpSettings.Search.MaxResultLimit.Should().BeGreaterThan(mcpSettings.Search.DefaultResultLimit);
        mcpSettings.Search.DefaultMinRelevance.Should().BeInRange(0.0, 1.0);

        // Assert - Model recommendation settings
        mcpSettings.ModelRecommendation.Should().NotBeNull();
        mcpSettings.ModelRecommendation.DefaultModelCount.Should().BeGreaterThan(0);
        mcpSettings.ModelRecommendation.MaxModelCount.Should().BeGreaterThan(mcpSettings.ModelRecommendation.DefaultModelCount);

        // Assert - Knowledge analytics settings
        mcpSettings.KnowledgeAnalytics.Should().NotBeNull();
        mcpSettings.KnowledgeAnalytics.DefaultIncludeMetrics.Should().BeTrue();

        // Assert - General settings
        mcpSettings.General.Should().NotBeNull();
        mcpSettings.General.OllamaBaseUrl.Should().NotBeNullOrWhiteSpace();
        mcpSettings.General.DefaultEmbeddingModel.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Configuration_DatabasePath_ShouldNotBeNullOrFallback()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var fallbackPath = "/tmp/knowledge-mcp/knowledge.db";

        // Act
        var databasePath = configuration["DatabasePath"];

        // Assert
        databasePath.Should().NotBeNullOrWhiteSpace("because DatabasePath must be explicitly configured");
        databasePath.Should().NotBe(fallbackPath, "because we should not fall back to the temporary path");
    }

    [Fact]
    public void Configuration_SearchSettings_ShouldHaveValidDefaults()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var mcpSettings = new McpServerSettings();
        configuration.GetSection(McpServerSettings.SectionName).Bind(mcpSettings);

        // Act & Assert
        mcpSettings.Search.DefaultResultLimit.Should().Be(5, "because this is a reasonable default");
        mcpSettings.Search.MaxResultLimit.Should().Be(50, "because this prevents resource exhaustion");
        mcpSettings.Search.DefaultMinRelevance.Should().Be(0.6, "because this balances precision and recall");
        mcpSettings.Search.MaxPreviewLength.Should().Be(200, "because this provides adequate context");
    }

    [Fact]
    public void Configuration_ModelRecommendationSettings_ShouldHaveValidDefaults()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var mcpSettings = new McpServerSettings();
        configuration.GetSection(McpServerSettings.SectionName).Bind(mcpSettings);

        // Act & Assert
        mcpSettings.ModelRecommendation.DefaultModelCount.Should().Be(3);
        mcpSettings.ModelRecommendation.MaxModelCount.Should().Be(20);
        mcpSettings.ModelRecommendation.DefaultTimePeriod.Should().Be("monthly");
        mcpSettings.ModelRecommendation.DefaultProvider.Should().Be("all");
        mcpSettings.ModelRecommendation.DefaultComparisonFocus.Should().Be("all");
    }

    [Fact]
    public void Configuration_BasePathResolution_ShouldWorkFromExecutableLocation()
    {
        // Arrange
        var executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var basePath = Path.GetDirectoryName(executablePath);

        // Act
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(basePath!)
            .AddJsonFile("TestData/appsettings.test.json", optional: false);

        var configuration = configBuilder.Build();

        // Assert
        configuration.Should().NotBeNull();
        configuration["DatabasePath"].Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Configuration_ShouldLoadChatCompleteSettings()
    {
        // Arrange
        var configuration = CreateTestConfiguration();

        // Act
        var chatCompleteSection = configuration.GetSection("ChatCompleteSettings");

        // Assert
        chatCompleteSection.Exists().Should().BeTrue("because ChatCompleteSettings section must exist");
        chatCompleteSection["VectorStore:Provider"].Should().Be("Qdrant");
        chatCompleteSection["VectorStore:Qdrant:Port"].Should().Be("6334");
    }

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("TestData/appsettings.test.json", optional: false)
            .Build();
    }
}
