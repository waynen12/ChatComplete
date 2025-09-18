using System.ComponentModel;
using Knowledge.Analytics.Models;
using Knowledge.Analytics.Services;
using Knowledge.Contracts;
using Knowledge.Data;
using KnowledgeEngine.Agents.Models;
using KnowledgeEngine.Agents.Plugins;
using KnowledgeEngine.Persistence;
using Microsoft.SemanticKernel;
using Moq;

namespace KnowledgeManager.Tests.KnowledgeEngine;

public class KnowledgeAnalyticsAgentTests : IDisposable
{
    private readonly Mock<IKnowledgeRepository> _mockKnowledgeRepository;
    private readonly Mock<IUsageTrackingService> _mockUsageService;
    private readonly ISqliteDbContext _dbContext;
    private readonly KnowledgeAnalyticsAgent _agent;

    public KnowledgeAnalyticsAgentTests()
    {
        _mockKnowledgeRepository = new Mock<IKnowledgeRepository>();
        _mockUsageService = new Mock<IUsageTrackingService>();
        
        // Use in-memory SQLite database for testing
        _dbContext = new SqliteDbContext(":memory:");
        
        _agent = new KnowledgeAnalyticsAgent(
            _mockKnowledgeRepository.Object,
            _mockUsageService.Object,
            _dbContext
        );
    }

    [Fact]
    public void KnowledgeAnalyticsAgent_HasCorrectKernelFunction()
    {
        // Arrange & Act - Use reflection to get methods with KernelFunction attribute
        var kernelFunctionMethods = typeof(KnowledgeAnalyticsAgent)
            .GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(KernelFunctionAttribute), false).Any())
            .ToList();

        // Assert
        Assert.Single(kernelFunctionMethods);
        Assert.Equal("GetKnowledgeBaseSummaryAsync", kernelFunctionMethods[0].Name);
    }

    [Fact]
    public void GetKnowledgeBaseSummaryAsync_HasCorrectAttributes()
    {
        // Arrange & Act
        var method = typeof(KnowledgeAnalyticsAgent).GetMethod("GetKnowledgeBaseSummaryAsync");
        var kernelFunctionAttr = method?.GetCustomAttributes(typeof(KernelFunctionAttribute), false).FirstOrDefault();
        var descriptionAttr = method?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(method);
        Assert.NotNull(kernelFunctionAttr);
        Assert.NotNull(descriptionAttr);
        
        var description = ((DescriptionAttribute)descriptionAttr).Description;
        Assert.Contains("comprehensive summary", description);
        Assert.Contains("knowledge bases", description);
    }

    [Fact]
    public async Task GetKnowledgeBaseSummaryAsync_WithNoKnowledgeBases_ReturnsNoKnowledgeBasesMessage()
    {
        // Arrange
        _mockKnowledgeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeSummaryDto>());

        // Act
        var result = await _agent.GetKnowledgeBaseSummaryAsync(true, "activity");

        // Assert
        Assert.Contains("No knowledge bases found", result);
        Assert.Contains("Upload some documents", result);
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

        // Setup in-memory database with test data
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
        var result = await _agent.GetKnowledgeBaseSummaryAsync(includeMetrics: true, sortBy: "activity");

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
            new() { KnowledgeId = "kb1", QueryCount = 5 },   // Low activity  
            new() { KnowledgeId = "kb2", QueryCount = 100 }  // High activity
        };

        _mockUsageService
            .Setup(x => x.GetKnowledgeUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usageStats);

        // Act
        var result = await _agent.GetKnowledgeBaseSummaryAsync(true, "activity");

        // Assert
        var lines = result.Split('\n');
        var apiRefIndex = Array.FindIndex(lines, line => line.Contains("API Reference"));
        var reactDocsIndex = Array.FindIndex(lines, line => line.Contains("React Docs"));
        
        Assert.True(apiRefIndex < reactDocsIndex, "API Reference (high activity) should appear before React Docs (low activity)");
    }

    [Fact]
    public async Task GetKnowledgeBaseSummaryAsync_SortBySize_SortsCorrectly()
    {
        // Arrange
        var knowledgeBases = new List<KnowledgeSummaryDto>
        {
            new() { Id = "kb1", Name = "Small KB", DocumentCount = 10 },
            new() { Id = "kb2", Name = "Large KB", DocumentCount = 20 }
        };

        _mockKnowledgeRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(knowledgeBases);

        // Setup database with different chunk counts
        await SetupTestDataWithChunkCountsAsync();

        _mockUsageService
            .Setup(x => x.GetKnowledgeUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeUsageStats>());

        // Act
        var result = await _agent.GetKnowledgeBaseSummaryAsync(true, "size");

        // Assert
        var lines = result.Split('\n');
        var largeKbIndex = Array.FindIndex(lines, line => line.Contains("Large KB"));
        var smallKbIndex = Array.FindIndex(lines, line => line.Contains("Small KB"));
        
        Assert.True(largeKbIndex < smallKbIndex, "Large KB should appear before small KB when sorted by size");
    }

    [Fact]
    public async Task GetKnowledgeBaseSummaryAsync_SortByAlphabetical_SortsCorrectly()
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

        _mockUsageService
            .Setup(x => x.GetKnowledgeUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeUsageStats>());

        // Act
        var result = await _agent.GetKnowledgeBaseSummaryAsync(true, "alphabetical");

        // Assert
        var lines = result.Split('\n');
        var apiRefIndex = Array.FindIndex(lines, line => line.Contains("API Reference"));
        var reactDocsIndex = Array.FindIndex(lines, line => line.Contains("React Docs"));
        
        Assert.True(apiRefIndex < reactDocsIndex, "API Reference should appear before React Docs when sorted alphabetically");
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
        var result = await _agent.GetKnowledgeBaseSummaryAsync();

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
        var result = await _agent.GetKnowledgeBaseSummaryAsync(false, "activity");

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
        var result = await _agent.GetKnowledgeBaseSummaryAsync(true, "activity");

        // Assert
        Assert.Contains("Error retrieving knowledge base summary", result);
        Assert.Contains("Database error", result);
    }

    [Fact]
    public async Task GetKnowledgeBaseSummaryAsync_IncludesSystemTotals_WhenMetricsEnabled()
    {
        // Arrange
        var knowledgeBases = new List<KnowledgeSummaryDto>
        {
            new() { Id = "kb1", Name = "KB 1", DocumentCount = 45 },
            new() { Id = "kb2", Name = "KB 2", DocumentCount = 28 }
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
        var result = await _agent.GetKnowledgeBaseSummaryAsync(true, "activity");

        // Assert
        Assert.Contains("System Totals:", result);
        Assert.Contains("Total Documents: 73", result);
        Assert.Contains("Active Knowledge Bases: 2/2", result);
        Assert.Contains("Total Monthly Queries: 323", result);
    }

    [Fact]
    public async Task GetKnowledgeBaseSummaryAsync_DefaultSort_SortsByActivity()
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
            new() { KnowledgeId = "kb1", QueryCount = 100 },  // High activity
            new() { KnowledgeId = "kb2", QueryCount = 5 }     // Low activity
        };

        _mockUsageService
            .Setup(x => x.GetKnowledgeUsageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usageStats);

        // Act - No sortBy parameter (should default to activity)
        var result = await _agent.GetKnowledgeBaseSummaryAsync(true, "activity");

        // Assert
        Assert.Contains("Sorted by Activity", result);
        var lines = result.Split('\n');
        var reactDocsIndex = Array.FindIndex(lines, line => line.Contains("React Docs"));
        var apiRefIndex = Array.FindIndex(lines, line => line.Contains("API Reference"));
        
        Assert.True(reactDocsIndex < apiRefIndex, "React Docs (high activity) should appear before API Reference (low activity)");
    }

    /// <summary>
    /// Sets up test data in the in-memory database
    /// </summary>
    private async Task SetupTestDataAsync()
    {
        await _dbContext.InitializeDatabaseAsync();
        
        var connection = await _dbContext.GetConnectionAsync();
        
        // First disable foreign keys for testing
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

    /// <summary>
    /// Sets up test data with different chunk counts for size sorting tests
    /// </summary>
    private async Task SetupTestDataWithChunkCountsAsync()
    {
        await _dbContext.InitializeDatabaseAsync();
        
        var connection = await _dbContext.GetConnectionAsync();
        
        // First disable foreign keys for testing
        using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = OFF";
        await pragmaCommand.ExecuteNonQueryAsync();
        
        // Insert test knowledge collections with different chunk counts
        const string insertSql = """
            INSERT OR REPLACE INTO KnowledgeCollections 
            (CollectionId, Name, DocumentCount, ChunkCount, TotalTokens, Status, CreatedAt, UpdatedAt)
            VALUES 
            ('kb1', 'Small KB', 10, 500, 10000, 'Active', datetime('now', '-10 days'), datetime('now', '-1 day')),
            ('kb2', 'Large KB', 20, 2000, 40000, 'Active', datetime('now', '-20 days'), datetime('now', '-2 days'))
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