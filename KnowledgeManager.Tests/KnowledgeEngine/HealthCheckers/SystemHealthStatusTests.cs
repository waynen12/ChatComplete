using KnowledgeEngine.Agents.Models;

namespace KnowledgeManager.Tests.KnowledgeEngine.HealthCheckers;

public class SystemHealthStatusTests
{
    [Fact]
    public void SystemHealthStatus_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var status = new SystemHealthStatus();

        // Assert
        Assert.Equal("Unknown", status.OverallStatus);
        Assert.True(status.LastChecked <= DateTime.UtcNow);
        Assert.NotNull(status.Components);
        Assert.Empty(status.Components);
        Assert.NotNull(status.Metrics);
        Assert.NotNull(status.ActiveAlerts);
        Assert.Empty(status.ActiveAlerts);
        Assert.NotNull(status.Recommendations);
        Assert.Empty(status.Recommendations);
    }

    [Fact]
    public void ComponentCounts_WithMixedComponents_ShouldCalculateCorrectly()
    {
        // Arrange
        var status = new SystemHealthStatus();
        status.Components.AddRange(new[]
        {
            new ComponentHealth { Status = "Healthy", IsConnected = true },
            new ComponentHealth { Status = "Healthy", IsConnected = true },
            new ComponentHealth { Status = "Warning", IsConnected = true },
            new ComponentHealth { Status = "Critical", IsConnected = true },
            new ComponentHealth { Status = "Healthy", IsConnected = false }
        });

        // Act & Assert
        Assert.Equal(3, status.HealthyComponents); // All healthy components (connected + disconnected)
        Assert.Equal(1, status.ComponentsWithWarnings);
        Assert.Equal(1, status.CriticalComponents);
        Assert.Equal(1, status.OfflineComponents);
    }

    [Fact]
    public void SystemHealthPercentage_WithMixedComponents_ShouldCalculateCorrectly()
    {
        // Arrange
        var status = new SystemHealthStatus();
        status.Components.AddRange(new[]
        {
            new ComponentHealth { Status = "Healthy", IsConnected = true }, // 100 points
            new ComponentHealth { Status = "Warning", IsConnected = true }, // 60 points
            new ComponentHealth { Status = "Critical", IsConnected = true }, // 20 points
            new ComponentHealth { Status = "Healthy", IsConnected = false } // 0 points
        });

        // Act
        var percentage = status.SystemHealthPercentage;

        // Assert
        // Total: 180 out of 400 possible = 45%
        Assert.Equal(45.0, percentage);
    }

    [Fact]
    public void UpdateOverallStatus_WithCriticalComponents_ShouldSetCritical()
    {
        // Arrange
        var status = new SystemHealthStatus();
        status.Components.Add(new ComponentHealth { Status = "Critical" });

        // Act
        status.UpdateOverallStatus();

        // Assert
        Assert.Equal("Critical", status.OverallStatus);
    }

    [Fact]
    public void UpdateOverallStatus_WithWarningComponents_ShouldSetWarning()
    {
        // Arrange
        var status = new SystemHealthStatus();
        status.Components.AddRange(new[]
        {
            new ComponentHealth { Status = "Healthy", IsConnected = true },
            new ComponentHealth { Status = "Warning", IsConnected = true }
        });

        // Act
        status.UpdateOverallStatus();

        // Assert
        Assert.Equal("Warning", status.OverallStatus);
    }

    [Fact]
    public void UpdateOverallStatus_WithAllHealthyComponents_ShouldSetHealthy()
    {
        // Arrange
        var status = new SystemHealthStatus();
        status.Components.AddRange(new[]
        {
            new ComponentHealth { Status = "Healthy", IsConnected = true },
            new ComponentHealth { Status = "Healthy", IsConnected = true }
        });

        // Act
        status.UpdateOverallStatus();

        // Assert
        Assert.Equal("Healthy", status.OverallStatus);
    }

    [Fact]
    public void GetComponentHealth_WithExistingComponent_ShouldReturnComponent()
    {
        // Arrange
        var status = new SystemHealthStatus();
        var component = new ComponentHealth { ComponentName = "TestComponent" };
        status.Components.Add(component);

        // Act
        var result = status.GetComponentHealth("TestComponent");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestComponent", result.ComponentName);
    }

    [Fact]
    public void GetComponentHealth_WithNonExistentComponent_ShouldReturnNull()
    {
        // Arrange
        var status = new SystemHealthStatus();

        // Act
        var result = status.GetComponentHealth("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AddAlert_WithNewAlert_ShouldAddToList()
    {
        // Arrange
        var status = new SystemHealthStatus();

        // Act
        status.AddAlert("Test alert");

        // Assert
        Assert.Single(status.ActiveAlerts);
        Assert.Contains("Test alert", status.ActiveAlerts);
    }

    [Fact]
    public void AddAlert_WithDuplicateAlert_ShouldNotAddDuplicate()
    {
        // Arrange
        var status = new SystemHealthStatus();
        status.AddAlert("Test alert");

        // Act
        status.AddAlert("Test alert");

        // Assert
        Assert.Single(status.ActiveAlerts);
    }

    [Fact]
    public void AddRecommendation_WithNewRecommendation_ShouldAddToList()
    {
        // Arrange
        var status = new SystemHealthStatus();

        // Act
        status.AddRecommendation("Test recommendation");

        // Assert
        Assert.Single(status.Recommendations);
        Assert.Contains("Test recommendation", status.Recommendations);
    }

    [Theory]
    [InlineData("Healthy", 0, 80.0, true)]
    [InlineData("Warning", 0, 85.0, false)]
    [InlineData("Healthy", 1, 80.0, false)]
    [InlineData("Healthy", 0, 72.0, false)]
    public void IsSystemHealthy_ShouldEvaluateCorrectly(string status, int criticalComponents, double healthPercentage, bool expected)
    {
        // Arrange
        var systemStatus = new SystemHealthStatus { OverallStatus = status };
        
        // Add components to achieve the desired health percentage
        // Calculate component mix to achieve target percentage
        if (healthPercentage >= 80.0)
        {
            // 8 healthy, 2 warning = 80%: (8*100 + 2*60) / 1000 = 880/1000 = 88%
            // Use 10 healthy for 100%: (10*100) / 1000 = 100%
            for (int i = 0; i < 10; i++)
                systemStatus.Components.Add(new ComponentHealth { Status = "Healthy", IsConnected = true });
        }
        else if (healthPercentage >= 72.0)
        {
            // 3 healthy, 7 warning = 72%: (3*100 + 7*60) / 1000 = 720/1000 = 72%
            for (int i = 0; i < 3; i++)
                systemStatus.Components.Add(new ComponentHealth { Status = "Healthy", IsConnected = true });
            for (int i = 0; i < 7; i++)
                systemStatus.Components.Add(new ComponentHealth { Status = "Warning", IsConnected = true });
        }
        
        for (int i = 0; i < criticalComponents; i++)
            systemStatus.Components.Add(new ComponentHealth { Status = "Critical", IsConnected = true });

        // Act & Assert
        Assert.Equal(expected, systemStatus.IsSystemHealthy);
    }

    [Fact]
    public void GetComponentsByStatus_ShouldGroupCorrectly()
    {
        // Arrange
        var status = new SystemHealthStatus();
        status.Components.AddRange(new[]
        {
            new ComponentHealth { Status = "Healthy", ComponentName = "Component1" },
            new ComponentHealth { Status = "Healthy", ComponentName = "Component2" },
            new ComponentHealth { Status = "Warning", ComponentName = "Component3" }
        });

        // Act
        var grouped = status.GetComponentsByStatus();

        // Assert
        Assert.Equal(2, grouped["Healthy"].Count);
        Assert.Single(grouped["Warning"]);
        Assert.Equal("Component1", grouped["Healthy"][0].ComponentName);
    }

    [Fact]
    public void GetWorstComponentStatus_ShouldReturnMostSevere()
    {
        // Arrange
        var status = new SystemHealthStatus();
        status.Components.AddRange(new[]
        {
            new ComponentHealth { Status = "Healthy" },
            new ComponentHealth { Status = "Warning" },
            new ComponentHealth { Status = "Critical" }
        });

        // Act
        var worst = status.GetWorstComponentStatus();

        // Assert
        Assert.Equal("Critical", worst);
    }

    [Fact]
    public void HealthSummary_ShouldGenerateCorrectSummary()
    {
        // Arrange
        var status = new SystemHealthStatus
        {
            OverallStatus = "Warning"
        };
        status.Components.AddRange(new[]
        {
            new ComponentHealth { Status = "Healthy", IsConnected = true },
            new ComponentHealth { Status = "Warning", IsConnected = true }
        });
        status.ActiveAlerts.Add("Test alert");
        status.Metrics.ErrorsLast24Hours = 5;

        // Act
        var summary = status.HealthSummary;

        // Assert
        Assert.Contains("Overall: Warning", summary);
        Assert.Contains("1/2 components healthy", summary);
        Assert.Contains("1 active alerts", summary);
        Assert.Contains("5 errors (24h)", summary);
    }
}