using KnowledgeEngine.Agents.Models;

namespace KnowledgeManager.Tests.KnowledgeEngine.HealthCheckers;

public class ComponentHealthTests
{
    [Fact]
    public void ComponentHealth_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var componentHealth = new ComponentHealth();

        // Assert
        Assert.Equal(string.Empty, componentHealth.ComponentName);
        Assert.Equal("Unknown", componentHealth.Status);
        Assert.Equal(string.Empty, componentHealth.StatusMessage);
        Assert.True(componentHealth.LastChecked <= DateTime.UtcNow);
        Assert.Equal(TimeSpan.Zero, componentHealth.ResponseTime);
        Assert.NotNull(componentHealth.Metrics);
        Assert.Empty(componentHealth.Metrics);
        Assert.Equal(string.Empty, componentHealth.Version);
        Assert.False(componentHealth.IsConnected);
        Assert.Equal(0, componentHealth.ErrorCount);
        Assert.Equal(default, componentHealth.LastSuccess);
    }

    [Theory]
    [InlineData(500, "500ms")]
    [InlineData(1200, "1.2s")]
    [InlineData(2500, "2.5s")]
    public void FormattedResponseTime_ShouldFormatCorrectly(double milliseconds, string expected)
    {
        // Arrange
        var componentHealth = new ComponentHealth
        {
            ResponseTime = TimeSpan.FromMilliseconds(milliseconds)
        };

        // Act & Assert
        Assert.Equal(expected, componentHealth.FormattedResponseTime);
    }

    [Theory]
    [InlineData("Healthy", true, false, false)]
    [InlineData("Warning", false, true, false)]
    [InlineData("Critical", false, false, true)]
    [InlineData("Unknown", false, false, false)]
    public void StatusChecks_ShouldReturnCorrectValues(string status, bool expectedHealthy, bool expectedWarnings, bool expectedCritical)
    {
        // Arrange
        var componentHealth = new ComponentHealth { Status = status };

        // Act & Assert
        Assert.Equal(expectedHealthy, componentHealth.IsHealthy);
        Assert.Equal(expectedWarnings, componentHealth.HasWarnings);
        Assert.Equal(expectedCritical, componentHealth.IsCritical);
    }

    [Fact]
    public void TimeSinceLastSuccess_WhenNeverSucceeded_ShouldReturnNever()
    {
        // Arrange
        var componentHealth = new ComponentHealth { LastSuccess = default };

        // Act & Assert
        Assert.Equal("Never", componentHealth.TimeSinceLastSuccess);
    }

    [Fact]
    public void TimeSinceLastSuccess_WhenRecentSuccess_ShouldReturnJustNow()
    {
        // Arrange
        var componentHealth = new ComponentHealth { LastSuccess = DateTime.UtcNow.AddSeconds(-30) };

        // Act & Assert
        Assert.Equal("Just now", componentHealth.TimeSinceLastSuccess);
    }

    [Theory]
    [InlineData(-5, "5m ago")]
    [InlineData(-90, "1h ago")]
    [InlineData(-1500, "1d ago")]
    public void TimeSinceLastSuccess_ShouldFormatTimeCorrectly(int minutesAgo, string expected)
    {
        // Arrange
        var componentHealth = new ComponentHealth 
        { 
            LastSuccess = DateTime.UtcNow.AddMinutes(minutesAgo) 
        };

        // Act & Assert
        Assert.Equal(expected, componentHealth.TimeSinceLastSuccess);
    }
}