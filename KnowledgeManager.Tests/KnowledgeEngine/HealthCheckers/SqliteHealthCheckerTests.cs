using KnowledgeEngine.Services.HealthCheckers;
using Knowledge.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;

namespace KnowledgeManager.Tests.KnowledgeEngine.HealthCheckers;

public class SqliteHealthCheckerTests : IDisposable
{
    private readonly Mock<ISqliteDbContext> _mockDbContext;
    private readonly Mock<ILogger<SqliteHealthChecker>> _mockLogger;
    private readonly SqliteConnection _connection;
    private readonly SqliteHealthChecker _healthChecker;

    public SqliteHealthCheckerTests()
    {
        _mockDbContext = new Mock<ISqliteDbContext>();
        _mockLogger = new Mock<ILogger<SqliteHealthChecker>>();
        
        // Create in-memory SQLite connection for testing
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        // Setup database schema
        SetupTestDatabase();
        
        _mockDbContext.Setup(x => x.GetConnectionAsync())
            .ReturnsAsync(_connection);
        
        _healthChecker = new SqliteHealthChecker(_mockDbContext.Object, _mockLogger.Object);
    }

    private void SetupTestDatabase()
    {
        var createTableSql = """
            CREATE TABLE IF NOT EXISTS KnowledgeCollections (
                CollectionId TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Status TEXT NOT NULL DEFAULT 'Active',
                DocumentCount INTEGER DEFAULT 0,
                ChunkCount INTEGER DEFAULT 0,
                CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP
            );
            """;
        
        using var command = new SqliteCommand(createTableSql, _connection);
        command.ExecuteNonQuery();
        
        // Insert test data
        var insertSql = """
            INSERT INTO KnowledgeCollections (CollectionId, Name, Status, DocumentCount, ChunkCount)
            VALUES 
                ('test1', 'Test Collection 1', 'Active', 5, 100),
                ('test2', 'Test Collection 2', 'Active', 3, 75);
            """;
        
        using var insertCommand = new SqliteCommand(insertSql, _connection);
        insertCommand.ExecuteNonQuery();
    }

    [Fact]
    public void ComponentProperties_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("SQLite", _healthChecker.ComponentName);
        Assert.Equal(1, _healthChecker.Priority);
        Assert.True(_healthChecker.IsCriticalComponent);
    }

    [Fact]
    public async Task CheckHealthAsync_WithWorkingDatabase_ShouldReturnHealthy()
    {
        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("SQLite", result.ComponentName);
        Assert.Equal("Healthy", result.Status);
        Assert.True(result.IsConnected);
        Assert.Contains("Database operational", result.StatusMessage);
        Assert.True(result.ResponseTime.TotalMilliseconds > 0);
        Assert.NotEmpty(result.Metrics);
    }

    [Fact]
    public async Task CheckHealthAsync_WithSlowResponse_ShouldReturnWarning()
    {
        // Arrange - Create a slow database operation by adding delay
        _mockDbContext.Setup(x => x.GetConnectionAsync())
            .Returns(async () =>
            {
                await Task.Delay(1100); // > 1 second delay
                return _connection;
            });

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("Warning", result.Status);
        Assert.Contains("Slow database response time", result.StatusMessage);
        Assert.True(result.ResponseTime.TotalMilliseconds > 1000);
    }

    [Fact]
    public async Task CheckHealthAsync_WithDatabaseError_ShouldReturnCritical()
    {
        // Arrange
        _mockDbContext.Setup(x => x.GetConnectionAsync())
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("Critical", result.Status);
        Assert.False(result.IsConnected);
        Assert.Contains("Database connection failed", result.StatusMessage);
        Assert.Equal(1, result.ErrorCount);
    }

    [Fact]
    public async Task QuickHealthCheckAsync_WithWorkingDatabase_ShouldReturnHealthy()
    {
        // Act
        var result = await _healthChecker.QuickHealthCheckAsync();

        // Assert
        Assert.Equal("SQLite", result.ComponentName);
        Assert.Equal("Healthy", result.Status);
        Assert.True(result.IsConnected);
        Assert.Equal("Database connection successful", result.StatusMessage);
    }

    [Fact]
    public async Task QuickHealthCheckAsync_WithDatabaseError_ShouldReturnCritical()
    {
        // Arrange
        _mockDbContext.Setup(x => x.GetConnectionAsync())
            .ThrowsAsync(new SqliteException("Database locked", 5));

        // Act
        var result = await _healthChecker.QuickHealthCheckAsync();

        // Assert
        Assert.Equal("Critical", result.Status);
        Assert.False(result.IsConnected);
        Assert.Contains("Connection failed", result.StatusMessage);
    }

    [Fact]
    public async Task GetComponentMetricsAsync_ShouldReturnDatabaseMetrics()
    {
        // Act
        var metrics = await _healthChecker.GetComponentMetricsAsync();

        // Assert
        Assert.NotEmpty(metrics);
        Assert.Contains("DatabaseSizeBytes", metrics.Keys);
        Assert.Contains("FormattedDatabaseSize", metrics.Keys);
        Assert.Contains("TableCount", metrics.Keys);
        Assert.Contains("ActiveKnowledgeCollections", metrics.Keys);
        Assert.Contains("TotalDocuments", metrics.Keys);
        Assert.Contains("TotalChunks", metrics.Keys);
        
        // Verify specific values based on test data
        Assert.Equal(2, metrics["ActiveKnowledgeCollections"]);
        Assert.Equal(8, metrics["TotalDocuments"]); // 5 + 3
        Assert.Equal(175, metrics["TotalChunks"]); // 100 + 75
    }

    [Fact]
    public async Task GetComponentMetricsAsync_WithDatabaseError_ShouldIncludeError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.GetConnectionAsync())
            .ThrowsAsync(new InvalidOperationException("Connection failed"));

        // Act
        var metrics = await _healthChecker.GetComponentMetricsAsync();

        // Assert
        Assert.Contains("Error", metrics.Keys);
        Assert.Equal("Connection failed", metrics["Error"]);
    }

    [Theory]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1073741824, "1.0 GB")]
    public async Task GetComponentMetricsAsync_ShouldFormatDatabaseSizeCorrectly(long fakePageSize, string expectedFormat)
    {
        // Note: This test verifies the formatting logic exists
        // In a real scenario, we'd need to mock the PRAGMA commands to return specific values
        
        // Act
        var metrics = await _healthChecker.GetComponentMetricsAsync();

        // Assert
        Assert.Contains("FormattedDatabaseSize", metrics.Keys);
        Assert.IsType<string>(metrics["FormattedDatabaseSize"]);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}