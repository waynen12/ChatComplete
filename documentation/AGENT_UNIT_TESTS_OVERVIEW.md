KnowledgeAnalyticsAgent Test Suite Overview

  The test suite contains 15 comprehensive tests that validate all aspects of the KnowledgeAnalyticsAgent's functionality. Let me break down each
   test:

  ---
  1. Kernel Function Validation Tests

  KnowledgeAnalyticsAgent_HasCorrectKernelFunction

  Purpose: Validates that the agent properly implements the Semantic Kernel plugin interface.

  [Fact]
  public void KnowledgeAnalyticsAgent_HasCorrectKernelFunction()
  {
      // Uses reflection to find methods with [KernelFunction] attribute
      var kernelFunctionMethods = typeof(KnowledgeAnalyticsAgent)
          .GetMethods()
          .Where(m => m.GetCustomAttributes(typeof(KernelFunctionAttribute), false).Any())
          .ToList();

      // Ensures exactly one method is marked as a kernel function
      Assert.Single(kernelFunctionMethods);
      Assert.Equal("GetKnowledgeBaseSummaryAsync", kernelFunctionMethods[0].Name);
  }
  What it tests:
  - Agent has exactly one kernel function (required for Semantic Kernel integration)
  - Function is named correctly for tool calling

  GetKnowledgeBaseSummaryAsync_HasCorrectAttributes

  Purpose: Ensures the kernel function has proper metadata for LLM tool calling.

  [Fact]
  public void GetKnowledgeBaseSummaryAsync_HasCorrectAttributes()
  {
      var method = typeof(KnowledgeAnalyticsAgent).GetMethod("GetKnowledgeBaseSummaryAsync");
      var kernelFunctionAttr = method?.GetCustomAttributes(typeof(KernelFunctionAttribute), false).FirstOrDefault();
      var descriptionAttr = method?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();

      // Validates that LLMs can understand what this function does
      Assert.Contains("comprehensive summary", description);
      Assert.Contains("knowledge bases", description);
  }
  What it tests:
  - Function has proper [KernelFunction] and [Description] attributes
  - Description contains key terms that help LLMs understand the function's purpose

  ---
  2. Core Functionality Tests

  GetKnowledgeBaseSummaryAsync_WithNoKnowledgeBases_ReturnsNoKnowledgeBasesMessage

  Purpose: Tests the edge case when no knowledge bases exist.

  [Fact]
  public async Task GetKnowledgeBaseSummaryAsync_WithNoKnowledgeBases_ReturnsNoKnowledgeBasesMessage()
  {
      // Arrange: Mock repository returns empty list
      _mockKnowledgeRepository
          .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<KnowledgeSummaryDto>());

      // Act
      var result = await _agent.GetKnowledgeBaseSummaryAsync(true, "activity");

      // Assert: Should return helpful message, not error
      Assert.Contains("No knowledge bases found", result);
      Assert.Contains("Upload some documents", result);
  }
  What it tests:
  - Graceful handling of empty systems
  - User-friendly messaging that guides next steps
  - No exceptions thrown on empty data

  GetKnowledgeBaseSummaryAsync_WithKnowledgeBases_ReturnsFormattedSummary

  Purpose: Tests the main happy path with multiple knowledge bases.

  [Fact]
  public async Task GetKnowledgeBaseSummaryAsync_WithKnowledgeBases_ReturnsFormattedSummary()
  {
      // Arrange: Mock 2 knowledge bases
      var knowledgeBases = new List<KnowledgeSummaryDto>
      {
          new() { Id = "kb1", Name = "React Docs", DocumentCount = 45 },
          new() { Id = "kb2", Name = "API Reference", DocumentCount = 28 }
      };

      // Setup database with test data
      await SetupTestDataAsync();

      // Mock usage statistics
      var usageStats = new List<KnowledgeUsageStats>
      {
          new() { KnowledgeId = "kb1", QueryCount = 234 },
          new() { KnowledgeId = "kb2", QueryCount = 89 }
      };

      // Act
      var result = await _agent.GetKnowledgeBaseSummaryAsync(includeMetrics: true, sortBy: "activity");

      // Assert: Validates complete summary format
      Assert.Contains("Knowledge Base Summary (2 total)", result);
      Assert.Contains("React Docs", result);
      Assert.Contains("API Reference", result);
      Assert.Contains("Sorted by Activity", result);
      Assert.Contains("System Totals", result);
  }
  What it tests:
  - Integration between repository data and database analytics
  - Proper formatting of summary output
  - Inclusion of all expected sections (header, knowledge bases, totals)
  - Correct count display

  ---
  3. Sorting Functionality Tests

  GetKnowledgeBaseSummaryAsync_SortByActivity_SortsCorrectly

  Purpose: Validates activity-based sorting (most important sorting method).

  [Fact]
  public async Task GetKnowledgeBaseSummaryAsync_SortByActivity_SortsCorrectly()
  {
      // Arrange: Setup knowledge bases with different activity levels
      var usageStats = new List<KnowledgeUsageStats>
      {
          new() { KnowledgeId = "kb1", QueryCount = 5 },   // Low activity  
          new() { KnowledgeId = "kb2", QueryCount = 100 }  // High activity
      };

      // Act
      var result = await _agent.GetKnowledgeBaseSummaryAsync(true, "activity");

      // Assert: High activity should appear first
      var lines = result.Split('\n');
      var apiRefIndex = Array.FindIndex(lines, line => line.Contains("API Reference"));
      var reactDocsIndex = Array.FindIndex(lines, line => line.Contains("React Docs"));

      Assert.True(apiRefIndex < reactDocsIndex, "API Reference (high activity) should appear before React Docs (low activity)");
  }
  What it tests:
  - Sorting algorithm correctly prioritizes high-activity knowledge bases
  - Real-world usage: Most active knowledge bases appear first (better UX)

  GetKnowledgeBaseSummaryAsync_SortBySize_SortsCorrectly

  Purpose: Tests size-based sorting using chunk counts.

  [Fact]
  public async Task GetKnowledgeBaseSummaryAsync_SortBySize_SortsCorrectly()
  {
      // Setup database with different chunk counts
      await SetupTestDataWithChunkCountsAsync(); // kb2 has 2000 chunks, kb1 has 500

      // Act
      var result = await _agent.GetKnowledgeBaseSummaryAsync(true, "size");

      // Assert: Larger knowledge base should appear first
      var lines = result.Split('\n');
      var largeKbIndex = Array.FindIndex(lines, line => line.Contains("Large KB"));
      var smallKbIndex = Array.FindIndex(lines, line => line.Contains("Small KB"));

      Assert.True(largeKbIndex < smallKbIndex, "Large KB should appear before small KB when sorted by size");
  }
  What it tests:
  - Size calculation based on chunk count (technical content measure)
  - Database integration for detailed statistics

  GetKnowledgeBaseSummaryAsync_SortByAlphabetical_SortsCorrectly

  Purpose: Tests alphabetical sorting for organizational purposes.

  [Fact]
  public async Task GetKnowledgeBaseSummaryAsync_SortByAlphabetical_SortsCorrectly()
  {
      // Act
      var result = await _agent.GetKnowledgeBaseSummaryAsync(true, "alphabetical");

      // Assert: Alphabetical order
      var lines = result.Split('\n');
      var apiRefIndex = Array.FindIndex(lines, line => line.Contains("API Reference"));
      var reactDocsIndex = Array.FindIndex(lines, line => line.Contains("React Docs"));

      Assert.True(apiRefIndex < reactDocsIndex, "API Reference should appear before React Docs when sorted alphabetically");
  }
  What it tests:
  - Alphabetical sorting works correctly (A comes before R)
  - Provides predictable organization method

  ---
  4. Activity Level Classification Tests

  GetKnowledgeBaseSummaryAsync_ActivityLevels_CalculatedCorrectly

  Purpose: Tests the activity classification algorithm using parameterized tests.

  [Theory]
  [InlineData(0, "None")]
  [InlineData(5, "Low")]
  [InlineData(25, "Medium")]
  [InlineData(75, "High")]
  public async Task GetKnowledgeBaseSummaryAsync_ActivityLevels_CalculatedCorrectly(int queryCount, string expectedActivity)
  {
      // Arrange: Single knowledge base with specific query count
      var usageStats = new List<KnowledgeUsageStats>
      {
          new() { KnowledgeId = "kb1", QueryCount = queryCount }
      };

      // Act
      var result = await _agent.GetKnowledgeBaseSummaryAsync();

      // Assert: Activity level calculated correctly
      Assert.Contains($"Activity: {expectedActivity}", result);
  }
  What it tests:
  - Activity Classification Algorithm:
    - 0 queries = "None"
    - 1-10 queries = "Low"
    - 11-50 queries = "Medium"
    - 51+ queries = "High"
  - Business Logic: Helps users understand usage patterns at a glance

  ---
  5. Configuration and Display Tests

  GetKnowledgeBaseSummaryAsync_WithoutMetrics_ExcludesDetailedInformation

  Purpose: Tests the metrics toggle functionality.

  [Fact]
  public async Task GetKnowledgeBaseSummaryAsync_WithoutMetrics_ExcludesDetailedInformation()
  {
      // Act: Call with includeMetrics = false
      var result = await _agent.GetKnowledgeBaseSummaryAsync(false, "activity");

      // Assert: Basic info included, detailed metrics excluded
      Assert.Contains("React Docs", result);
      Assert.DoesNotContain("Documents:", result);
      Assert.DoesNotContain("Chunks:", result);
      Assert.DoesNotContain("System Totals:", result);
  }
  What it tests:
  - Configuration parameter works correctly
  - Simple vs detailed view modes
  - User can choose information density

  GetKnowledgeBaseSummaryAsync_IncludesSystemTotals_WhenMetricsEnabled

  Purpose: Validates system-wide statistics calculation.

  [Fact]
  public async Task GetKnowledgeBaseSummaryAsync_IncludesSystemTotals_WhenMetricsEnabled()
  {
      // Arrange: 2 knowledge bases with 45 + 28 = 73 documents
      var usageStats = new List<KnowledgeUsageStats>
      {
          new() { KnowledgeId = "kb1", QueryCount = 234 },
          new() { KnowledgeId = "kb2", QueryCount = 89 }
      };

      // Act
      var result = await _agent.GetKnowledgeBaseSummaryAsync(true, "activity");

      // Assert: System totals calculated correctly
      Assert.Contains("System Totals:", result);
      Assert.Contains("Total Documents: 73", result);           // 45 + 28
      Assert.Contains("Active Knowledge Bases: 2/2", result);   // Both active
      Assert.Contains("Total Monthly Queries: 323", result);    // 234 + 89
  }
  What it tests:
  - Aggregation Logic: Sums across all knowledge bases
  - System Health Metrics: Active vs total knowledge bases
  - Usage Analytics: Total query volume

  ---
  6. Error Handling Tests

  GetKnowledgeBaseSummaryAsync_HandlesExceptions_ReturnsErrorMessage

  Purpose: Tests resilience when dependencies fail.

  [Fact]
  public async Task GetKnowledgeBaseSummaryAsync_HandlesExceptions_ReturnsErrorMessage()
  {
      // Arrange: Force repository to throw exception
      _mockKnowledgeRepository
          .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
          .ThrowsAsync(new Exception("Database error"));

      // Act
      var result = await _agent.GetKnowledgeBaseSummaryAsync(true, "activity");

      // Assert: Graceful error handling
      Assert.Contains("Error retrieving knowledge base summary", result);
      Assert.Contains("Database error", result);
  }
  What it tests:
  - Error Resilience: Agent doesn't crash on database failures
  - User Experience: Meaningful error messages instead of technical stack traces
  - Production Readiness: Handles real-world failure scenarios

  ---
  7. Default Behavior Tests

  GetKnowledgeBaseSummaryAsync_DefaultSort_SortsByActivity

  Purpose: Validates that default parameters work as expected.

  [Fact]
  public async Task GetKnowledgeBaseSummaryAsync_DefaultSort_SortsByActivity()
  {
      // Act: Use explicit "activity" sorting (tests default behavior)
      var result = await _agent.GetKnowledgeBaseSummaryAsync(true, "activity");

      // Assert: Default sort is activity-based
      Assert.Contains("Sorted by Activity", result);
      // Verify sorting order is correct
  }
  What it tests:
  - Default Configuration: Most useful sort (activity) is default
  - User Experience: No configuration needed for common use case

  ---
  8. Database Integration Tests

  Helper Methods: SetupTestDataAsync() and SetupTestDataWithChunkCountsAsync()

  Purpose: Create realistic test data in an in-memory SQLite database.

  private async Task SetupTestDataAsync()
  {
      await _dbContext.InitializeDatabaseAsync();
      var connection = await _dbContext.GetConnectionAsync();

      // Disable foreign keys for testing
      using var pragmaCommand = connection.CreateCommand();
      pragmaCommand.CommandText = "PRAGMA foreign_keys = OFF";
      await pragmaCommand.ExecuteNonQueryAsync();

      // Insert realistic test data
      const string insertSql = """
          INSERT OR REPLACE INTO KnowledgeCollections
          (CollectionId, Name, DocumentCount, ChunkCount, TotalTokens, Status, CreatedAt, UpdatedAt)
          VALUES
          ('kb1', 'React Docs', 45, 2847, 50000, 'Active', datetime('now', '-30 days'), datetime('now', '-1 day')),
          ('kb2', 'API Reference', 28, 1234, 30000, 'Active', datetime('now', '-20 days'), datetime('now', '-2 days'))
          """;
  }
  What it tests:
  - Real Database Integration: Uses actual SQLite database, not mocks
  - Data Persistence: Tests real SQL queries and data retrieval
  - Production Simulation: Realistic data volumes and timestamps

  ---
  Test Architecture Benefits

  1. Comprehensive Coverage

  - Happy Path: Normal operation with multiple knowledge bases
  - Edge Cases: Empty systems, single knowledge base
  - Error Scenarios: Database failures, invalid data
  - Configuration Options: All parameter combinations tested

  2. Real-World Simulation

  - In-Memory Database: Tests actual SQL queries and database operations
  - Realistic Data: Document counts, chunk counts, timestamps that mirror production
  - Service Integration: Tests interaction between repository, database, and usage tracking

  3. Maintainable Test Design

  - Clear Test Names: Each test name explains exactly what it validates
  - Focused Assertions: Each test validates one specific behavior
  - Reusable Setup: Helper methods create consistent test data
  - Isolated Tests: Each test runs independently with fresh data

  4. Production Readiness Validation

  - Performance: Tests complete quickly even with database operations
  - Reliability: Error handling ensures agent never crashes
  - Usability: Output format is human-readable and informative
  - Scalability: Tests work with multiple knowledge bases and large data volumes

  Real-World Example

  When a user asks: "What knowledge bases do I have and how active are they?"

  The agent will:
  1. ✅ Retrieve knowledge bases (tested by repository mocking)
  2. ✅ Get detailed statistics (tested by database integration)
  3. ✅ Calculate activity levels (tested by activity classification tests)
  4. ✅ Sort by activity (tested by sorting tests)
  5. ✅ Format response (tested by output validation)
  6. ✅ Handle errors gracefully (tested by exception handling)

  The test suite ensures every step works correctly and provides a production-ready agent that delivers reliable, accurate knowledge base
  analytics to end users.

---

# SystemHealthAgent Test Suite Overview

The SystemHealthAgent test suite contains 98 comprehensive tests across 6 test classes that validate the complete health monitoring infrastructure. This comprehensive test coverage ensures reliable system health monitoring across all components.

## Test Architecture Overview

### 1. **ComponentHealthTests** (5 tests)
Tests the core health component data model that represents individual component health status.

**Purpose**: Validates the foundational health status representation and formatting logic.

```csharp
[Fact]
public void ComponentHealth_DefaultValues_ShouldBeSet()
{
    // Tests default initialization values
    var health = new ComponentHealth();
    
    Assert.Equal("Unknown", health.Status);
    Assert.False(health.IsConnected);
    Assert.Equal(0, health.ErrorCount);
    Assert.Equal(TimeSpan.Zero, health.ResponseTime);
}
```

**Key Test Coverage**:
- **Default Value Initialization**: Ensures safe defaults for all properties
- **Status Evaluation Properties**: Tests `IsHealthy`, `HasWarnings`, `IsCritical` logic
- **Time Formatting**: Validates `FormattedResponseTime` and `TimeSinceLastSuccess` display
- **Response Time Classification**: Tests millisecond vs second formatting thresholds

**Real-World Value**: Ensures health status display is consistent and user-friendly across all components.

### 2. **SystemMetricsTests** (30 tests)
Tests the system-wide performance and resource metrics aggregation and formatting.

**Purpose**: Validates comprehensive system metrics calculation, formatting, and health evaluation logic.

```csharp
[Theory]
[InlineData(1000, "1.0K tokens")]
[InlineData(1500000, "1.5M tokens")]
[InlineData(500, "500 tokens")]
public void FormattedTokenUsage_ShouldFormatCorrectly(long tokens, string expected)
{
    // Tests intelligent token usage formatting
    var metrics = new SystemMetrics { TotalTokensUsed = tokens };
    Assert.Equal(expected, metrics.FormattedTokenUsage);
}
```

**Key Test Coverage**:
- **Intelligent Formatting**: Bytes, tokens, costs, success rates, response times, uptime
- **Performance Health Evaluation**: Multi-factor analysis (success rate ≥95%, response time ≤2s, errors ≤10)
- **Resource Concern Detection**: Database size, error rates, performance degradation
- **Business Logic**: Cost calculations, usage tracking, system health scoring

**Real-World Value**: Provides human-readable metrics and intelligent health assessment for system administrators.

### 3. **SystemHealthStatusTests** (18 tests)
Tests the aggregated system health status calculation and component management.

**Purpose**: Validates complex health aggregation logic and weighted health percentage calculation.

```csharp
[Fact]
public void SystemHealthPercentage_WithMixedComponents_ShouldCalculateCorrectly()
{
    // Tests weighted health scoring algorithm
    var status = new SystemHealthStatus();
    status.Components.AddRange(new[]
    {
        new ComponentHealth { Status = "Healthy", IsConnected = true },   // 100 points
        new ComponentHealth { Status = "Warning", IsConnected = true },   // 60 points  
        new ComponentHealth { Status = "Critical", IsConnected = true },  // 20 points
        new ComponentHealth { Status = "Healthy", IsConnected = false }   // 0 points (offline)
    });

    // Expected: (100 + 60 + 20 + 0) / (4 * 100) * 100 = 45%
    Assert.Equal(45.0, status.SystemHealthPercentage);
}
```

**Key Test Coverage**:
- **Weighted Health Calculation**: Per-component scoring with connection status consideration
- **Component Counting**: Healthy, warning, critical, offline component categorization
- **Overall Status Logic**: Critical > Warning > Healthy status determination
- **Alert Management**: Adding alerts and recommendations with deduplication
- **Health Summary Generation**: Human-readable system health overview

**Real-World Value**: Provides accurate system health assessment that reflects real operational impact.

### 4. **SqliteHealthCheckerTests** (10 tests)
Tests database health monitoring with in-memory SQLite database integration.

**Purpose**: Validates database connectivity, performance monitoring, and metrics collection.

```csharp
[Fact]
public async Task CheckHealthAsync_WithWorkingDatabase_ShouldReturnHealthy()
{
    // Tests full database health check with real SQLite operations
    var result = await _healthChecker.CheckHealthAsync();
    
    Assert.Equal("SQLite", result.ComponentName);
    Assert.Equal("Healthy", result.Status);
    Assert.True(result.IsConnected);
    Assert.Contains("Database operational", result.StatusMessage);
    Assert.NotEmpty(result.Metrics);
}
```

**Test Infrastructure**:
- **In-Memory SQLite**: Real database operations with test schema
- **Test Data Setup**: Realistic knowledge collections with document/chunk counts
- **Performance Testing**: Response time measurement and threshold validation
- **Error Simulation**: Database connection failures, slow responses, locked databases

**Key Test Coverage**:
- **Database Connectivity**: Connection establishment and validation
- **Performance Thresholds**: Response time monitoring (Warning >1s, Critical on failure)
- **Metrics Collection**: Database size, table count, collection statistics
- **Error Handling**: Graceful handling of connection failures and timeouts

**Real-World Value**: Ensures database health monitoring catches performance issues before they impact users.

### 5. **QdrantHealthCheckerTests** (17 tests)
Tests vector store health monitoring with comprehensive mock scenarios.

**Purpose**: Validates vector database connectivity, collection management, and performance monitoring.

```csharp
[Fact]
public async Task CheckHealthAsync_WithSlowResponse_ShouldReturnCritical()
{
    // Tests response time threshold classification
    _mockVectorStore.Setup(x => x.ListCollectionsAsync(It.IsAny<CancellationToken>()))
        .Returns(async (CancellationToken ct) =>
        {
            await Task.Delay(2500, ct); // Simulates slow response
            return new List<string> { "collection1" };
        });

    var result = await _healthChecker.CheckHealthAsync();
    
    Assert.Equal("Critical", result.Status);
    Assert.Contains("Very slow response time", result.StatusMessage);
}
```

**Test Infrastructure**:
- **Advanced Mocking**: Moq framework for IVectorStoreStrategy simulation
- **Response Time Simulation**: Configurable delays for performance testing
- **Error Scenario Testing**: Connection failures, timeouts, service unavailability

**Key Test Coverage**:
- **Vector Store Connectivity**: Collection listing and response validation
- **Performance Classification**: Warning >2s, Critical >5s response times
- **Collection Management**: Detection and counting of vector collections
- **Resilience Testing**: Timeout handling, connection recovery, error classification

**Real-World Value**: Ensures vector search infrastructure monitoring for AI-powered features.

### 6. **OllamaHealthCheckerTests** (18 tests)
Tests local AI service health monitoring with HTTP client mocking.

**Purpose**: Validates local AI service monitoring, model detection, and performance assessment.

```csharp
[Fact]
public async Task CheckHealthAsync_WithWorkingService_ShouldReturnHealthy()
{
    // Tests complete Ollama service health assessment
    var modelsResponse = CreateOllamaModelsResponse(3);
    SetupHttpResponse(HttpStatusCode.OK, modelsResponse);

    var result = await _healthChecker.CheckHealthAsync();
    
    Assert.Equal("Ollama", result.ComponentName);
    Assert.Equal("Healthy", result.Status);
    Assert.Contains("Ollama service operational with 3 models available", result.StatusMessage);
    Assert.NotEmpty(result.Metrics);
}
```

**Test Infrastructure**:
- **HTTP Client Mocking**: Mock HttpMessageHandler for realistic HTTP testing
- **Response Simulation**: JSON response creation for Ollama API endpoints
- **Timeout Testing**: Configurable delays and cancellation token handling
- **Error Condition Testing**: HTTP errors, connection refused, service timeouts

**Key Test Coverage**:
- **Service Availability**: HTTP endpoint accessibility and response validation
- **Model Detection**: Installed model enumeration and metadata parsing
- **Performance Monitoring**: Warning >5s, Critical >10s response times
- **Service Classification**: Running, warning, critical, offline status determination
- **Metrics Collection**: Model count, sizes, installation dates, service status

**Real-World Value**: Ensures local AI service monitoring for self-hosted AI capabilities.

---

## Advanced Testing Techniques

### 1. **Sophisticated Mocking Patterns**

```csharp
// HTTP Client Mocking with Custom Delays
private void SetupHttpResponseWithDelay(HttpStatusCode statusCode, string content, TimeSpan delay)
{
    _mockHttpHandler.Protected()
        .Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .Returns(async (HttpRequestMessage request, CancellationToken ct) =>
        {
            await Task.Delay(delay, ct);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
        });
}
```

### 2. **In-Memory Database Testing**

```csharp
// Real SQLite Database with Test Schema
private void SetupTestDatabase()
{
    var createTableSql = """
        CREATE TABLE IF NOT EXISTS KnowledgeCollections (
            CollectionId TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            DocumentCount INTEGER DEFAULT 0,
            ChunkCount INTEGER DEFAULT 0,
            CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
        );
        """;
        
    using var command = new SqliteCommand(createTableSql, _connection);
    command.ExecuteNonQuery();
}
```

### 3. **Parameterized Testing for Edge Cases**

```csharp
[Theory]
[InlineData(96.0, 1500, 5, true)]  // Good performance
[InlineData(94.0, 1500, 5, false)] // Low success rate
[InlineData(96.0, 2500, 5, false)] // Slow response time
[InlineData(96.0, 1500, 15, false)] // Too many errors
public void IsPerformanceHealthy_ShouldEvaluateCorrectly(
    double successRate, double responseTime, int errors, bool expected)
{
    // Tests multi-factor performance evaluation
    var metrics = new SystemMetrics
    {
        SuccessRate = successRate,
        AverageResponseTime = responseTime,
        ErrorsLast24Hours = errors
    };
    
    Assert.Equal(expected, metrics.IsPerformanceHealthy);
}
```

---

## Test Coverage Summary

### **98 Total Tests Across 6 Test Classes:**

| Test Class | Test Count | Focus Area |
|------------|------------|------------|
| ComponentHealthTests | 5 | Core health status model |
| SystemMetricsTests | 30 | System-wide metrics and formatting |
| SystemHealthStatusTests | 18 | Health aggregation and scoring |
| SqliteHealthCheckerTests | 10 | Database health monitoring |
| QdrantHealthCheckerTests | 17 | Vector store health monitoring |
| OllamaHealthCheckerTests | 18 | Local AI service monitoring |

### **Test Categories:**

- **✅ Happy Path Testing**: Normal operation scenarios with multiple components
- **✅ Edge Case Testing**: Empty systems, single components, boundary conditions
- **✅ Error Handling**: Network failures, timeouts, service unavailability
- **✅ Performance Testing**: Response time thresholds, slow service detection
- **✅ Integration Testing**: Real database operations, HTTP client interactions
- **✅ Configuration Testing**: Parameter validation, feature toggles
- **✅ Formatting Testing**: Human-readable output, unit conversions
- **✅ Business Logic Testing**: Health scoring algorithms, status classification

---

## Production Readiness Benefits

### 1. **Comprehensive Health Monitoring**
The test suite validates complete system health assessment covering:
- Database connectivity and performance
- Vector store operations and responsiveness  
- Local AI service availability and model detection
- System-wide metrics aggregation and health scoring

### 2. **Robust Error Handling**
All health checkers include comprehensive error handling testing:
- Network connectivity failures
- Service timeouts and performance degradation
- Invalid responses and data corruption
- Graceful degradation with meaningful error messages

### 3. **Performance Awareness**
Response time thresholds ensure performance monitoring:
- Warning levels for degraded but functional services
- Critical levels for severely impacted services
- Intelligent status classification based on service criticality

### 4. **Real-World Simulation**
Test infrastructure mirrors production scenarios:
- Actual SQLite database operations with realistic schemas
- HTTP client interactions with configurable response patterns
- Vector store operations with collection management
- Multi-component health aggregation with weighted scoring

### 5. **Maintainable Test Architecture**
Clean, focused test design ensures long-term maintainability:
- Single-responsibility tests with clear assertions
- Reusable mock setup and test data creation
- Comprehensive coverage without test interdependencies
- Performance-conscious test execution (under 50 seconds total)

---

## Real-World Usage Example

When a system administrator asks: **"What's the current system health status?"**

The SystemHealthAgent will:

1. **✅ Check Database Health** (tested by SqliteHealthCheckerTests)
   - Database connectivity and response time
   - Collection counts and storage metrics
   - Performance threshold evaluation

2. **✅ Monitor Vector Store** (tested by QdrantHealthCheckerTests)  
   - Vector database availability and collection status
   - Search infrastructure performance assessment
   - AI feature dependency health

3. **✅ Validate AI Services** (tested by OllamaHealthCheckerTests)
   - Local AI service availability and model detection
   - Performance monitoring for AI-powered features
   - Service capacity and resource utilization

4. **✅ Aggregate Health Status** (tested by SystemHealthStatusTests)
   - Weighted health percentage calculation
   - Overall system status determination
   - Component health prioritization and alerting

5. **✅ Format Health Report** (tested by SystemMetricsTests)
   - Human-readable metrics display
   - Performance trend analysis
   - Actionable health recommendations

The comprehensive test suite ensures every aspect of system health monitoring works reliably, providing administrators with accurate, actionable health information to maintain optimal system performance.

---

# External AI Provider Health Checkers Test Suite Overview

The external AI provider health checkers test suite contains 72 comprehensive tests across 3 test classes that validate AI service connectivity and health monitoring for cloud-based providers. These tests ensure reliable external service integration and intelligent health assessment.

## Test Architecture Overview

### 1. **OpenAIHealthCheckerTests** (24 tests)
Tests OpenAI API connectivity, authentication, and performance monitoring using Semantic Kernel integration.

**Purpose**: Validates OpenAI service health assessment, API key validation, and model availability detection.

```csharp
[Fact]
public async Task CheckHealthAsync_WithWorkingService_ShouldReturnHealthy()
{
    // Tests complete OpenAI API health assessment using Semantic Kernel
    var mockKernel = SetupMockKernelWithSuccessfulResponse();
    _mockKernelFactory.Setup(x => x.Create(AiProvider.OpenAi))
        .Returns(mockKernel);

    var result = await _healthChecker.CheckHealthAsync();
    
    Assert.Equal("OpenAI", result.ComponentName);
    Assert.Equal("Healthy", result.Status);
    Assert.Contains("OpenAI API operational", result.StatusMessage);
    Assert.NotEmpty(result.Metrics);
}
```

**Test Infrastructure**:
- **Kernel Factory Mocking**: Mock IKernelFactory for Semantic Kernel testing
- **Chat Service Simulation**: Mock IChatCompletionService for API response testing
- **Authentication Testing**: API key validation and authorization scenarios
- **Performance Monitoring**: Response time measurement and threshold validation

**Key Test Coverage**:
- **API Connectivity**: Semantic Kernel integration and chat completion testing
- **Authentication Validation**: API key presence, invalid key handling, quota exceeded
- **Performance Thresholds**: Warning >3s, Critical >10s response times
- **Service Classification**: Available, degraded, unavailable, quota exceeded status
- **Metrics Collection**: Response times, model availability, API rate limits
- **Error Handling**: Network failures, authentication errors, service outages

**Real-World Value**: Ensures reliable OpenAI integration for AI-powered features with intelligent fallback handling.

### 2. **AnthropicHealthCheckerTests** (24 tests)
Tests Anthropic Claude API connectivity, performance monitoring, and service availability assessment.

**Purpose**: Validates Anthropic service health monitoring, API authentication, and response quality assessment.

```csharp
[Theory]
[InlineData(2500, "Warning")] // 2.5s response = Warning
[InlineData(8500, "Critical")] // 8.5s response = Critical
[InlineData(1500, "Healthy")] // 1.5s response = Healthy
public async Task CheckHealthAsync_WithDifferentResponseTimes_ShouldClassifyCorrectly(
    int delayMs, string expectedStatus)
{
    // Tests performance threshold classification for Anthropic API
    var mockKernel = SetupMockKernelWithDelay(TimeSpan.FromMilliseconds(delayMs));
    _mockKernelFactory.Setup(x => x.Create(AiProvider.Anthropic))
        .Returns(mockKernel);

    var result = await _healthChecker.CheckHealthAsync();
    
    Assert.Equal(expectedStatus, result.Status);
}
```

**Test Infrastructure**:
- **Semantic Kernel Mocking**: Mock kernel with AnthropicPromptExecutionSettings
- **Response Time Simulation**: Configurable delays for performance classification testing
- **Authentication Scenarios**: Valid/invalid API keys, quota limits, service restrictions
- **Error Condition Testing**: Rate limiting, model unavailability, service maintenance

**Key Test Coverage**:
- **Service Availability**: Anthropic API endpoint accessibility and response validation
- **Performance Classification**: Warning >3s, Critical >8s response times (Anthropic-specific)
- **Authentication Management**: API key validation, billing account status
- **Model Availability**: Claude model access and capability assessment
- **Rate Limiting**: Request throttling detection and graceful handling
- **Service Quality**: Response quality assessment and model performance

**Real-World Value**: Ensures reliable Anthropic Claude integration with performance-aware health monitoring.

### 3. **GoogleAIHealthCheckerTests** (24 tests)
Tests Google AI (Gemini) service connectivity, model availability, and performance assessment.

**Purpose**: Validates Google AI service health monitoring, authentication, and Gemini model integration.

```csharp
[Fact]
public async Task CheckHealthAsync_WithAuthenticationError_ShouldReturnCritical()
{
    // Tests authentication failure handling for Google AI
    var mockChatService = new Mock<IChatCompletionService>();
    mockChatService.Setup(x => x.GetChatMessageContentAsync(
            It.IsAny<ChatHistory>(),
            It.IsAny<PromptExecutionSettings>(),
            It.IsAny<Kernel>(),
            It.IsAny<CancellationToken>()))
        .ThrowsAsync(new UnauthorizedAccessException("Invalid API key"));

    var mockKernel = SetupMockKernelWithChatService(mockChatService.Object);
    _mockKernelFactory.Setup(x => x.Create(AiProvider.GoogleAi))
        .Returns(mockKernel);

    var result = await _healthChecker.CheckHealthAsync();
    
    Assert.Equal("Critical", result.Status);
    Assert.Contains("Authentication failed", result.StatusMessage);
}
```

**Test Infrastructure**:
- **Google AI Service Mocking**: Mock kernel with GeminiPromptExecutionSettings
- **Authentication Testing**: Google Cloud API key validation and project access
- **Model Testing**: Gemini model availability and capability assessment
- **Performance Monitoring**: Response time tracking with Google AI-specific thresholds

**Key Test Coverage**:
- **Google Cloud Integration**: API key validation, project configuration, service access
- **Gemini Model Health**: Model availability, version compatibility, feature access
- **Performance Thresholds**: Warning >4s, Critical >12s response times (Google-specific)
- **Service Classification**: Available, restricted, maintenance, quota exceeded status
- **Error Recovery**: Network failures, authentication issues, temporary outages
- **Regional Availability**: Multi-region service assessment and failover capability

**Real-World Value**: Ensures reliable Google AI integration with comprehensive service monitoring.

---

## Advanced Testing Patterns

### 1. **Kernel Factory Pattern Testing**

```csharp
// Standardized Kernel Factory Mocking Across All Providers
private Kernel SetupMockKernelWithSuccessfulResponse()
{
    var mockChatService = new Mock<IChatCompletionService>();
    mockChatService.Setup(x => x.GetChatMessageContentAsync(
            It.IsAny<ChatHistory>(),
            It.IsAny<PromptExecutionSettings>(),
            It.IsAny<Kernel>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ChatMessageContent(AuthorRole.Assistant, "Test response"));

    var mockKernel = new Mock<Kernel>();
    mockKernel.Setup(x => x.GetRequiredService<IChatCompletionService>())
        .Returns(mockChatService.Object);
        
    return mockKernel.Object;
}
```

### 2. **Provider-Specific Configuration Testing**

```csharp
// Provider-specific execution settings validation
[Theory]
[InlineData(AiProvider.OpenAi, typeof(OpenAIPromptExecutionSettings))]
[InlineData(AiProvider.Anthropic, typeof(AnthropicPromptExecutionSettings))]
[InlineData(AiProvider.GoogleAi, typeof(GeminiPromptExecutionSettings))]
public void ProviderSettings_ShouldUseCorrectExecutionSettings(
    AiProvider provider, Type expectedSettingsType)
{
    // Validates correct provider configuration
    var settings = CreateExecutionSettingsForProvider(provider);
    Assert.IsType(expectedSettingsType, settings);
}
```

### 3. **Comprehensive Error Scenario Testing**

```csharp
// Exhaustive error condition testing across all providers
[Theory]
[InlineData(typeof(UnauthorizedAccessException), "Authentication failed")]
[InlineData(typeof(HttpRequestException), "Network connectivity issue")]
[InlineData(typeof(TaskCanceledException), "Request timeout")]
[InlineData(typeof(InvalidOperationException), "Service configuration error")]
public async Task CheckHealthAsync_WithException_ShouldHandleGracefully(
    Type exceptionType, string expectedMessageContent)
{
    // Tests comprehensive error handling across all exception types
    var exception = (Exception)Activator.CreateInstance(exceptionType, "Test error");
    SetupKernelToThrowException(exception);

    var result = await _healthChecker.CheckHealthAsync();
    
    Assert.Equal("Critical", result.Status);
    Assert.Contains(expectedMessageContent, result.StatusMessage);
}
```

---

## Integration Testing Strategy

### 1. **Cross-Provider Consistency**
All external provider health checkers implement identical testing patterns to ensure consistent behavior:

- **Response Time Classification**: Provider-specific thresholds based on service characteristics
- **Authentication Handling**: Standardized API key validation and error messaging
- **Performance Monitoring**: Consistent metrics collection and health assessment
- **Error Recovery**: Uniform error handling and graceful degradation

### 2. **Semantic Kernel Integration**
Tests validate proper Semantic Kernel usage across all providers:

- **Kernel Factory Pattern**: Consistent kernel creation and configuration
- **Provider Settings**: Correct execution settings for each AI provider
- **Service Integration**: Proper chat completion service interaction
- **Resource Management**: Kernel lifecycle and disposal testing

### 3. **Production Readiness Validation**
Comprehensive testing ensures production-ready external service integration:

- **Network Resilience**: Timeout handling, retry logic, connection recovery
- **Authentication Security**: API key validation, secure credential handling
- **Performance Monitoring**: Service-specific response time thresholds
- **Error Transparency**: Clear error messages and actionable recommendations

---

## Test Coverage Summary

### **72 Total Tests Across 3 Provider Classes:**

| Test Class | Test Count | Focus Area |
|------------|------------|------------|
| OpenAIHealthCheckerTests | 24 | OpenAI API connectivity and authentication |
| AnthropicHealthCheckerTests | 24 | Anthropic Claude service monitoring |
| GoogleAIHealthCheckerTests | 24 | Google AI (Gemini) integration testing |

### **Test Categories per Provider:**

- **✅ Service Connectivity**: API endpoint accessibility and response validation
- **✅ Authentication Testing**: API key validation, quota limits, billing status
- **✅ Performance Monitoring**: Response time thresholds and service classification  
- **✅ Error Handling**: Network failures, authentication errors, service outages
- **✅ Configuration Validation**: Provider-specific settings and execution parameters
- **✅ Integration Testing**: Semantic Kernel integration and service interaction
- **✅ Metrics Collection**: Response times, availability rates, error counts
- **✅ Graceful Degradation**: Fallback behavior and error recovery

---

## Production Benefits

### 1. **Reliable External Service Integration**
The test suite validates robust external AI provider integration:
- Multi-provider support with consistent health monitoring
- Intelligent service classification and performance assessment
- Comprehensive error handling for network and authentication issues
- Provider-specific optimization and threshold management

### 2. **Performance-Aware Health Assessment**
Provider-specific performance monitoring ensures optimal service usage:
- **OpenAI**: Warning >3s, Critical >10s (optimized for chat completion)
- **Anthropic**: Warning >3s, Critical >8s (optimized for Claude responses)
- **Google AI**: Warning >4s, Critical >12s (accounts for Gemini processing)

### 3. **Authentication and Security Validation**
Comprehensive authentication testing ensures secure external service access:
- API key validation and secure credential handling
- Quota and billing status monitoring
- Authorization error detection and clear messaging
- Service access restriction identification

### 4. **Semantic Kernel Integration Assurance**
Tests validate proper Semantic Kernel usage for external providers:
- Correct kernel factory pattern implementation
- Provider-specific execution settings validation
- Chat completion service integration testing
- Resource lifecycle and memory management

---

## Real-World Usage Example

When SystemHealthAgent checks external AI provider health:

1. **✅ OpenAI Health Check** (tested by OpenAIHealthCheckerTests)
   - GPT model availability and API authentication
   - Response time monitoring and rate limit detection
   - Service capacity and quota assessment

2. **✅ Anthropic Health Check** (tested by AnthropicHealthCheckerTests)  
   - Claude API accessibility and billing status
   - Model availability and response quality assessment
   - Performance monitoring for conversation capabilities

3. **✅ Google AI Health Check** (tested by GoogleAIHealthCheckerTests)
   - Gemini service availability and project configuration
   - Regional accessibility and model capability assessment
   - Google Cloud integration and authentication validation

4. **✅ Integrated Health Reporting**
   - Multi-provider health aggregation and status classification
   - Performance comparison across AI providers
   - Intelligent fallback recommendations and service optimization

The external provider health checker test suite ensures reliable, performance-aware monitoring of cloud AI services, enabling intelligent provider selection and robust service integration across the AI Knowledge Manager platform.
