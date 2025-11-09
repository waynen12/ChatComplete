# Phase 2B: MCP Resources Implementation Guide

## Overview

**Goal**: Expose knowledge base documents as MCP resources (read-only data endpoints)
**Effort**: 2-3 days
**Priority**: ⭐⭐⭐⭐⭐ CORE MCP LEARNING
**MCP Spec Coverage**: 25% → 50% (adding Resources primitive)

---

## What You're Building

Transform your knowledge base from **search-only** into **directly readable** resources:

**Before (Phase 1 - Tools Only):**
```
User: "Show me Docker SSL docs"
→ Tool searches knowledge base
→ Returns snippets/excerpts
→ Limited context for LLM
```

**After (Phase 2B - With Resources):**
```
User: "Show me Docker SSL docs"
→ Client lists available resources
→ Client reads full document
→ LLM gets complete content
→ Better, more accurate answers
```

---

## Implementation Checklist

### Phase 2B.1: Core Resource Provider (Day 1)

#### ✅ Task 1: Create Resource Provider Class
**File**: `Knowledge.Mcp/Resources/KnowledgeResourceProvider.cs` (NEW)

**What it does:**
- Handles MCP resource protocol messages
- Lists all available resources (collections, documents, system data)
- Reads specific resource content
- Manages resource subscriptions

**Key Methods:**
1. `ListResourcesAsync()` - Returns catalog of all available URIs
2. `ReadResourceAsync(uri)` - Returns content of specific resource
3. `SubscribeToResourceAsync(uri)` - Enables real-time updates (optional)

**Lines of Code**: ~300-400
**Dependencies**: IKnowledgeRepository, ILogger

**Example Output**: See [MCP_RESOURCES_JSON_EXAMPLES.md](MCP_RESOURCES_JSON_EXAMPLES.md) - Scenario 1

---

#### ✅ Task 2: Create URI Parser
**File**: `Knowledge.Mcp/Resources/ResourceUriParser.cs` (NEW)

**What it does:**
- Parses resource URIs into structured data
- Validates URI format
- Extracts collection IDs, document IDs, resource types

**URI Patterns to Support:**
```
resource://knowledge/collections                          → CollectionList
resource://knowledge/{collectionId}/documents             → DocumentList
resource://knowledge/{collectionId}/document/{docId}      → Document
resource://knowledge/{collectionId}/stats                 → CollectionStats
resource://system/health                                  → SystemHealth
resource://system/models                                  → ModelList
```

**Lines of Code**: ~100-150
**Dependencies**: None (pure parsing logic)

**Example Input/Output:**
```csharp
Input:  "resource://knowledge/docker-guides/document/ssl-setup"
Output: new ParsedResourceUri {
    Type = ResourceType.Document,
    CollectionId = "docker-guides",
    DocumentId = "ssl-setup"
}
```

---

#### ✅ Task 3: Create Resource Models
**File**: `Knowledge.Mcp/Resources/Models/ResourceModels.cs` (NEW)

**What it does:**
- Defines enums, DTOs, and data structures
- Resource types, parsed URI structure, metadata templates

**Classes to Define:**
```csharp
public enum ResourceType
{
    CollectionList,      // List all collections
    DocumentList,        // List documents in collection
    Document,            // Single document content
    CollectionStats,     // Collection analytics
    SystemHealth,        // System health data
    ModelList            // AI models inventory
}

public class ParsedResourceUri
{
    public ResourceType Type { get; set; }
    public string? CollectionId { get; set; }
    public string? DocumentId { get; set; }
}

public class ResourceMetadata
{
    public string Uri { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string MimeType { get; set; }
}
```

**Lines of Code**: ~50-75
**Dependencies**: None

---

#### ✅ Task 4: Register Resource Provider
**File**: `Knowledge.Mcp/Program.cs` (MODIFY)

**What to add:**
```csharp
// Add resource provider to DI container
services.AddScoped<KnowledgeResourceProvider>();

// MCP SDK automatically discovers resource providers via reflection
// No additional configuration needed - the SDK will call:
// - ListResourcesAsync() when client sends resources/list
// - ReadResourceAsync(uri) when client sends resources/read
// - SubscribeToResourceAsync(uri) when client sends resources/subscribe
```

**Lines of Code**: ~5-10 lines added
**Dependencies**: KnowledgeResourceProvider

---

### Phase 2B.2: Resource Content Handlers (Day 2)

#### ✅ Task 5: Implement Collection List Handler
**Location**: Inside `KnowledgeResourceProvider.cs`

**What it does:**
- Returns JSON list of all knowledge base collections
- Includes metadata (document count, chunk count, activity)

**Method Signature:**
```csharp
private async Task<ResourceReadResult> ReadCollectionListAsync(
    CancellationToken cancellationToken)
```

**Data Source**: `IKnowledgeRepository.GetAllAsync()`

**Example Output**: See [MCP_RESOURCES_JSON_EXAMPLES.md](MCP_RESOURCES_JSON_EXAMPLES.md) - Scenario 2

**Lines of Code**: ~30-40

---

#### ✅ Task 6: Implement Document List Handler
**Location**: Inside `KnowledgeResourceProvider.cs`

**What it does:**
- Returns JSON list of all documents in a specific collection
- Includes document metadata (size, chunk count, URIs)

**Method Signature:**
```csharp
private async Task<ResourceReadResult> ReadDocumentListAsync(
    string collectionId,
    CancellationToken cancellationToken)
```

**Data Source**: `IKnowledgeRepository.GetDocumentsByCollectionAsync(collectionId)`

**Example Output**: See [MCP_RESOURCES_JSON_EXAMPLES.md](MCP_RESOURCES_JSON_EXAMPLES.md) - Scenario 4

**Lines of Code**: ~40-50

---

#### ✅ Task 7: Implement Document Content Handler
**Location**: Inside `KnowledgeResourceProvider.cs`

**What it does:**
- Returns full content of a specific document
- Handles markdown, text, JSON formats

**Method Signature:**
```csharp
private async Task<ResourceReadResult> ReadDocumentAsync(
    string collectionId,
    string documentId,
    CancellationToken cancellationToken)
```

**Data Source**: `IKnowledgeRepository.GetDocumentAsync(collectionId, documentId)`

**Example Output**: See [MCP_RESOURCES_JSON_EXAMPLES.md](MCP_RESOURCES_JSON_EXAMPLES.md) - Scenario 3

**Lines of Code**: ~30-40

**Key Feature**: This is the most important handler - gives LLMs full document access!

---

#### ✅ Task 8: Implement Collection Stats Handler
**Location**: Inside `KnowledgeResourceProvider.cs`

**What it does:**
- Returns analytics for a specific collection
- Document count, chunk count, usage statistics, storage metrics

**Method Signature:**
```csharp
private async Task<ResourceReadResult> ReadCollectionStatsAsync(
    string collectionId,
    CancellationToken cancellationToken)
```

**Data Source**: Combine data from:
- `IKnowledgeRepository.GetByIdAsync(collectionId)`
- `IUsageTrackingService.GetUsageHistoryAsync()` (filter by collection)

**Example Output**: See [MCP_RESOURCES_JSON_EXAMPLES.md](MCP_RESOURCES_JSON_EXAMPLES.md) - Scenario 6

**Lines of Code**: ~50-60

---

#### ✅ Task 9: Implement System Health Handler
**Location**: Inside `KnowledgeResourceProvider.cs`

**What it does:**
- Returns current system health status
- Delegates to existing `ISystemHealthService`

**Method Signature:**
```csharp
private async Task<ResourceReadResult> ReadSystemHealthAsync(
    CancellationToken cancellationToken)
```

**Data Source**: `ISystemHealthService.GetSystemHealthAsync()`

**Example Output**: See [MCP_RESOURCES_JSON_EXAMPLES.md](MCP_RESOURCES_JSON_EXAMPLES.md) - Scenario 5

**Lines of Code**: ~20-30

**Note**: Reuses existing health check logic from Phase 2A!

---

#### ✅ Task 10: Implement Model List Handler
**Location**: Inside `KnowledgeResourceProvider.cs`

**What it does:**
- Returns inventory of all Ollama models
- Includes performance metrics from usage tracking

**Method Signature:**
```csharp
private async Task<ResourceReadResult> ReadModelListAsync(
    CancellationToken cancellationToken)
```

**Data Source**: Combine data from:
- Ollama API (model list and sizes)
- `IUsageTrackingService.GetUsageHistoryAsync()` (performance metrics)

**Example Output**: See [MCP_RESOURCES_JSON_EXAMPLES.md](MCP_RESOURCES_JSON_EXAMPLES.md) - Scenario 8

**Lines of Code**: ~40-50

---

### Phase 2B.3: Configuration & Testing (Day 3)

#### ✅ Task 11: Add Resource Configuration
**File**: `Knowledge.Mcp/appsettings.json` (MODIFY)

**What to add:**
```json
{
  "McpServerSettings": {
    "Resources": {
      "MaxDocumentSize": 10485760,        // 10MB max per document
      "EnableDocumentCaching": true,
      "CacheDurationMinutes": 60,
      "MaxListResults": 1000,             // Max resources in list response
      "EnableSubscriptions": true,
      "SubscriptionTimeoutMinutes": 30
    }
  }
}
```

**Lines Added**: ~10-15

---

#### ✅ Task 12: Add Unit Tests
**File**: `Knowledge.Mcp.Tests/Resources/KnowledgeResourceProviderTests.cs` (NEW)

**Tests to Write:**

1. **ListResourcesAsync Tests**
   - Returns all available resources
   - Includes correct URIs for collections
   - Includes correct URIs for documents
   - Returns proper metadata (name, description, mimeType)

2. **ReadResourceAsync Tests**
   - Reads collection list successfully
   - Reads document list for valid collection
   - Reads full document content
   - Returns collection stats
   - Returns system health
   - Returns model list
   - Throws error for invalid URI
   - Throws error for non-existent collection
   - Throws error for non-existent document

3. **URI Parser Tests**
   - Parses collection list URI
   - Parses document list URI
   - Parses document content URI
   - Parses collection stats URI
   - Parses system URIs
   - Throws error for malformed URIs

**Lines of Code**: ~400-500 lines total
**Test Count**: ~20-25 tests

---

#### ✅ Task 13: Manual Integration Testing
**Tool**: VS Code with MCP client

**Test Scenarios:**

1. **List All Resources**
   ```
   VS Code command: @knowledge-mcp List available resources
   Expected: JSON with 10+ resource URIs
   ```

2. **Read Collection List**
   ```
   VS Code command: @knowledge-mcp Show me all knowledge base collections
   Expected: JSON with 4 collections (your current data)
   ```

3. **Read Specific Document**
   ```
   VS Code command: @knowledge-mcp Read the Heliograph Chapter 1 document
   Expected: Full markdown content of "The Keeper" story
   ```

4. **Read System Health**
   ```
   VS Code command: @knowledge-mcp What's the current system health?
   Expected: JSON with Healthy status, 100% score, component details
   ```

5. **Browse Documents in Collection**
   ```
   VS Code command: @knowledge-mcp List all documents in Knowledge Manager
   Expected: JSON with 12 document entries
   ```

**Testing Time**: ~1-2 hours

---

## File Structure Summary

### New Files (6 total)
```
Knowledge.Mcp/
├── Resources/
│   ├── KnowledgeResourceProvider.cs        (NEW - 300-400 lines)
│   ├── ResourceUriParser.cs                (NEW - 100-150 lines)
│   └── Models/
│       └── ResourceModels.cs               (NEW - 50-75 lines)
│
Knowledge.Mcp.Tests/
└── Resources/
    ├── KnowledgeResourceProviderTests.cs   (NEW - 400-500 lines)
    ├── ResourceUriParserTests.cs           (NEW - 150-200 lines)
    └── ResourceIntegrationTests.cs         (NEW - 200-250 lines)
```

### Modified Files (2 total)
```
Knowledge.Mcp/
├── Program.cs                              (ADD ~10 lines)
└── appsettings.json                        (ADD ~15 lines)
```

**Total New Code**: ~1,300-1,700 lines (including tests)
**Total Modified Code**: ~25 lines

---

## Dependencies & Interfaces

### Required Services (Already Available)
- ✅ `IKnowledgeRepository` - Get collections, documents, metadata
- ✅ `ISystemHealthService` - System health data
- ✅ `IUsageTrackingService` - Usage statistics
- ✅ `ILogger<T>` - Logging

### New Interfaces (None!)
Phase 2B uses existing interfaces - no new contracts needed.

---

## Resource Types You'll Expose

| Resource Type | Count | Example URI |
|--------------|-------|-------------|
| **Collection List** | 1 | `resource://knowledge/collections` |
| **Document Lists** | 4 | `resource://knowledge/AI_Engineering/documents` |
| **Individual Documents** | 18 | `resource://knowledge/Heliograph_Test_Document/document/chapter1` |
| **Collection Stats** | 4 | `resource://knowledge/Knowledge_Manager/stats` |
| **System Health** | 1 | `resource://system/health` |
| **Model List** | 1 | `resource://system/models` |
| **Total Resources** | **29** | (Based on your current 4 collections, 18 documents) |

---

## Implementation Order (Recommended)

### Day 1: Foundation
1. ✅ Create `ResourceModels.cs` (data structures)
2. ✅ Create `ResourceUriParser.cs` (URI parsing)
3. ✅ Create `KnowledgeResourceProvider.cs` shell (empty methods)
4. ✅ Add unit tests for URI parser
5. ✅ Register provider in `Program.cs`

**Deliverable**: Build succeeds, URI parser tested

---

### Day 2: Core Handlers
6. ✅ Implement `ReadCollectionListAsync()`
7. ✅ Implement `ReadDocumentListAsync()`
8. ✅ Implement `ReadDocumentAsync()` (most important!)
9. ✅ Implement `ListResourcesAsync()` (uses above 3)
10. ✅ Add unit tests for handlers

**Deliverable**: Can list and read collections/documents

---

### Day 3: System Resources & Testing
11. ✅ Implement `ReadCollectionStatsAsync()`
12. ✅ Implement `ReadSystemHealthAsync()`
13. ✅ Implement `ReadModelListAsync()`
14. ✅ Add configuration to `appsettings.json`
15. ✅ Complete unit test suite
16. ✅ Manual integration testing with VS Code

**Deliverable**: All resources working, fully tested

---

## Success Criteria

### Functional Requirements
- ✅ Client can list all 29 available resources
- ✅ Client can read any collection list (4 collections)
- ✅ Client can read any document list (4 document lists)
- ✅ Client can read any individual document (18 documents)
- ✅ Client can read collection statistics (4 stat resources)
- ✅ Client can read system health status
- ✅ Client can read model inventory
- ✅ All URIs follow standard pattern
- ✅ All responses are valid JSON or markdown

### Quality Requirements
- ✅ 90%+ unit test coverage
- ✅ All tests passing
- ✅ No hardcoded values (use configuration)
- ✅ Proper error handling (404 for missing resources)
- ✅ Logging at appropriate levels
- ✅ Documentation updated

### Performance Requirements
- ✅ `resources/list` responds in < 500ms
- ✅ `resources/read` (collection list) in < 200ms
- ✅ `resources/read` (document) in < 1000ms
- ✅ No N+1 query problems

---

## Testing Strategy

### Unit Tests (~20-25 tests)
```csharp
[Fact]
public async Task ListResourcesAsync_ReturnsAllCollections()
{
    // Arrange
    var provider = CreateProvider(mockCollections: 4);

    // Act
    var result = await provider.ListResourcesAsync();

    // Assert
    Assert.Equal(29, result.Resources.Count); // 1 + 4*2 + 18 + 4 + 1 + 1
    Assert.Contains(result.Resources, r =>
        r.Uri == "resource://knowledge/collections");
}

[Fact]
public async Task ReadResourceAsync_Document_ReturnsFullContent()
{
    // Arrange
    var provider = CreateProvider();
    var uri = "resource://knowledge/Heliograph_Test_Document/document/chapter1";

    // Act
    var result = await provider.ReadResourceAsync(uri);

    // Assert
    Assert.Single(result.Contents);
    Assert.Equal("text/markdown", result.Contents[0].MimeType);
    Assert.Contains("The Keeper", result.Contents[0].Text);
}
```

### Integration Tests (VS Code)
1. List resources → Verify count and URIs
2. Read collection list → Verify 4 collections
3. Read document → Verify full content
4. Read non-existent → Verify error handling
5. Subscribe to resource → Verify subscription acknowledged

---

## Risk Assessment

### Low Risk Items ✅
- **URI parsing** - Straightforward string manipulation
- **Collection list** - Simple database query
- **System health** - Delegates to existing service
- **Configuration** - Standard appsettings.json pattern

### Medium Risk Items ⚠️
- **Document reading** - Need to handle various formats (markdown, text, JSON)
- **Large documents** - May need chunking or size limits
- **Missing metadata** - Some documents may lack MIME type info

### High Risk Items 🔴
- **None identified** - Phase 2B is relatively low-risk since it builds on Phase 1 foundation

### Mitigation Strategies
1. **Document size**: Add `MaxDocumentSize` configuration (10MB limit)
2. **Missing metadata**: Default to "text/plain" MIME type if unknown
3. **Format handling**: Use content type detection library if needed
4. **Performance**: Add caching for frequently accessed resources

---

## Post-Implementation

### Documentation to Update
- ✅ Update `AGENT_IMPLEMENTATION_PLAN.md` - Mark Phase 2B complete
- ✅ Update `CLAUDE.md` - Add MCP Resources milestone
- ✅ Create `MCP_RESOURCES_USER_GUIDE.md` - How to use resources from clients
- ✅ Update `README.md` - Add MCP Resources feature

### Optional Enhancements (Future)
- Resource caching for better performance
- Resource templates (URI patterns with variables)
- Resource permissions/access control
- Binary resource support (images, PDFs)
- Resource search/filtering
- Resource versioning

---

## Summary

**What You're Building**: A resource provider that exposes your knowledge base as browsable, readable resources via MCP protocol.

**Key Files**: 6 new files (~1,500 lines with tests), 2 modified files (~25 lines)

**Timeline**: 3 days (1 day foundation, 1 day core handlers, 1 day system resources + testing)

**Outcome**:
- 50% MCP spec coverage (Tools + Resources)
- 29 resources exposed (collections, documents, system data)
- LLMs get full document access (not just search snippets)
- Better, more accurate AI assistant responses

**Next Phase**: Phase 2C - MCP Prompts (workflow templates)

Ready to start implementing! 🚀
