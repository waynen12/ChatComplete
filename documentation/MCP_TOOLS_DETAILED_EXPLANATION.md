# MCP Tools - Detailed Explanation

## Overview
This document provides in-depth explanations of three key MCP tools from the AI Knowledge Manager, describing their purpose, use cases, parameters, and expected outputs.

---

## 1. `get_knowledge_base_summary` - Core Analytics

### Purpose
Provides a **comprehensive overview of all knowledge bases** in the system, including document counts, usage statistics, storage metrics, and activity patterns.

### What It Does
- Lists **all knowledge bases** stored in SQLite
- Retrieves **usage statistics** from the usage tracking database
- Checks for **orphaned collections** (Qdrant collections without SQLite metadata)
- Calculates **storage metrics** and activity levels
- **Sorts results** based on your preference (activity, size, age, alphabetical)

### Use Cases

#### 1. **System Health Monitoring**
```
Scenario: Daily check to see which knowledge bases are being used
Command: @knowledge-mcp Get knowledge base summary sorted by activity
Expected: List showing most recently used knowledge bases first
```

#### 2. **Storage Capacity Planning**
```
Scenario: Identify which knowledge bases are consuming the most storage
Command: @knowledge-mcp Get knowledge base summary sorted by size
Expected: List showing largest knowledge bases first (helps with cleanup decisions)
```

#### 3. **Cleanup Candidates Identification**
```
Scenario: Find old, unused knowledge bases that could be archived
Command: @knowledge-mcp Get knowledge base summary sorted by age
Expected: List showing oldest knowledge bases first (candidates for removal)
```

#### 4. **Organized Content Management**
```
Scenario: Browse all knowledge bases in alphabetical order
Command: @knowledge-mcp Get knowledge base summary sorted alphabetically
Expected: A-Z sorted list for easy navigation
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `includeMetrics` | boolean | `true` | Include detailed metrics (doc counts, usage stats, last access time) |
| `sortBy` | string | `"activity"` | Sort order: `activity`, `size`, `age`, or `alphabetical` |

### Expected Output Structure

```json
{
  "summary": {
    "totalKnowledgeBases": 15,
    "totalDocuments": 234,
    "totalStorageUsed": "45.2 MB",
    "activeKnowledgeBases": 12,
    "orphanedCollections": ["old-test-kb", "deprecated-docs"]
  },
  "knowledgeBases": [
    {
      "id": "react-docs",
      "name": "React Documentation",
      "documentCount": 47,
      "totalChunks": 892,
      "storageSize": "8.5 MB",
      "lastAccessed": "2025-10-01T15:30:00Z",
      "conversationCount": 45,
      "createdAt": "2025-09-15T10:00:00Z",
      "isActive": true
    },
    {
      "id": "dotnet-api-reference",
      "name": ".NET API Reference",
      "documentCount": 89,
      "totalChunks": 1543,
      "storageSize": "12.3 MB",
      "lastAccessed": "2025-09-28T09:15:00Z",
      "conversationCount": 23,
      "createdAt": "2025-08-20T14:00:00Z",
      "isActive": true
    }
  ],
  "recommendations": [
    "Consider archiving 'old-test-kb' (last accessed 90+ days ago)",
    "Delete orphaned Qdrant collection: 'deprecated-docs'"
  ]
}
```

### Real-World Example

**Question**: "Which knowledge bases are using the most storage?"

**VS Code Command**: `@knowledge-mcp Get knowledge base summary sorted by size`

**What Happens**:
1. Tool queries SQLite for all knowledge bases
2. Retrieves document counts and metadata
3. Checks Qdrant for collection sizes
4. Sorts by storage size (largest first)
5. Returns structured JSON with recommendations

**Result**: You see that `dotnet-api-reference` (12.3 MB) and `react-docs` (8.5 MB) are your largest knowledge bases, helping you plan storage allocation.

---

## 2. `get_quick_health_overview` - Lightweight Monitoring

### Purpose
Provides a **fast, concise health snapshot** of critical system components, optimized for monitoring dashboards and quick status checks.

### What It Does
- Checks **critical components only** (SQLite, Qdrant, Ollama)
- Returns **overall health score** (0-100%)
- Lists component status counts (healthy, warnings, critical, offline)
- Shows **top 3 alerts** (most critical issues)
- Provides **top 2 recommendations** (actionable guidance)
- Includes **key metrics** (success rate, avg response time, errors)

### Difference from `get_system_health`

| Feature | `get_quick_health_overview` | `get_system_health` |
|---------|----------------------------|---------------------|
| **Speed** | Fast (critical components only) | Slower (all components) |
| **Detail Level** | Concise summary | Full component breakdown |
| **Alerts** | Top 3 | Top 5 |
| **Recommendations** | Top 2 | Top 3 |
| **Use Case** | Quick status checks, dashboards | Deep troubleshooting, comprehensive analysis |
| **Component Details** | Summary counts only | Full details per component |

### Use Cases

#### 1. **Dashboard Monitoring**
```
Scenario: Real-time dashboard showing system health every 30 seconds
Command: @knowledge-mcp Get quick health overview
Expected: Fast response with overall health score and critical alerts
```

#### 2. **Pre-Deployment Health Check**
```
Scenario: Before deploying changes, verify system is healthy
Command: @knowledge-mcp Get quick health overview
Expected: Green light (100% score) or critical issues requiring attention
```

#### 3. **Quick Troubleshooting Triage**
```
Scenario: User reports slow responses, need fast diagnosis
Command: @knowledge-mcp Get quick health overview
Expected: Immediate identification of component failures or high error rates
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| None | - | - | No parameters required (optimized for speed) |

### Expected Output Structure

```json
{
  "status": "Healthy",
  "healthScore": 100.0,
  "timestamp": "2025-10-05T13:05:07Z",
  "summary": {
    "healthyComponents": 3,
    "componentsWithWarnings": 0,
    "criticalComponents": 0,
    "offlineComponents": 0,
    "totalComponents": 3
  },
  "isSystemHealthy": true,
  "activeAlerts": [
    // Top 3 most critical alerts only
  ],
  "quickRecommendations": [
    "Consider optimizing queries or scaling resources (19.8s avg response time)",
    "Investigate error patterns to improve reliability"
  ],
  "metrics": {
    "successRate": "92.4%",
    "averageResponseTime": "19.8s",
    "errorsLast24Hours": 0,
    "systemUptime": "15d 6h 23m"
  }
}
```

### Real-World Example

**Question**: "Is the system healthy right now?"

**VS Code Command**: `@knowledge-mcp Get quick health overview`

**What Happens**:
1. Tool performs fast health check on critical components (SQLite, Qdrant, Ollama)
2. Calculates overall health score
3. Identifies top 3 most critical alerts (if any)
4. Returns concise JSON response

**Result**: You immediately see "100% Healthy" with all 3 components operational, or get alerted to critical issues like "Qdrant offline - 0 healthy components".

---

## 3. `check_component_health` - Component-Specific Debugging

### Purpose
Performs a **deep dive health check on a single component**, providing detailed diagnostics for troubleshooting specific issues.

### What It Does
- Checks **one specific component** (SQLite, Qdrant, OpenAI, Anthropic, Ollama, etc.)
- Tests **connectivity** to the component
- Measures **response time** (latency diagnostics)
- Counts **recent errors**
- Retrieves **component-specific metrics** (varies by component type)
- Returns **detailed status message** with troubleshooting hints

### Supported Components

| Component | What Gets Checked |
|-----------|------------------|
| **SQLite** | Database file accessibility, connection pool health, query performance |
| **Qdrant** | gRPC connection, collection list retrieval, vector store responsiveness |
| **Ollama** | HTTP API connectivity, model availability, inference performance |
| **OpenAI** | API key validation, endpoint reachability, rate limit status |
| **Anthropic** | API key validation, Claude model availability, quota status |
| **Google** | Gemini API connectivity, model access, authentication status |

### Use Cases

#### 1. **Troubleshoot Search Failures**
```
Scenario: Knowledge search returning errors
Command: @knowledge-mcp Check component health Qdrant
Expected: Qdrant connection status, response time, collection count
Result: "Qdrant offline - connection refused on port 6334" → Action: Start Qdrant Docker container
```

#### 2. **Diagnose Slow Model Responses**
```
Scenario: Ollama models responding slowly
Command: @knowledge-mcp Check component health Ollama
Expected: Ollama API status, average response time, model load times
Result: "Ollama healthy but 45s avg response time" → Action: Check GPU utilization or switch to smaller model
```

#### 3. **Verify API Key Configuration**
```
Scenario: OpenAI chat requests failing
Command: @knowledge-mcp Check component health OpenAI
Expected: API key validation, endpoint connectivity, quota status
Result: "OpenAI authentication failed - invalid API key" → Action: Update OPENAI_API_KEY in config
```

#### 4. **Database Performance Analysis**
```
Scenario: Slow conversation history loading
Command: @knowledge-mcp Check component health SQLite
Expected: Database connectivity, query performance, file size
Result: "SQLite healthy - 400KB database, 2ms avg query time" → Issue is elsewhere
```

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `componentName` | string | ✅ Yes | Component to check (e.g., "SQLite", "Qdrant", "Ollama", "OpenAI") |

### Expected Output Structure

```json
{
  "componentName": "Qdrant",
  "status": "Healthy",
  "isConnected": true,
  "statusMessage": "Vector store operational - 15 collections available",
  "responseTime": "45ms",
  "lastChecked": "2025-10-05T13:10:15Z",
  "errorCount": 0,
  "metrics": {
    "collections": "15",
    "vectorDimensions": "768",
    "totalVectors": "12,543",
    "memoryUsage": "234 MB"
  },
  "isHealthy": true
}
```

**Error Example** (Qdrant Offline):
```json
{
  "componentName": "Qdrant",
  "status": "Critical",
  "isConnected": false,
  "statusMessage": "Failed to connect to Qdrant gRPC endpoint at localhost:6334 - connection refused",
  "responseTime": "N/A",
  "lastChecked": "2025-10-05T13:15:20Z",
  "errorCount": 1,
  "metrics": {},
  "isHealthy": false
}
```

### Real-World Example

**Question**: "Why is my knowledge search failing?"

**VS Code Command**: `@knowledge-mcp Check component health Qdrant`

**What Happens**:
1. Tool attempts to connect to Qdrant vector store
2. Tests collection listing (GET /collections)
3. Measures response time
4. Checks error logs for recent failures
5. Returns detailed diagnostic report

**Scenario A - Qdrant Running**:
```json
{
  "status": "Healthy",
  "isConnected": true,
  "statusMessage": "Vector store operational - 15 collections available",
  "responseTime": "45ms",
  "errorCount": 0
}
```
**Action**: Issue is not Qdrant - check embedding service or search query.

**Scenario B - Qdrant Offline**:
```json
{
  "status": "Critical",
  "isConnected": false,
  "statusMessage": "Connection refused on port 6334",
  "errorCount": 1
}
```
**Action**: Start Qdrant container with `docker start qdrant` or `docker-compose up -d qdrant`.

---

## When to Use Each Tool

### Decision Tree

```
Need system-wide health check?
├─ Yes, need full details → Use get_system_health
├─ Yes, need quick overview → Use get_quick_health_overview
└─ No, specific component issue → Use check_component_health

Need knowledge base information?
├─ Find most-used knowledge bases → get_knowledge_base_summary (sort: activity)
├─ Find largest knowledge bases → get_knowledge_base_summary (sort: size)
├─ Find cleanup candidates → get_knowledge_base_summary (sort: age)
└─ Browse all knowledge bases → get_knowledge_base_summary (sort: alphabetical)

Troubleshooting specific failure?
├─ Search not working → check_component_health Qdrant
├─ Chat failing → check_component_health Ollama (or OpenAI, Anthropic)
├─ Slow database queries → check_component_health SQLite
└─ General system issues → get_system_health (full diagnostics)
```

---

## Testing Recommendations

### Test 1: Knowledge Base Summary
```bash
# In VS Code with MCP client
@knowledge-mcp Get knowledge base summary sorted by activity

# Expected: JSON with list of all knowledge bases, sorted by last access time
# Validates: SQLite connectivity, usage tracking, Qdrant sync detection
```

### Test 2: Quick Health Overview
```bash
# In VS Code with MCP client
@knowledge-mcp Get quick health overview

# Expected: Health score 100%, 3 components healthy
# Validates: Fast health check, concise output, critical component monitoring
```

### Test 3: Component Health Check
```bash
# In VS Code with MCP client
@knowledge-mcp Check component health Qdrant

# Expected: Qdrant status, collection count, response time
# Validates: Component-specific diagnostics, detailed metrics
```

---

## Summary

| Tool | Speed | Detail Level | Best For |
|------|-------|--------------|----------|
| `get_knowledge_base_summary` | Medium | High | Content management, storage planning, activity analysis |
| `get_quick_health_overview` | Fast | Low | Dashboards, quick checks, deployment validation |
| `check_component_health` | Medium | Very High | Troubleshooting specific failures, deep diagnostics |

All three tools are **fully implemented** and ready for testing in Phase 2A.
