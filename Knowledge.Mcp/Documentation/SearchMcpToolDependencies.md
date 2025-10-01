# Cross-Knowledge Search MCP Tool Dependencies

This document outlines all the classes required for the `search_all_knowledge_bases` MCP tool implementation, including existing classes and their relationships.

## 🏗️ Architecture Overview

```
External MCP Client (VS Code, CLI, etc.)
    ↓
CrossKnowledgeSearchMcpTool (NEW)
    ↓
CrossKnowledgeSearchPlugin (EXISTING - Wrapper)
    ↓
KnowledgeManager (EXISTING - Core Engine)
    ↓
IVectorStoreStrategy + IKnowledgeRepository (EXISTING - Data Layer)
```

## 📋 Required Classes Inventory

### 🆕 NEW CLASSES (To Be Created)

#### 1. CrossKnowledgeSearchMcpTool
- **Location:** `/Knowledge.Mcp/Tools/CrossKnowledgeSearchMcpTool.cs`
- **Status:** ✅ CREATED
- **Purpose:** MCP wrapper providing external access to knowledge search
- **Pattern:** Follows `SystemHealthMcpTool.cs` structure

```csharp
// Key Attributes and Methods
[McpServerToolType]
public sealed class CrossKnowledgeSearchMcpTool
{
    [McpServerTool]
    [Description("Search across ALL knowledge bases...")]
    public static async Task<string> SearchAllKnowledgeBasesAsync(
        string query, 
        int limit = 5, 
        double minRelevance = 0.6, 
        IServiceProvider serviceProvider)
}
```

---

### 🔧 EXISTING CLASSES (Core Dependencies)

#### 2. CrossKnowledgeSearchPlugin *(Existing Logic Layer)*
- **Location:** `/KnowledgeEngine/Agents/Plugins/CrossKnowledgeSearchPlugin.cs`
- **Status:** ✅ EXISTS
- **Purpose:** Contains proven search logic that we'll wrap for MCP access
- **Key Dependencies:** `KnowledgeManager`

```csharp
// Key Method We'll Reuse
public async Task<string> SearchAllKnowledgeBasesAsync(
    string query, 
    int limit = 5, 
    double minRelevance = 0.6)
{
    // 1. Get available collections
    var collections = await _knowledgeManager.GetAvailableCollectionsAsync();
    
    // 2. Parallel search across collections
    var searchTasks = collections.Select(collection => 
        SearchCollectionAsync(collection, query, limit, minRelevance));
    
    // 3. Aggregate and format results
    var searchResults = await Task.WhenAll(searchTasks);
    
    // 4. Return formatted response
    return FormatSearchResults(searchResults);
}
```

#### 3. KnowledgeManager *(Core Search Engine)*
- **Location:** `/KnowledgeEngine/KnowledgeManager.cs`
- **Status:** ✅ EXISTS
- **Purpose:** Central orchestrator for all knowledge operations
- **Key Dependencies:** `IVectorStoreStrategy`, `IEmbeddingGenerator`, `IKnowledgeRepository`

```csharp
// Key Methods Used by Search
public class KnowledgeManager
{
    // Lists all available knowledge collections
    public async Task<List<string>> GetAvailableCollectionsAsync(
        CancellationToken cancellationToken = default)
    
    // Searches individual knowledge collection
    public async Task<List<KnowledgeSearchResult>> SearchAsync(
        string collectionName,
        string query,
        int limit = 10,
        double minRelevanceScore = 0.3,
        CancellationToken cancellationToken = default)
}
```

#### 4. KnowledgeSearchResult *(Data Model)*
- **Location:** `/KnowledgeEngine/Models/KnowledgeSearchResult.cs`
- **Status:** ✅ EXISTS
- **Purpose:** Standardized search result data structure

```csharp
// Search Result Structure
public record KnowledgeSearchResult
{
    public string Text { get; init; } = string.Empty;        // Chunk content
    public string Source { get; init; } = string.Empty;      // Source document
    public int ChunkOrder { get; init; }                     // Position in document
    public string Tags { get; init; } = string.Empty;        // Metadata tags
    public double Score { get; init; }                       // Relevance score
}
```

#### 5. IVectorStoreStrategy *(Vector Store Abstraction)*
- **Location:** `/KnowledgeEngine/Persistence/VectorStores/IVectorStoreStrategy.cs`
- **Status:** ✅ EXISTS
- **Purpose:** Abstracts vector store operations (Qdrant/MongoDB)
- **Implementation:** `QdrantVectorStoreStrategy` (active in MCP server)

```csharp
// Key Interface Methods
public interface IVectorStoreStrategy
{
    // Search vectors in specific collection
    Task<List<KnowledgeSearchResult>> SearchAsync(
        string collectionName,
        string query,
        Embedding<float> queryEmbedding,
        int limit,
        double minRelevanceScore,
        CancellationToken cancellationToken = default);
    
    // List all available collections
    Task<List<string>> ListCollectionsAsync(
        CancellationToken cancellationToken = default);
}
```

#### 6. IKnowledgeRepository *(Metadata Access)*
- **Location:** `/KnowledgeEngine/Persistence/IKnowledgeRepository.cs`
- **Status:** ✅ EXISTS  
- **Purpose:** Repository pattern for knowledge metadata
- **Implementation:** `SqliteKnowledgeRepository` (active in MCP server)

```csharp
// Key Interface Methods
public interface IKnowledgeRepository
{
    // Get all knowledge collection summaries
    Task<IEnumerable<KnowledgeSummaryDto>> GetAllAsync(
        CancellationToken cancellationToken = default);
    
    // Check if collection exists
    Task<bool> ExistsAsync(string knowledgeId, 
        CancellationToken cancellationToken = default);
}
```

---

### ⚙️ INFRASTRUCTURE CLASSES (DI & Configuration)

#### 7. IServiceProvider *(Dependency Injection)*
- **Location:** Built-in .NET interface
- **Status:** ✅ EXISTS
- **Purpose:** Service resolution within MCP tools
- **Usage:** Automatically provided by MCP framework

```csharp
// Service Resolution Pattern Used in MCP Tools
var knowledgeManager = serviceProvider.GetRequiredService<KnowledgeManager>();
var systemHealthService = serviceProvider.GetRequiredService<ISystemHealthService>();
```

#### 8. Program.cs Service Registration *(DI Configuration)*
- **Location:** `/Knowledge.Mcp/Program.cs`
- **Status:** ✅ EXISTS & CONFIGURED
- **Purpose:** Registers all required services for dependency injection

```csharp
// Already Configured Services (Qdrant-only setup)
services.AddSingleton(chatCompleteSettings);
services.AddSingleton(chatCompleteSettings.VectorStore.Qdrant);

// Vector Store Strategy
services.AddSingleton<QdrantVectorStore>(provider => /* Qdrant setup */);
services.AddSingleton<IVectorStoreStrategy, QdrantVectorStoreStrategy>();

// Knowledge Services
services.AddSingleton<IKnowledgeRepository, SqliteKnowledgeRepository>();
services.AddSingleton<KnowledgeManager>();

// Embedding Services (configured for Ollama)
services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(/* embedding setup */);
```

---

## 🔗 Dependency Chain Analysis

### Service Resolution Flow
1. **MCP Client Request** → `CrossKnowledgeSearchMcpTool.SearchAllKnowledgeBasesAsync()`
2. **Service Resolution** → `serviceProvider.GetRequiredService<KnowledgeManager>()`
3. **Plugin Creation** → `new CrossKnowledgeSearchPlugin(knowledgeManager)`
4. **Search Execution** → `searchPlugin.SearchAllKnowledgeBasesAsync()`
5. **Collection Discovery** → `knowledgeManager.GetAvailableCollectionsAsync()`
6. **Parallel Search** → `knowledgeManager.SearchAsync()` per collection
7. **Vector Operations** → `vectorStoreStrategy.SearchAsync()` + `repository.GetAllAsync()`
8. **Result Aggregation** → Format and return to MCP client

### Critical Dependencies
- ✅ **KnowledgeManager** - Central orchestrator
- ✅ **IVectorStoreStrategy** - Vector search operations  
- ✅ **IKnowledgeRepository** - Metadata access
- ✅ **IEmbeddingGenerator** - Query vectorization
- ✅ **CrossKnowledgeSearchPlugin** - Proven search logic

## 🎯 Implementation Status

### ✅ COMPLETED
- Service registration in `Program.cs`
- All infrastructure classes exist and are tested
- Qdrant configuration working in MCP server
- Error handling patterns established

### 🔄 IN PROGRESS  
- `CrossKnowledgeSearchMcpTool.cs` created with full implementation
- Integration testing with existing MCP server

### 📋 NEXT STEPS
1. Add `CrossKnowledgeSearchMcpTool` to MCP server registration
2. Test with VS Code MCP client
3. Create integration tests
4. Update MCP server documentation

## 🚀 Usage Examples

### VS Code MCP Client
```bash
# Search for Docker documentation
mcp-call search_all_knowledge_bases "Docker SSL configuration" --limit=10 --minRelevance=0.7

# Find API examples
mcp-call search_all_knowledge_bases "REST API authentication" --limit=5
```

### CLI Integration
```bash
# Knowledge base verification
curl -X POST mcp://knowledge/search_all_knowledge_bases \
  -d '{"query": "deployment guide", "limit": 3}'
```

### Monitoring Scripts
```python
# Check knowledge base content
result = mcp_client.call_tool("search_all_knowledge_bases", {
    "query": "health check endpoint",
    "limit": 1,
    "minRelevance": 0.8
})
```

This architecture leverages all existing, tested infrastructure while providing a clean MCP interface for external access to the knowledge search capabilities.