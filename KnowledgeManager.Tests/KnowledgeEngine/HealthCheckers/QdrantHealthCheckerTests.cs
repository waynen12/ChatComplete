using KnowledgeEngine.Services.HealthCheckers;
using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.Extensions.Logging;
using Moq;

namespace KnowledgeManager.Tests.KnowledgeEngine.HealthCheckers;

public class QdrantHealthCheckerTests
{
    private readonly Mock<IVectorStoreStrategy> _mockVectorStore;
    private readonly Mock<ILogger<QdrantHealthChecker>> _mockLogger;
    private readonly QdrantHealthChecker _healthChecker;

    public QdrantHealthCheckerTests()
    {
        _mockVectorStore = new Mock<IVectorStoreStrategy>();
        _mockLogger = new Mock<ILogger<QdrantHealthChecker>>();
        _healthChecker = new QdrantHealthChecker(_mockVectorStore.Object, _mockLogger.Object);
    }

    [Fact]
    public void ComponentProperties_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("Qdrant", _healthChecker.ComponentName);
        Assert.Equal(2, _healthChecker.Priority);
        Assert.True(_healthChecker.IsCriticalComponent);
    }

    [Fact]
    public async Task CheckHealthAsync_WithWorkingVectorStore_ShouldReturnHealthy()
    {
        // Arrange
        var collections = new List<string> { "collection1", "collection2", "collection3" };
        _mockVectorStore.Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("Qdrant", result.ComponentName);
        Assert.Equal("Healthy", result.Status);
        Assert.True(result.IsConnected);
        Assert.Contains("Vector store operational with 3 collections", result.StatusMessage);
        Assert.True(result.ResponseTime.TotalMilliseconds >= 0);
        Assert.NotEmpty(result.Metrics);
    }

    [Fact]
    public async Task CheckHealthAsync_WithNoCollections_ShouldReturnWarning()
    {
        // Arrange
        _mockVectorStore.Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("Warning", result.Status);
        Assert.True(result.IsConnected);
        Assert.Contains("No collections found", result.StatusMessage);
    }

    [Fact]
    public async Task CheckHealthAsync_WithSlowResponse_ShouldReturnWarning()
    {
        // Arrange
        _mockVectorStore.Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) =>
            {
                await Task.Delay(2500, ct); // 2.5 second delay
                return new List<string> { "collection1" };
            });

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("Critical", result.Status);
        Assert.Contains("Very slow response time", result.StatusMessage);
        Assert.True(result.ResponseTime.TotalMilliseconds >= 2000);
    }

    [Fact]
    public async Task CheckHealthAsync_WithVerySlowResponse_ShouldReturnCritical()
    {
        // Arrange
        _mockVectorStore.Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) =>
            {
                await Task.Delay(6000, ct); // 6 second delay
                return new List<string> { "collection1" };
            });

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("Critical", result.Status);
        Assert.Contains("Very slow response time", result.StatusMessage);
        Assert.True(result.ResponseTime.TotalMilliseconds >= 5000);
    }

    [Fact]
    public async Task CheckHealthAsync_WithConnectionError_ShouldReturnCritical()
    {
        // Arrange
        _mockVectorStore.Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("Critical", result.Status);
        Assert.False(result.IsConnected);
        Assert.Contains("Vector store connection failed", result.StatusMessage);
        Assert.Equal(1, result.ErrorCount);
    }

    [Fact]
    public async Task QuickHealthCheckAsync_WithWorkingVectorStore_ShouldReturnHealthy()
    {
        // Arrange
        _mockVectorStore.Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "collection1" });

        // Act
        var result = await _healthChecker.QuickHealthCheckAsync();

        // Assert
        Assert.Equal("Qdrant", result.ComponentName);
        Assert.Equal("Healthy", result.Status);
        Assert.True(result.IsConnected);
        Assert.Equal("Vector store connection successful", result.StatusMessage);
    }

    [Fact]
    public async Task QuickHealthCheckAsync_WithSlowResponse_ShouldReturnWarning()
    {
        // Arrange
        _mockVectorStore.Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) =>
            {
                await Task.Delay(6000, ct); // 6 second delay
                return new List<string> { "collection1" };
            });

        // Act
        var result = await _healthChecker.QuickHealthCheckAsync();

        // Assert
        Assert.Equal("Warning", result.Status);
        Assert.Contains("Slow response", result.StatusMessage);
    }

    [Fact]
    public async Task QuickHealthCheckAsync_WithConnectionError_ShouldReturnCritical()
    {
        // Arrange
        _mockVectorStore.Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        // Act
        var result = await _healthChecker.QuickHealthCheckAsync();

        // Assert
        Assert.Equal("Critical", result.Status);
        Assert.False(result.IsConnected);
        Assert.Contains("Connection failed", result.StatusMessage);
    }

    [Fact]
    public async Task GetComponentMetricsAsync_ShouldReturnVectorStoreMetrics()
    {
        // Arrange
        var collections = new List<string> { "docs", "knowledge", "embeddings" };
        _mockVectorStore.Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        // Act
        var metrics = await _healthChecker.GetComponentMetricsAsync();

        // Assert
        Assert.NotEmpty(metrics);
        Assert.Contains("CollectionCount", metrics.Keys);
        Assert.Contains("Collections", metrics.Keys);
        Assert.Contains("EstimatedVectorCount", metrics.Keys);
        Assert.Contains("ConnectionStatus", metrics.Keys);
        Assert.Contains("LastConnectionTest", metrics.Keys);
        
        Assert.Equal(3, metrics["CollectionCount"]);
        Assert.Equal(collections, metrics["Collections"]);
        Assert.Equal("Connected", metrics["ConnectionStatus"]);
    }

    [Fact]
    public async Task GetComponentMetricsAsync_WithConnectionError_ShouldIncludeError()
    {
        // Arrange
        _mockVectorStore.Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Request timeout"));

        // Act
        var metrics = await _healthChecker.GetComponentMetricsAsync();

        // Assert
        Assert.Contains("Error", metrics.Keys);
        Assert.Contains("ConnectionStatus", metrics.Keys);
        Assert.Equal("Request timeout", metrics["Error"]);
        Assert.Equal("Failed", metrics["ConnectionStatus"]);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldMeasureResponseTime()
    {
        // Arrange
        _mockVectorStore.Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) =>
            {
                await Task.Delay(100, ct); // Small delay to measure
                return new List<string> { "test" };
            });

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.True(result.ResponseTime.TotalMilliseconds >= 90); // Allow for some variance
        Assert.True(result.ResponseTime.TotalMilliseconds < 1000); // Should be well under 1 second
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_ShouldHandleGracefully()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockVectorStore.Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _healthChecker.CheckHealthAsync(cts.Token);

        // Assert
        Assert.Equal("Critical", result.Status);
        Assert.False(result.IsConnected);
    }
}