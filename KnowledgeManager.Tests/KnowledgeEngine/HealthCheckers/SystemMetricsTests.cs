using KnowledgeEngine.Agents.Models;

namespace KnowledgeManager.Tests.KnowledgeEngine.HealthCheckers;

public class SystemMetricsTests
{
    [Fact]
    public void SystemMetrics_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var metrics = new SystemMetrics();

        // Assert
        Assert.Equal(0, metrics.TotalTokensUsed);
        Assert.Equal(0, metrics.EstimatedMonthlyCost);
        Assert.Equal(0, metrics.TotalConversations);
        Assert.Equal(0, metrics.ActiveKnowledgeBases);
        Assert.Equal(0, metrics.DatabaseSizeBytes);
        Assert.Equal(0, metrics.VectorStoreCollections);
        Assert.Equal(0, metrics.AverageResponseTime);
        Assert.Equal(0, metrics.SuccessRate);
        Assert.Equal(0, metrics.ErrorsLast24Hours);
        Assert.Equal(default, metrics.SystemStartTime);
        Assert.Equal(0, metrics.PeakMemoryUsage);
        Assert.Equal(0, metrics.CurrentMemoryUsage);
        Assert.Equal(0, metrics.ActiveConnections);
    }

    [Theory]
    [InlineData(0, "0 bytes")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1073741824, "1.0 GB")]
    [InlineData(1099511627776, "1.0 TB")]
    public void FormattedDatabaseSize_ShouldFormatCorrectly(long bytes, string expected)
    {
        // Arrange
        var metrics = new SystemMetrics { DatabaseSizeBytes = bytes };

        // Act & Assert
        Assert.Equal(expected, metrics.FormattedDatabaseSize);
    }

    [Theory]
    [InlineData(95.5, "95.5%")]
    [InlineData(100.0, "100.0%")]
    [InlineData(0.0, "0.0%")]
    public void FormattedSuccessRate_ShouldFormatCorrectly(double rate, string expected)
    {
        // Arrange
        var metrics = new SystemMetrics { SuccessRate = rate };

        // Act & Assert
        Assert.Equal(expected, metrics.FormattedSuccessRate);
    }

    [Theory]
    [InlineData(500, "500ms")]
    [InlineData(1200, "1.2s")]
    [InlineData(2500, "2.5s")]
    public void FormattedAverageResponseTime_ShouldFormatCorrectly(double milliseconds, string expected)
    {
        // Arrange
        var metrics = new SystemMetrics { AverageResponseTime = milliseconds };

        // Act & Assert
        Assert.Equal(expected, metrics.FormattedAverageResponseTime);
    }

    [Theory]
    [InlineData(1000, "1.0K tokens")]
    [InlineData(1500000, "1.5M tokens")]
    [InlineData(500, "500 tokens")]
    public void FormattedTokenUsage_ShouldFormatCorrectly(long tokens, string expected)
    {
        // Arrange
        var metrics = new SystemMetrics { TotalTokensUsed = tokens };

        // Act & Assert
        Assert.Equal(expected, metrics.FormattedTokenUsage);
    }

    [Theory]
    [InlineData(25.99, "$25.99")]
    [InlineData(0.50, "$0.50")]
    [InlineData(1234.56, "$1234.56")]
    public void FormattedEstimatedCost_ShouldFormatCorrectly(decimal cost, string expected)
    {
        // Arrange
        var metrics = new SystemMetrics { EstimatedMonthlyCost = cost };

        // Act & Assert
        Assert.Equal(expected, metrics.FormattedEstimatedCost);
    }

    [Fact]
    public void FormattedUptime_WhenSystemJustStarted_ShouldShowMinutes()
    {
        // Arrange
        var metrics = new SystemMetrics 
        { 
            SystemStartTime = DateTime.UtcNow.AddMinutes(-30) 
        };

        // Act & Assert
        Assert.Contains("30m", metrics.FormattedUptime);
    }

    [Fact]
    public void FormattedUptime_WhenSystemRunningHours_ShouldShowHoursAndMinutes()
    {
        // Arrange
        var metrics = new SystemMetrics 
        { 
            SystemStartTime = DateTime.UtcNow.AddHours(-2).AddMinutes(-15) 
        };

        // Act & Assert
        Assert.Contains("2h 15m", metrics.FormattedUptime);
    }

    [Fact]
    public void FormattedUptime_WhenSystemRunningDays_ShouldShowDays()
    {
        // Arrange
        var metrics = new SystemMetrics 
        { 
            SystemStartTime = DateTime.UtcNow.AddDays(-3).AddHours(-5) 
        };

        // Act & Assert
        Assert.Contains("3d 5h", metrics.FormattedUptime);
    }

    [Theory]
    [InlineData(96.0, 1500, 5, true)] // Good performance
    [InlineData(94.0, 1500, 5, false)] // Low success rate
    [InlineData(96.0, 2500, 5, false)] // Slow response time
    [InlineData(96.0, 1500, 15, false)] // Too many errors
    public void IsPerformanceHealthy_ShouldEvaluateCorrectly(double successRate, double responseTime, int errors, bool expected)
    {
        // Arrange
        var metrics = new SystemMetrics
        {
            SuccessRate = successRate,
            AverageResponseTime = responseTime,
            ErrorsLast24Hours = errors
        };

        // Act & Assert
        Assert.Equal(expected, metrics.IsPerformanceHealthy);
    }

    [Theory]
    [InlineData(500_000_000, 10, 95.0, false)] // Normal values
    [InlineData(1_500_000_000, 10, 95.0, true)] // Large database
    [InlineData(500_000_000, 60, 95.0, true)] // Many errors
    [InlineData(500_000_000, 10, 85.0, true)] // Low success rate
    public void HasResourceConcerns_ShouldEvaluateCorrectly(long dbSize, int errors, double successRate, bool expected)
    {
        // Arrange
        var metrics = new SystemMetrics
        {
            DatabaseSizeBytes = dbSize,
            ErrorsLast24Hours = errors,
            SuccessRate = successRate
        };

        // Act & Assert
        Assert.Equal(expected, metrics.HasResourceConcerns);
    }

    [Fact]
    public void Uptime_ShouldCalculateCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-2);
        var metrics = new SystemMetrics { SystemStartTime = startTime };

        // Act
        var uptime = metrics.Uptime;

        // Assert
        Assert.True(uptime.TotalHours >= 1.9 && uptime.TotalHours <= 2.1); // Allow for test execution time
    }
}