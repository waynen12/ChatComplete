using System.Net;
using System.Text;
using System.Text.Json;
using KnowledgeEngine.Services.HealthCheckers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace KnowledgeManager.Tests.KnowledgeEngine.HealthCheckers;

public class OllamaHealthCheckerTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<OllamaHealthChecker>> _mockLogger;
    private readonly HttpClient _httpClient;
    private readonly OllamaHealthChecker _healthChecker;

    public OllamaHealthCheckerTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<OllamaHealthChecker>>();

        _httpClient = new HttpClient(_mockHttpHandler.Object);

        // Mock IConfiguration using the indexer
        _mockConfiguration.Setup(x => x["ChatCompleteSettings:OllamaBaseUrl"])
            .Returns("http://localhost:11434");

        _healthChecker = new OllamaHealthChecker(_httpClient, _mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public void ComponentProperties_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("Ollama", _healthChecker.ComponentName);
        Assert.Equal(3, _healthChecker.Priority);
        Assert.False(_healthChecker.IsCriticalComponent);
    }

    [Fact]
    public async Task CheckHealthAsync_WithWorkingService_ShouldReturnHealthy()
    {
        // Arrange
        var modelsResponse = CreateOllamaModelsResponse(3);
        SetupHttpResponse(HttpStatusCode.OK, modelsResponse);

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("Ollama", result.ComponentName);
        Assert.Equal("Healthy", result.Status);
        Assert.True(result.IsConnected);
        Assert.Contains("Ollama service operational with 3 models available", result.StatusMessage);
        Assert.True(result.ResponseTime.TotalMilliseconds >= 0);
        Assert.NotEmpty(result.Metrics);
    }

    [Fact]
    public async Task CheckHealthAsync_WithNoModels_ShouldReturnWarning()
    {
        // Arrange
        var modelsResponse = CreateOllamaModelsResponse(0);
        SetupHttpResponse(HttpStatusCode.OK, modelsResponse);

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("Warning", result.Status);
        Assert.True(result.IsConnected);
        Assert.Contains("No models installed", result.StatusMessage);
    }

    [Fact]
    public async Task CheckHealthAsync_WithSlowResponse_ShouldReturnWarning()
    {
        // Arrange
        var modelsResponse = CreateOllamaModelsResponse(2);
        SetupHttpResponseWithDelay(HttpStatusCode.OK, modelsResponse, TimeSpan.FromSeconds(7));

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("Critical", result.Status);
        Assert.Contains("Very slow response time", result.StatusMessage);
        Assert.True(result.ResponseTime.TotalMilliseconds >= 7000);
    }

    [Fact]
    public async Task CheckHealthAsync_WithVerySlowResponse_ShouldReturnCritical()
    {
        // Arrange
        var modelsResponse = CreateOllamaModelsResponse(2);
        SetupHttpResponseWithDelay(HttpStatusCode.OK, modelsResponse, TimeSpan.FromSeconds(11));

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("Critical", result.Status);
        Assert.Contains("Very slow response time", result.StatusMessage);
        Assert.True(result.ResponseTime.TotalMilliseconds >= 10000);
    }

    [Fact]
    public async Task CheckHealthAsync_WithHttpError_ShouldReturnOffline()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.ServiceUnavailable, "Service unavailable");

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("Offline", result.Status);
        Assert.False(result.IsConnected);
        Assert.Contains("Ollama service not reachable", result.StatusMessage);
    }

    [Fact]
    public async Task CheckHealthAsync_WithConnectionRefused_ShouldReturnOffline()
    {
        // Arrange
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("Offline", result.Status);
        Assert.False(result.IsConnected);
        Assert.Contains("Ollama service not reachable", result.StatusMessage);
    }

    [Fact]
    public async Task CheckHealthAsync_WithTimeout_ShouldReturnCritical()
    {
        // Arrange
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("Critical", result.Status);
        Assert.False(result.IsConnected);
        Assert.Contains("Ollama service timeout", result.StatusMessage);
    }

    [Fact]
    public async Task QuickHealthCheckAsync_WithWorkingService_ShouldReturnHealthy()
    {
        // Arrange
        var modelsResponse = CreateOllamaModelsResponse(5);
        SetupHttpResponse(HttpStatusCode.OK, modelsResponse);

        // Act
        var result = await _healthChecker.QuickHealthCheckAsync();

        // Assert
        Assert.Equal("Ollama", result.ComponentName);
        Assert.Equal("Healthy", result.Status);
        Assert.True(result.IsConnected);
        Assert.Contains("Service accessible with 5 models", result.StatusMessage);
    }

    [Fact]
    public async Task QuickHealthCheckAsync_WithSlowResponse_ShouldReturnWarning()
    {
        // Arrange
        var modelsResponse = CreateOllamaModelsResponse(2);
        SetupHttpResponseWithDelay(HttpStatusCode.OK, modelsResponse, TimeSpan.FromSeconds(11));

        // Act
        var result = await _healthChecker.QuickHealthCheckAsync();

        // Assert
        Assert.Equal("Warning", result.Status);
        Assert.Contains("Slow response", result.StatusMessage);
    }

    [Fact]
    public async Task GetComponentMetricsAsync_ShouldReturnOllamaMetrics()
    {
        // Arrange
        var modelsResponse = CreateOllamaModelsResponse(3);
        SetupHttpResponse(HttpStatusCode.OK, modelsResponse);

        // Act
        var metrics = await _healthChecker.GetComponentMetricsAsync();

        // Assert
        Assert.NotEmpty(metrics);
        Assert.Contains("ModelCount", metrics.Keys);
        Assert.Contains("InstalledModels", metrics.Keys);
        Assert.Contains("TotalModelSizeBytes", metrics.Keys);
        Assert.Contains("FormattedTotalSize", metrics.Keys);
        Assert.Contains("ServiceStatus", metrics.Keys);
        
        Assert.Equal(3, metrics["ModelCount"]);
        Assert.Equal("Running", metrics["ServiceStatus"]);
        
        var modelNames = (List<string>)metrics["InstalledModels"];
        Assert.Equal(3, modelNames.Count);
        Assert.Contains("llama3.2:3b", modelNames);
    }

    [Fact]
    public async Task GetComponentMetricsAsync_WithLargeModels_ShouldIncludeModelInfo()
    {
        // Arrange
        var modelsResponse = CreateOllamaModelsResponse(2, includeMetadata: true);
        SetupHttpResponse(HttpStatusCode.OK, modelsResponse);

        // Act
        var metrics = await _healthChecker.GetComponentMetricsAsync();

        // Assert
        Assert.Contains("NewestModel", metrics.Keys);
        Assert.Contains("OldestModel", metrics.Keys);
        Assert.Contains("LargestModel", metrics.Keys);
    }

    [Fact]
    public async Task GetComponentMetricsAsync_WithConnectionError_ShouldIncludeError()
    {
        // Arrange
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var metrics = await _healthChecker.GetComponentMetricsAsync();

        // Assert
        Assert.Contains("Error", metrics.Keys);
        Assert.Contains("ServiceStatus", metrics.Keys);
        Assert.Equal("Network error", metrics["Error"]);
        Assert.Equal("Error", metrics["ServiceStatus"]);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupHttpResponseWithDelay(HttpStatusCode statusCode, string content, TimeSpan delay)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken ct) =>
            {
                await Task.Delay(delay, ct);
                return response;
            });
    }

    private static string CreateOllamaModelsResponse(int modelCount, bool includeMetadata = false)
    {
        var models = new List<object>();

        for (int i = 0; i < modelCount; i++)
        {
            var model = new
            {
                name = $"llama3.2:{(i + 1) * 3}b",
                size = (long)(i + 1) * 1024 * 1024 * 1024, // GB sizes
                modified_at = DateTime.UtcNow.AddDays(-i).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
            models.Add(model);
        }

        var response = new { models };
        return JsonSerializer.Serialize(response, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
        });
    }
}