using Knowledge.Analytics.Models;
using Knowledge.Analytics.Services;
using Knowledge.Contracts;
using Knowledge.Data;
using KnowledgeEngine.Agents.AgentFramework;
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace KnowledgeManager.Tests.AgentFramework;

public class KnowledgeAnalyticsPluginTests : IDisposable
{
    private readonly Mock<IKnowledgeRepository> _mockKnowledgeRepository;
    private readonly Mock<IUsageTrackingService> _mockUsageService;
    private readonly Mock<IVectorStoreStrategy> _mockVectorStore;
    private readonly ISqliteDbContext _dbContext;
    private readonly KnowledgeAnalyticsPlugin _plugin;

    public KnowledgeAnalyticsPluginTests()
    {
        _mockKnowledgeRepository = new Mock<IKnowledgeRepository>();
        _mockUsageService = new Mock<IUsageTrackingService>();
        _mockVectorStore = new Mock<IVectorStoreStrategy>();

        // Use in-memory SQLite database for testing
        _dbContext = new SqliteDbContext(":memory:");

        _plugin = new KnowledgeAnalyticsPlugin(
            _mockKnowledgeRepository.Object,
            _mockUsageService.Object,
            _dbContext,
            _mockVectorStore.Object
        );

        // Setup default vector store behavior
        SetupDefaultVectorStoreMock();
    }

    private void SetupDefaultVectorStoreMock()
    {
        _mockVectorStore
            .Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "kb1", "kb2" });
    }

    [Fact]
    public void KnowledgeAnalyticsPlugin_ShouldHave_OnePublicMethod()
    {
        // Arrange & Act
        var publicMethods = typeof(KnowledgeAnalyticsPlugin)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(m => m.DeclaringType == typeof(KnowledgeAnalyticsPlugin))
            .Where(m => !m.IsSpecialName)
            .ToList();

        // Assert
        Assert.Single(publicMethods);
        Assert.Equal("GetKnowledgeBaseSummaryAsync", publicMethods[0].Name);
    }

    [Fact]
    public void AgentToolRegistration_ShouldDiscover_OneFunction()
    {
        // Arrange & Act
        var tools = AgentToolRegistration.CreateToolsFromPlugin(_plugin);

        // Assert
        Assert.Single(tools);
    }

    [Fact]
    public async Task GetKnowledgeBaseSummaryAsync_WithNoKnowledgeBases_ReturnsMessage()
    {
        // Arrange
        _mockKnowledgeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeSummaryDto>());

        _mockVectorStore
            .Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _plugin.GetKnowledgeBaseSummaryAsync();

        // Assert
        Assert.Contains("No knowledge bases found", result);
    }

    [Fact]
    public async Task GetKnowledgeBaseSummaryAsync_WithKnowledgeBases_ReturnsFormattedSummary()
    {
        // Arrange
        var knowledgeBases = new List<KnowledgeSummaryDto>
        {
            new() { Id = "kb1", Name = "React Docs", DocumentCount = 45 },
            new() { Id = "kb2", Name = "API Reference", DocumentCount = 28 }
        };

        _mockKnowledgeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(knowledgeBases);

        await SetupTestDataAsync();

        var usageStats = new List<KnowledgeUsageStats>
        {
            new() { KnowledgeId = "kb1", QueryCount = 234 },
            new() { KnowledgeId = "kb2", QueryCount = 89 }
        };

        _mockUsageService
            .Setup(x => x.GetKnowledgeUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usageStats);

        // Act
        var result = await _plugin.GetKnowledgeBaseSummaryAsync(includeMetrics: true, sortBy: "activity");

        // Assert
        Assert.Contains("Knowledge Base Summary (2 total)", result);
        Assert.Contains("React Docs", result);
        Assert.Contains("API Reference", result);
        Assert.Contains("Sorted by Activity", result);
        Assert.Contains("System Totals", result);
    }

    [Fact]
    public async Task GetKnowledgeBaseSummaryAsync_SortByActivity_SortsCorrectly()
    {
        // Arrange
        var knowledgeBases = new List<KnowledgeSummaryDto>
        {
            new() { Id = "kb1", Name = "React Docs", DocumentCount = 45 },
            new() { Id = "kb2", Name = "API Reference", DocumentCount = 28 }
        };

        _mockKnowledgeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(knowledgeBases);

        await SetupTestDataAsync();

        var usageStats = new List<KnowledgeUsageStats>
        {
            new() { KnowledgeId = "kb1", QueryCount = 5 },
            new() { KnowledgeId = "kb2", QueryCount = 100 }
        };

        _mockUsageService
            .Setup(x => x.GetKnowledgeUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usageStats);

        // Act
        var result = await _plugin.GetKnowledgeBaseSummaryAsync(true, "activity");

        // Assert
        var lines = result.Split('\n');
        var apiRefIndex = Array.FindIndex(lines, line => line.Contains("API Reference"));
        var reactDocsIndex = Array.FindIndex(lines, line => line.Contains("React Docs"));

        Assert.True(apiRefIndex < reactDocsIndex, "API Reference (high activity) should appear before React Docs (low activity)");
    }

    [Theory]
    [InlineData(0, "None")]
    [InlineData(5, "Low")]
    [InlineData(25, "Medium")]
    [InlineData(75, "High")]
    public async Task GetKnowledgeBaseSummaryAsync_ActivityLevels_CalculatedCorrectly(int queryCount, string expectedActivity)
    {
        // Arrange
        var knowledgeBases = new List<KnowledgeSummaryDto>
        {
            new() { Id = "kb1", Name = "Test KB", DocumentCount = 10 }
        };

        _mockKnowledgeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(knowledgeBases);

        await SetupTestDataAsync();

        var usageStats = new List<KnowledgeUsageStats>
        {
            new() { KnowledgeId = "kb1", QueryCount = queryCount }
        };

        _mockUsageService
            .Setup(x => x.GetKnowledgeUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usageStats);

        // Act
        var result = await _plugin.GetKnowledgeBaseSummaryAsync();

        // Assert
        Assert.Contains($"Activity: {expectedActivity}", result);
    }

    [Fact]
    public async Task GetKnowledgeBaseSummaryAsync_WithoutMetrics_ExcludesDetailedInformation()
    {
        // Arrange
        var knowledgeBases = new List<KnowledgeSummaryDto>
        {
            new() { Id = "kb1", Name = "React Docs", DocumentCount = 45 }
        };

        _mockKnowledgeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(knowledgeBases);

        await SetupTestDataAsync();

        _mockUsageService
            .Setup(x => x.GetKnowledgeUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeUsageStats>());

        // Act
        var result = await _plugin.GetKnowledgeBaseSummaryAsync(false, "activity");

        // Assert
        Assert.Contains("React Docs", result);
        Assert.DoesNotContain("Documents:", result);
        Assert.DoesNotContain("Chunks:", result);
        Assert.DoesNotContain("System Totals:", result);
    }

    [Fact]
    public async Task GetKnowledgeBaseSummaryAsync_HandlesExceptions_ReturnsErrorMessage()
    {
        // Arrange
        _mockKnowledgeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _plugin.GetKnowledgeBaseSummaryAsync(true, "activity");

        // Assert
        Assert.Contains("Error retrieving knowledge base summary", result);
        Assert.Contains("Database error", result);
    }

    private async Task SetupTestDataAsync()
    {
        await _dbContext.InitializeDatabaseAsync();

        var connection = await _dbContext.GetConnectionAsync();

        // Disable foreign keys for testing
        using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = OFF";
        await pragmaCommand.ExecuteNonQueryAsync();

        // Insert test knowledge collections
        const string insertSql = """
            INSERT OR REPLACE INTO KnowledgeCollections
            (CollectionId, Name, DocumentCount, ChunkCount, TotalTokens, Status, CreatedAt, UpdatedAt)
            VALUES
            ('kb1', 'React Docs', 45, 2847, 50000, 'Active', datetime('now', '-30 days'), datetime('now', '-1 day')),
            ('kb2', 'API Reference', 28, 1234, 30000, 'Active', datetime('now', '-20 days'), datetime('now', '-2 days'))
            """;

        using var command = connection.CreateCommand();
        command.CommandText = insertSql;
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
