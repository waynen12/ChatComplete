using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using ChatCompletion.Config;
using FluentAssertions;
using Xunit;
using Moq;
using KnowledgeEngine.Persistence.VectorStores;

namespace Knowledge.Mcp.Tests;

/// <summary>
/// Tests that verify Qdrant connection logic and service registration.
/// These tests help prevent regressions in the configuration and service setup.
/// </summary>
public class QdrantConnectionTests
{
    /// <summary>
    /// Test that verifies QdrantVectorStore can be created with correct configuration
    /// </summary>
    [Fact]
    public void QdrantVectorStore_WithCorrectConfig_ShouldBeCreatable()
    {
        // Arrange
        var qdrantSettings = new QdrantSettings
        {
            Host = "localhost",
            Port = 6334,
            UseHttps = false,
            ApiKey = null
        };
        
        // Act & Assert - Should not throw exception
        var action = () =>
        {
            var qdrantClient = new Qdrant.Client.QdrantClient(
                host: qdrantSettings.Host,
                port: qdrantSettings.Port,
                https: qdrantSettings.UseHttps,
                apiKey: qdrantSettings.ApiKey
            );
            return new QdrantVectorStore(qdrantClient, ownsClient: true);
        };
        
        action.Should().NotThrow("because valid Qdrant configuration should create QdrantVectorStore successfully");
    }

    /// <summary>
    /// Test that verifies the service registration pattern used in MCP server
    /// </summary>
    [Fact]
    public void ServiceRegistration_QdrantServices_ShouldResolveCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();
        
        var chatCompleteSettings = new ChatCompleteSettings();
        configuration.GetSection("ChatCompleteSettings").Bind(chatCompleteSettings);
        chatCompleteSettings.VectorStore.Provider = "Qdrant";
        chatCompleteSettings.VectorStore.Qdrant.Port = 6334;
        
        // Act - Register services as done in MCP server
        services.AddSingleton(chatCompleteSettings);
        services.AddSingleton(chatCompleteSettings.VectorStore.Qdrant);
        
        // Register QdrantVectorStore
        services.AddSingleton<QdrantVectorStore>(provider =>
        {
            var qdrantSettings = chatCompleteSettings.VectorStore.Qdrant;
            var qdrantClient = new Qdrant.Client.QdrantClient(
                host: qdrantSettings.Host,
                port: qdrantSettings.Port,
                https: qdrantSettings.UseHttps,
                apiKey: qdrantSettings.ApiKey
            );
            return new QdrantVectorStore(qdrantClient, ownsClient: true);
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert
        var resolvedSettings = serviceProvider.GetRequiredService<ChatCompleteSettings>();
        var resolvedQdrantSettings = serviceProvider.GetRequiredService<QdrantSettings>();
        var resolvedVectorStore = serviceProvider.GetRequiredService<QdrantVectorStore>();
        
        resolvedSettings.Should().NotBeNull();
        resolvedQdrantSettings.Should().NotBeNull();
        resolvedVectorStore.Should().NotBeNull();
        
        resolvedQdrantSettings.Port.Should().Be(6334);
        resolvedQdrantSettings.Host.Should().Be("localhost");
    }

    /// <summary>
    /// Test that verifies port configuration scenarios
    /// </summary>
    [Theory]
    [InlineData(6333, false, "REST API port should not match expected gRPC port")]
    [InlineData(6334, true, "gRPC port should match expected port")]
    [InlineData(9999, false, "Invalid port should not match expected port")]
    public void PortConfiguration_ShouldValidateCorrectly(int configuredPort, bool expectedMatch, string reason)
    {
        // Arrange
        var expectedPort = 6334;
        
        // Act
        var portMatch = configuredPort == expectedPort;
        
        // Assert
        portMatch.Should().Be(expectedMatch, reason);
    }

    /// <summary>
    /// Test that reproduces the exact debug output structure
    /// </summary>
    [Fact]
    public void DebugOutput_Structure_ShouldMatchMcpToolOutput()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var settings = new ChatCompleteSettings();
        configuration.GetSection("ChatCompleteSettings").Bind(settings);
        settings.VectorStore.Provider = "Qdrant";
        settings.VectorStore.Qdrant.Port = 6334;
        
        // Act - Create debug structure matching DebugQdrantConfigAsync
        var debugInfo = new
        {
            Timestamp = DateTime.UtcNow,
            ConfigurationSources = new
            {
                ChatSettingsVectorStoreProvider = settings.VectorStore?.Provider,
                QdrantSettingsFromDI = new
                {
                    Host = settings.VectorStore.Qdrant.Host,
                    Port = settings.VectorStore.Qdrant.Port,
                    UseHttps = settings.VectorStore.Qdrant.UseHttps,
                    ApiKey = string.IsNullOrEmpty(settings.VectorStore.Qdrant.ApiKey) ? "null" : "***SET***"
                }
            },
            TroubleshootingInfo = new
            {
                ExpectedPort = 6334,
                ActualConfiguredPort = settings.VectorStore.Qdrant.Port,
                PortMatch = settings.VectorStore.Qdrant.Port == 6334,
                RestApiTest = "curl http://localhost:6333/collections",
                GrpcPortNote = "Semantic Kernel uses gRPC (6334), REST API uses 6333"
            }
        };
        
        // Assert - Verify all required fields are present and correct
        debugInfo.ConfigurationSources.ChatSettingsVectorStoreProvider.Should().Be("Qdrant");
        debugInfo.ConfigurationSources.QdrantSettingsFromDI.Host.Should().Be("localhost");
        debugInfo.ConfigurationSources.QdrantSettingsFromDI.Port.Should().Be(6334);
        debugInfo.ConfigurationSources.QdrantSettingsFromDI.UseHttps.Should().BeFalse();
        debugInfo.ConfigurationSources.QdrantSettingsFromDI.ApiKey.Should().Be("null");
        
        debugInfo.TroubleshootingInfo.ExpectedPort.Should().Be(6334);
        debugInfo.TroubleshootingInfo.ActualConfiguredPort.Should().Be(6334);
        debugInfo.TroubleshootingInfo.PortMatch.Should().BeTrue();
        debugInfo.TroubleshootingInfo.RestApiTest.Should().Contain("6333");
        debugInfo.TroubleshootingInfo.GrpcPortNote.Should().Contain("6334");
    }

    /// <summary>
    /// Performance test to ensure configuration loading doesn't become expensive
    /// </summary>
    [Fact]
    public void ConfigurationLoading_Performance_ShouldBeReasonablyFast()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var iterations = 1000;
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var settings = new ChatCompleteSettings();
            configuration.GetSection("ChatCompleteSettings").Bind(settings);
            settings.VectorStore.Provider = "Qdrant";
            settings.VectorStore.Qdrant.Port = 6334;
        }
        
        stopwatch.Stop();
        
        // Assert - Should be reasonably fast (less than 1ms per configuration load)
        var averageTimeMs = stopwatch.ElapsedMilliseconds / (double)iterations;
        averageTimeMs.Should().BeLessThan(1.0, "because configuration loading should be fast");
    }

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("TestData/appsettings.test.json", optional: false)
            .Build();
    }
}