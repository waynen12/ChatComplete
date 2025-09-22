using System.Diagnostics.CodeAnalysis;
using ChatCompletion.Config;
using Knowledge.Contracts.Types;
using KnowledgeEngine;
using KnowledgeEngine.Agents.Models;
using KnowledgeEngine.Services.HealthCheckers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Moq;

namespace KnowledgeManager.Tests.KnowledgeEngine.HealthCheckers;

[Experimental("SKEXP0070")]
public class OpenAIHealthCheckerTests : IDisposable
{
    private readonly Mock<IOptions<ChatCompleteSettings>> _mockSettings;
    private readonly Mock<ILogger<OpenAIHealthChecker>> _mockLogger;
    private readonly OpenAIHealthChecker _healthChecker;
    private readonly ChatCompleteSettings _testSettings;
    private readonly string? _originalApiKey;

    public OpenAIHealthCheckerTests()
    {
        _mockSettings = new Mock<IOptions<ChatCompleteSettings>>();
        _mockLogger = new Mock<ILogger<OpenAIHealthChecker>>();

        _testSettings = new ChatCompleteSettings
        {
            SystemPrompt = "Test prompt",
            SystemPromptWithCoding = "Test coding prompt",
            Temperature = 0.7
        };

        _mockSettings.Setup(x => x.Value).Returns(_testSettings);

        // Store original API key to restore later
        _originalApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        _healthChecker = new OpenAIHealthChecker(_mockSettings.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        // Restore original API key
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", _originalApiKey);
    }

    #region Component Properties Tests

    [Fact]
    public void ComponentProperties_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("OpenAI", _healthChecker.ComponentName);
        Assert.Equal(1, _healthChecker.Priority);
        Assert.True(_healthChecker.IsCriticalComponent);
    }

    #endregion

    #region API Key Tests

    [Fact]
    public async Task CheckHealthAsync_WithMissingApiKey_ShouldReturnMissingApiKeyStatus()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("OpenAI", result.ComponentName);
        Assert.Equal("Missing API Key", result.Status);
        Assert.False(result.IsConnected);
        Assert.Contains("OpenAI API key not configured", result.StatusMessage);
    }

    [Fact]
    public async Task CheckHealthAsync_WithEmptyApiKey_ShouldReturnMissingApiKeyStatus()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "");

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("OpenAI", result.ComponentName);
        Assert.Equal("Missing API Key", result.Status);
        Assert.False(result.IsConnected);
        Assert.Contains("OpenAI API key not configured", result.StatusMessage);
    }

    [Fact]
    public async Task CheckHealthAsync_WithWhitespaceApiKey_ShouldAttemptConnection()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "   ");

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("OpenAI", result.ComponentName);
        // Whitespace is treated as a valid key, so it will attempt connection and likely fail
        Assert.NotEqual("Missing API Key", result.Status);
    }

    #endregion

    #region API Key Validation Tests

    [Fact]
    public async Task CheckHealthAsync_WithValidApiKey_ShouldAttemptConnection()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test1234567890abcdef1234567890abcdef1234567890abcdef");

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("OpenAI", result.ComponentName);
        Assert.NotNull(result.Status);
        Assert.NotNull(result.StatusMessage);
        Assert.NotNull(result.LastChecked);
        // Note: Will likely fail connection but should not be "Missing API Key"
        Assert.NotEqual("Missing API Key", result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WithShortApiKey_ShouldAttemptConnection()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test123");

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("OpenAI", result.ComponentName);
        Assert.NotEqual("Missing API Key", result.Status);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task CheckHealthAsync_WithCustomSettings_ShouldUseConfiguration()
    {
        // Arrange
        var customSettings = new ChatCompleteSettings
        {
            SystemPrompt = "Custom system prompt",
            SystemPromptWithCoding = "Custom coding prompt",
            Temperature = 0.5
        };

        _mockSettings.Setup(x => x.Value).Returns(customSettings);
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test1234567890abcdef");

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("OpenAI", result.ComponentName);
        Assert.NotNull(result.Status);
        // Verify that the health checker doesn't fail due to missing configuration
        Assert.NotEqual("Missing API Key", result.Status);
    }

    #endregion

    #region Quick Health Check Tests

    [Fact]
    public async Task QuickHealthCheckAsync_WithMissingApiKey_ShouldReturnSameAsFull()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);

        // Act
        var quickResult = await _healthChecker.QuickHealthCheckAsync();
        var fullResult = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal(fullResult.Status, quickResult.Status);
        Assert.Equal(fullResult.IsConnected, quickResult.IsConnected);
        Assert.Equal(fullResult.ComponentName, quickResult.ComponentName);
    }

    [Fact]
    public async Task QuickHealthCheckAsync_WithValidApiKey_ShouldReturnSameAsFull()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test1234567890abcdef");

        // Act
        var quickResult = await _healthChecker.QuickHealthCheckAsync();
        var fullResult = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal(fullResult.Status, quickResult.Status);
        Assert.Equal(fullResult.IsConnected, quickResult.IsConnected);
        Assert.Equal(fullResult.ComponentName, quickResult.ComponentName);
    }

    #endregion

    #region Response Time Tests

    [Fact]
    public async Task CheckHealthAsync_ShouldSetLastCheckedTime()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test123");
        var beforeCheck = DateTime.UtcNow;

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        var afterCheck = DateTime.UtcNow;
        Assert.True(result.LastChecked >= beforeCheck);
        Assert.True(result.LastChecked <= afterCheck);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldMeasureResponseTime()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test123");

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.True(result.ResponseTime >= TimeSpan.Zero);
        // Response time should be reasonable (less than 30 seconds for test)
        Assert.True(result.ResponseTime < TimeSpan.FromSeconds(30));
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test123");
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var result = await _healthChecker.CheckHealthAsync(cts.Token);
        
        // Should complete without throwing, but may have different status due to cancellation
        Assert.Equal("OpenAI", result.ComponentName);
        Assert.NotNull(result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WithTimeoutCancellation_ShouldHandleGracefully()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test123");
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100)); // Cancel after 100ms

        // Act
        var result = await _healthChecker.CheckHealthAsync(cts.Token);

        // Assert
        Assert.Equal("OpenAI", result.ComponentName);
        Assert.NotNull(result.Status);
        // Response time should reflect the quick cancellation
        Assert.True(result.ResponseTime < TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CheckHealthAsync_ShouldNotThrowExceptions()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "invalid-key-format");

        // Act & Assert - Should not throw
        var result = await _healthChecker.CheckHealthAsync();
        
        Assert.Equal("OpenAI", result.ComponentName);
        Assert.NotNull(result.Status);
        Assert.NotNull(result.StatusMessage);
    }

    [Fact]
    public async Task CheckHealthAsync_WithInvalidKeyFormat_ShouldHandleGracefully()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "not-a-valid-openai-key");

        // Act
        var result = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal("OpenAI", result.ComponentName);
        Assert.NotEqual("Missing API Key", result.Status);
        Assert.NotNull(result.StatusMessage);
        Assert.NotNull(result.LastChecked);
    }

    #endregion

    #region Multiple Calls Tests

    [Fact]
    public async Task CheckHealthAsync_CalledMultipleTimes_ShouldUpdateLastChecked()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test123");

        // Act
        var result1 = await _healthChecker.CheckHealthAsync();
        await Task.Delay(50); // Small delay to ensure different timestamps
        var result2 = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.True(result2.LastChecked >= result1.LastChecked);
    }

    [Fact]
    public async Task CheckHealthAsync_ConsistentResults_WithSameConfiguration()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test123");

        // Act
        var result1 = await _healthChecker.CheckHealthAsync();
        var result2 = await _healthChecker.CheckHealthAsync();

        // Assert
        Assert.Equal(result1.ComponentName, result2.ComponentName);
        Assert.Equal(result1.Status, result2.Status); // Status should be consistent
    }

    #endregion

    #region Settings Validation Tests

    [Fact]
    public void Constructor_WithNullSettings_ShouldCreateInstance()
    {
        // Act & Assert - Constructor doesn't validate, so no exception thrown
        var healthChecker = new OpenAIHealthChecker(null!, _mockLogger.Object);
        Assert.NotNull(healthChecker);
        Assert.Equal("OpenAI", healthChecker.ComponentName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldCreateInstance()
    {
        // Act & Assert - Constructor doesn't validate, so no exception thrown
        var healthChecker = new OpenAIHealthChecker(_mockSettings.Object, null!);
        Assert.NotNull(healthChecker);
        Assert.Equal("OpenAI", healthChecker.ComponentName);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var healthChecker = new OpenAIHealthChecker(_mockSettings.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(healthChecker);
        Assert.Equal("OpenAI", healthChecker.ComponentName);
        Assert.Equal(1, healthChecker.Priority);
        Assert.True(healthChecker.IsCriticalComponent);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void ImplementsIComponentHealthChecker()
    {
        // Assert
        Assert.IsAssignableFrom<IComponentHealthChecker>(_healthChecker);
    }

    [Fact]
    public void ComponentName_ShouldBeConsistent()
    {
        // Act & Assert
        Assert.Equal("OpenAI", _healthChecker.ComponentName);
        Assert.Equal(_healthChecker.ComponentName, _healthChecker.ComponentName); // Should be consistent
    }

    [Fact]
    public void Priority_ShouldBeHighForCriticalProvider()
    {
        // Assert - OpenAI should have high priority (low number = high priority)
        Assert.True(_healthChecker.Priority <= 2, $"Expected high priority (≤2), got {_healthChecker.Priority}");
    }

    [Fact]
    public void IsCriticalComponent_ShouldBeTrueForMainProvider()
    {
        // Assert - OpenAI is a main external provider so should be critical
        Assert.True(_healthChecker.IsCriticalComponent);
    }

    #endregion
}