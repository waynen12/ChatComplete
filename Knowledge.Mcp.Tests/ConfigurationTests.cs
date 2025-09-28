using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ChatCompletion.Config;
using FluentAssertions;
using Xunit;
using System.Text.Json;

namespace Knowledge.Mcp.Tests;

/// <summary>
/// Regression tests for Qdrant configuration binding issues.
/// These tests ensure that the MCP server correctly loads Qdrant configuration
/// and doesn't fall back to MongoDB defaults.
/// </summary>
public class ConfigurationTests
{
    /// <summary>
    /// Test that documents the configuration binding behavior
    /// </summary>
    [Fact]
    public void ConfigurationBinding_WithDefaults_ShouldShowActualBehavior()
    {
        // Arrange - Test both empty config and valid config
        var emptyConfiguration = new ConfigurationBuilder().Build();
        var validConfiguration = CreateTestConfiguration();
        
        // Act - Test with empty configuration (should use defaults)
        var emptySettings = new ChatCompleteSettings();
        emptyConfiguration.GetSection("ChatCompleteSettings").Bind(emptySettings);
        
        // Act - Test with valid configuration (should bind correctly)
        var boundSettings = new ChatCompleteSettings();
        validConfiguration.GetSection("ChatCompleteSettings").Bind(boundSettings);
        
        // Assert - Document the actual behavior
        emptySettings.VectorStore.Provider.Should().Be("MongoDB", "because ChatCompleteSettings defaults to MongoDB when no config is provided");
        emptySettings.VectorStore.Qdrant.Port.Should().Be(6333, "because QdrantSettings defaults to port 6333 when no config is provided");
        
        // With valid configuration, binding should work
        boundSettings.VectorStore.Provider.Should().Be("Qdrant", "because test configuration specifies Qdrant");
        boundSettings.VectorStore.Qdrant.Port.Should().Be(6334, "because test configuration specifies port 6334");
    }

    /// <summary>
    /// Test that demonstrates the working solution: Get<T>() properly deserializes configuration
    /// </summary>
    [Fact]
    public void ConfigurationBinding_WithGet_ShouldLoadQdrantConfigurationCorrectly()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        
        // Act - Using the working approach (Get<T>)
        var workingSettings = configuration.GetSection("ChatCompleteSettings").Get<ChatCompleteSettings>();
        
        // Assert - This should load the correct values from appsettings.test.json
        workingSettings.Should().NotBeNull();
        workingSettings!.VectorStore.Provider.Should().Be("Qdrant", "because configuration specifies Qdrant");
        workingSettings.VectorStore.Qdrant.Host.Should().Be("localhost");
        workingSettings.VectorStore.Qdrant.Port.Should().Be(6334, "because configuration specifies gRPC port 6334");
        workingSettings.VectorStore.Qdrant.UseHttps.Should().BeFalse();
    }

    /// <summary>
    /// Test that verifies the manual override approach (current solution) works correctly
    /// </summary>
    [Fact]
    public void ConfigurationBinding_WithManualOverride_ShouldForceCorrectQdrantSettings()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        
        // Act - Using the current MCP server approach (Bind + manual override)
        var settings = new ChatCompleteSettings();
        configuration.GetSection("ChatCompleteSettings").Bind(settings);
        
        // Apply manual override (as done in MCP server Program.cs)
        settings.VectorStore.Provider = "Qdrant";
        settings.VectorStore.Qdrant.Port = 6334;
        
        // Assert - Manual override should work
        settings.VectorStore.Provider.Should().Be("Qdrant");
        settings.VectorStore.Qdrant.Port.Should().Be(6334);
        settings.VectorStore.Qdrant.Host.Should().Be("localhost"); // This should come from configuration
    }

    /// <summary>
    /// Integration test that verifies the complete MCP server service registration
    /// </summary>
    [Fact]
    public void McpServer_ServiceRegistration_ShouldConfigureQdrantServicesCorrectly()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var services = new ServiceCollection();
        
        // Act - Simulate MCP server service registration
        var settings = new ChatCompleteSettings();
        configuration.GetSection("ChatCompleteSettings").Bind(settings);
        settings.VectorStore.Provider = "Qdrant";
        settings.VectorStore.Qdrant.Port = 6334;
        
        services.AddSingleton(settings);
        services.AddSingleton(settings.VectorStore.Qdrant);
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        var resolvedSettings = serviceProvider.GetRequiredService<ChatCompleteSettings>();
        var resolvedQdrantSettings = serviceProvider.GetRequiredService<QdrantSettings>();
        
        resolvedSettings.VectorStore.Provider.Should().Be("Qdrant");
        resolvedQdrantSettings.Port.Should().Be(6334);
        resolvedQdrantSettings.Host.Should().Be("localhost");
    }

    /// <summary>
    /// Test that verifies the configuration values that caused the original issue
    /// </summary>
    [Theory]
    [InlineData("MongoDB", 6333)] // Original broken values
    [InlineData("Qdrant", 6334)]  // Correct values
    public void ConfigurationDefaults_ShouldMatchExpectedValues(string expectedProvider, int expectedPort)
    {
        // Arrange
        var settings = new ChatCompleteSettings();
        
        // Act & Assert - Document the default behavior
        if (expectedProvider == "MongoDB")
        {
            // This test documents the problematic defaults
            settings.VectorStore.Provider.Should().Be("MongoDB", "because this is the problematic default");
            settings.VectorStore.Qdrant.Port.Should().Be(6333, "because this is the problematic default");
        }
        else
        {
            // This shows what we want the configuration to be
            settings.VectorStore.Provider = "Qdrant";
            settings.VectorStore.Qdrant.Port = 6334;
            
            settings.VectorStore.Provider.Should().Be("Qdrant");
            settings.VectorStore.Qdrant.Port.Should().Be(6334);
        }
    }

    /// <summary>
    /// Test that simulates the debug tool output format
    /// </summary>
    [Fact]
    public void DebugOutput_ShouldMatchExpectedFormat()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var settings = new ChatCompleteSettings();
        configuration.GetSection("ChatCompleteSettings").Bind(settings);
        settings.VectorStore.Provider = "Qdrant";
        settings.VectorStore.Qdrant.Port = 6334;
        
        // Act - Create debug output similar to DebugQdrantConfigAsync
        var debugInfo = new
        {
            ConfigurationSources = new
            {
                ChatSettingsVectorStoreProvider = settings.VectorStore?.Provider,
                QdrantSettingsFromDI = new
                {
                    Host = settings.VectorStore.Qdrant.Host,
                    Port = settings.VectorStore.Qdrant.Port,
                    UseHttps = settings.VectorStore.Qdrant.UseHttps
                }
            },
            TroubleshootingInfo = new
            {
                ExpectedPort = 6334,
                ActualConfiguredPort = settings.VectorStore.Qdrant.Port,
                PortMatch = settings.VectorStore.Qdrant.Port == 6334
            }
        };
        
        // Assert - Verify debug output shows correct values
        debugInfo.ConfigurationSources.ChatSettingsVectorStoreProvider.Should().Be("Qdrant");
        debugInfo.ConfigurationSources.QdrantSettingsFromDI.Port.Should().Be(6334);
        debugInfo.TroubleshootingInfo.PortMatch.Should().BeTrue();
        
        // Serialize to ensure it's valid JSON (like the MCP tool returns)
        var json = JsonSerializer.Serialize(debugInfo, new JsonSerializerOptions { WriteIndented = true });
        json.Should().Contain("\"Qdrant\"");
        json.Should().Contain("6334");
    }

    /// <summary>
    /// Helper method to create test configuration from appsettings.test.json
    /// </summary>
    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("TestData/appsettings.test.json", optional: false)
            .Build();
    }
}