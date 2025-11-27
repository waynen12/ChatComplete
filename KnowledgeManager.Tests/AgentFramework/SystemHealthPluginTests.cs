using KnowledgeEngine.Agents.AgentFramework;
using KnowledgeEngine.Agents.Models;
using KnowledgeEngine.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KnowledgeManager.Tests.AgentFramework;

/// <summary>
/// Unit tests for SystemHealthPlugin (Agent Framework version).
/// Verifies tool registration works correctly.
/// </summary>
public class SystemHealthPluginTests
{
    [Fact]
    public void SystemHealthPlugin_ShouldHave_SixPublicMethods()
    {
        // Arrange & Act
        var publicMethods = typeof(SystemHealthPlugin)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(m => m.DeclaringType == typeof(SystemHealthPlugin))
            .ToList();

        // Assert
        Assert.Equal(6, publicMethods.Count);
    }

    [Fact]
    public void AgentToolRegistration_ShouldDiscover_AllSixFunctions()
    {
        // Arrange
        var mockHealthService = new Mock<ISystemHealthService>();
        var mockLogger = new Mock<ILogger<SystemHealthPlugin>>();
        var plugin = new SystemHealthPlugin(mockHealthService.Object, mockLogger.Object);

        // Act
        var tools = AgentToolRegistration.CreateToolsFromPlugin(plugin);

        // Assert
        Assert.Equal(6, tools.Count);
    }

    [Fact]
    public async Task GetSystemHealthAsync_ShouldReturnFormattedReport()
    {
        // Arrange
        var mockHealthService = new Mock<ISystemHealthService>();
        var mockLogger = new Mock<ILogger<SystemHealthPlugin>>();
        var plugin = new SystemHealthPlugin(mockHealthService.Object, mockLogger.Object);

        var mockHealth = new SystemHealthStatus
        {
            OverallStatus = "Healthy",
            Components = new List<ComponentHealth>
            {
                new() { ComponentName = "SQLite", Status = "Healthy", IsConnected = true },
            }
        };

        mockHealthService
            .Setup(s => s.GetSystemHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockHealth);

        // Act
        var result = await plugin.GetSystemHealthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("System Health Status", result);
        Assert.Contains("Healthy", result);
    }

    [Fact]
    public async Task CheckComponentHealthAsync_ShouldReturnComponentStatus()
    {
        // Arrange
        var mockHealthService = new Mock<ISystemHealthService>();
        var mockLogger = new Mock<ILogger<SystemHealthPlugin>>();
        var plugin = new SystemHealthPlugin(mockHealthService.Object, mockLogger.Object);

        var mockComponent = new ComponentHealth
        {
            ComponentName = "SQLite",
            Status = "Healthy",
            IsConnected = true,
        };

        mockHealthService
            .Setup(s => s.CheckComponentHealthAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockComponent);

        // Act
        var result = await plugin.CheckComponentHealthAsync("SQLite");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("SQLite", result);
        Assert.Contains("Healthy", result);
    }

    [Fact]
    public async Task GetSystemMetricsAsync_ShouldReturnMetricsReport()
    {
        // Arrange
        var mockHealthService = new Mock<ISystemHealthService>();
        var mockLogger = new Mock<ILogger<SystemHealthPlugin>>();
        var plugin = new SystemHealthPlugin(mockHealthService.Object, mockLogger.Object);

        var mockMetrics = new SystemMetrics();

        mockHealthService
            .Setup(s => s.GetSystemMetricsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockMetrics);

        // Act
        var result = await plugin.GetSystemMetricsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("System Performance Metrics", result);
    }

    [Fact]
    public async Task GetHealthRecommendationsAsync_ShouldReturnRecommendations()
    {
        // Arrange
        var mockHealthService = new Mock<ISystemHealthService>();
        var mockLogger = new Mock<ILogger<SystemHealthPlugin>>();
        var plugin = new SystemHealthPlugin(mockHealthService.Object, mockLogger.Object);

        var mockHealth = new SystemHealthStatus { OverallStatus = "Healthy" };
        var mockRecommendations = new List<string> { "Test recommendation" };

        mockHealthService
            .Setup(s => s.GetSystemHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockHealth);

        mockHealthService
            .Setup(s => s.GetHealthRecommendationsAsync(It.IsAny<SystemHealthStatus>()))
            .ReturnsAsync(mockRecommendations);

        // Act
        var result = await plugin.GetHealthRecommendationsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("System Health Recommendations", result);
    }

    [Fact]
    public async Task GetAvailableComponentsAsync_ShouldReturnComponentList()
    {
        // Arrange
        var mockHealthService = new Mock<ISystemHealthService>();
        var mockLogger = new Mock<ILogger<SystemHealthPlugin>>();
        var plugin = new SystemHealthPlugin(mockHealthService.Object, mockLogger.Object);

        var mockComponents = new List<string> { "SQLite", "Qdrant", "OpenAI" };

        mockHealthService
            .Setup(s => s.GetAvailableComponentsAsync())
            .ReturnsAsync(mockComponents);

        // Act
        var result = await plugin.GetAvailableComponentsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Available System Components", result);
        Assert.Contains("SQLite", result);
    }

    [Fact]
    public async Task GetQuickHealthOverviewAsync_ShouldReturnQuickSummary()
    {
        // Arrange
        var mockHealthService = new Mock<ISystemHealthService>();
        var mockLogger = new Mock<ILogger<SystemHealthPlugin>>();
        var plugin = new SystemHealthPlugin(mockHealthService.Object, mockLogger.Object);

        var mockHealth = new SystemHealthStatus
        {
            OverallStatus = "Healthy",
            Components = new List<ComponentHealth>(),
        };

        mockHealthService
            .Setup(s => s.GetQuickHealthCheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockHealth);

        // Act
        var result = await plugin.GetQuickHealthOverviewAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("System Status", result);
    }

    [Fact]
    public async Task GetSystemHealthAsync_WithException_ShouldReturnErrorMessage()
    {
        // Arrange
        var mockHealthService = new Mock<ISystemHealthService>();
        var mockLogger = new Mock<ILogger<SystemHealthPlugin>>();
        var plugin = new SystemHealthPlugin(mockHealthService.Object, mockLogger.Object);

        mockHealthService
            .Setup(s => s.GetSystemHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await plugin.GetSystemHealthAsync();

        // Assert
        Assert.Contains("System Health Check Failed", result);
        Assert.Contains("Database connection failed", result);
    }
}
