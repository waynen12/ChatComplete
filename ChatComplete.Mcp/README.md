# ChatComplete MCP Server

A **Model Context Protocol (MCP) Server** that exposes ChatComplete's system intelligence tools for external integration with AI assistants, monitoring systems, and development tools.

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Running ChatComplete Knowledge.Api (for database access)
- Optional: Qdrant vector database
- Optional: OpenTelemetry collector (Jaeger, Grafana, etc.)

### Build and Run

```bash
# Build the project
dotnet build

# Run with default settings
dotnet run

# Run with specific operations
dotnet run -- --list-tools
dotnet run -- --health-check
dotnet run -- --test-tool get_system_health
```

## 🛠️ Available MCP Tools

### System Health & Monitoring
- **`get_system_health`** - Complete system health overview with recommendations
- **`check_component_health`** - Individual component health (SQLite, Qdrant, AI providers)
- **`get_system_metrics`** - Performance metrics and resource utilization

### Knowledge Management
- **`search_all_knowledge_bases`** - Cross-knowledge search with relevance scoring
- **`get_knowledge_base_summary`** - Analytics and metrics for all knowledge bases

### Model Analytics
- **`get_popular_models`** - Usage-based model popularity rankings
- **`analyze_model_performance`** - Detailed performance analysis for specific models
- **`compare_models`** - Side-by-side model comparisons

## 🔗 External Integration Examples

### Claude Desktop Integration
```json
{
  "mcp": {
    "servers": {
      "chatcomplete": {
        "command": "dotnet",
        "args": ["run", "--project", "/path/to/ChatComplete.Mcp"],
        "env": {
          "CHATCOMPLETE_Logging__LogLevel": "Information"
        }
      }
    }
  }
}
```

### Grafana Dashboard Queries
```bash
# System health monitoring
curl -X POST "mcp://chatcomplete/get_system_health" \
  -d '{"scope": "critical-only", "includeDetailedMetrics": true}'

# Model performance tracking
curl -X POST "mcp://chatcomplete/get_popular_models" \
  -d '{"count": 5, "provider": "all", "period": "weekly"}'
```

### VS Code Extension Usage
```typescript
import { McpClient } from 'model-context-protocol';

const client = new McpClient('chatcomplete');

// Search knowledge bases
const searchResult = await client.callTool('search_all_knowledge_bases', {
  query: 'Docker SSL configuration',
  limit: 3,
  minRelevance: 0.7
});

// Check system health
const healthResult = await client.callTool('get_system_health', {
  scope: 'ai-services',
  includeRecommendations: true
});
```

## ⚙️ Configuration

### appsettings.json
```json
{
  "McpServer": {
    "OpenTelemetry": {
      "OtlpEndpoint": "http://localhost:4317",
      "EnablePrometheusExporter": true,
      "TraceSampleRate": 1.0
    },
    "Logging": {
      "LogLevel": "Information"
    }
  }
}
```

### Environment Variables
```bash
# Database path
CHATCOMPLETE_KnowledgeSettings__DatabasePath="/path/to/knowledge.db"

# OpenTelemetry endpoint
CHATCOMPLETE_McpServer__OpenTelemetry__OtlpEndpoint="http://jaeger:14268/api/traces"

# Log level
CHATCOMPLETE_McpServer__Logging__LogLevel="Debug"
```

## 📊 OpenTelemetry Integration

### Traces
- `ChatComplete.Mcp.Server` - Server operations (tool execution, discovery)
- `ChatComplete.Mcp` - Individual tool execution traces

### Metrics
- `mcp_server_requests_total` - Total server requests by operation and status
- `mcp_server_request_duration_seconds` - Request duration histograms
- `mcp_tool_executions_total` - Tool execution counts by tool and status
- `mcp_tool_execution_duration_seconds` - Tool execution duration

### Example Grafana Dashboard Queries
```promql
# Request rate
rate(mcp_server_requests_total[5m])

# Average tool execution time
rate(mcp_tool_execution_duration_seconds_sum[5m]) / rate(mcp_tool_execution_duration_seconds_count[5m])

# Error rate by tool
rate(mcp_tool_executions_total{status="error"}[5m]) / rate(mcp_tool_executions_total[5m])
```

## 🏥 Health Monitoring

### Health Check Endpoint
```bash
dotnet run -- --health-check
```

### Example Health Response
```json
{
  "status": "healthy",
  "server_name": "chatcomplete",
  "tools_available": 8,
  "uptime_seconds": 3600,
  "tools": [
    "get_system_health",
    "search_all_knowledge_bases",
    "get_popular_models"
  ]
}
```

## 🧪 Testing Tools

### Test Individual Tools
```bash
# Test system health tool
dotnet run -- --test-tool get_system_health

# Test knowledge search
dotnet run -- --test-tool search_all_knowledge_bases

# Test model analytics
dotnet run -- --test-tool get_popular_models
```

### Example Tool Test Output
```
🧪 Testing tool: get_system_health

✅ Tool execution successful:
Duration: 156.78ms
Content: 🏥 Overall System Health: HEALTHY (Score: 94/100)
...
Metadata:
  tool_name: get_system_health
  scope: all
  timestamp: 2025-01-15T10:30:45.123Z
```

## 🔒 Security Features

### API Key Authentication (Optional)
```json
{
  "McpServer": {
    "Security": {
      "EnableApiKeyAuthentication": true,
      "ApiKeys": ["mcp-key-abc123", "mcp-key-def456"]
    }
  }
}
```

### Rate Limiting (Optional)
```json
{
  "McpServer": {
    "Security": {
      "EnableRateLimiting": true,
      "MaxRequestsPerMinute": 60
    }
  }
}
```

## 🐳 Docker Deployment

### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY bin/Release/net8.0/publish/ .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ChatComplete.Mcp.dll"]
```

### Docker Compose Integration
```yaml
services:
  chatcomplete-mcp:
    build: 
      context: .
      dockerfile: ChatComplete.Mcp/Dockerfile
    environment:
      - CHATCOMPLETE_McpServer__OpenTelemetry__OtlpEndpoint=http://jaeger:14268
    depends_on:
      - knowledge-api
      - qdrant
```

## 🤝 Contributing

1. Add new tools by implementing `IMcpToolProvider`
2. Register tools in `ServiceCollectionExtensions.cs`
3. Add appropriate OpenTelemetry instrumentation
4. Include comprehensive error handling
5. Add tool documentation and examples

## 📚 References

- [Model Context Protocol Specification](https://modelcontextprotocol.io/specification/)
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/dotnet/)
- [Grafana MCP Integration](https://grafana.com/docs/grafana-cloud/send-data/traces/mcp-server/)