# RAG Implementation Guide: Lessons from AI Knowledge Manager

**Purpose:** This document captures the core learnings and architectural patterns from the AI Knowledge Manager project to guide RAG implementation in other projects.

**Source Project:** AI Knowledge Manager (ChatComplete)
**Target Framework:** .NET 8 (Agent Framework) / .NET 4.8+ (Semantic Kernel fallback)
**Created:** 2026-01-12
**Author:** Wayne

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Core RAG Architecture](#core-rag-architecture)
3. [Framework Decision Tree (.NET 8 vs .NET 4.8)](#framework-decision-tree)
4. [Vector Store Options](#vector-store-options)
5. [Document Management & Templates](#document-management--templates)
6. [Multi-Provider LLM Support](#multi-provider-llm-support)
7. [Chat Implementation Patterns](#chat-implementation-patterns)
8. [Knowledge Base Management](#knowledge-base-management)
9. [Testing Strategy](#testing-strategy)
10. [Deployment Considerations](#deployment-considerations)
11. [Code Examples & Patterns](#code-examples--patterns)
12. [Common Pitfalls & Solutions](#common-pitfalls--solutions)
13. [Performance Optimization](#performance-optimization)
14. [Multi-Tenancy & Customer Customization](#multi-tenancy--customer-customization)

---

## Executive Summary

### What We Built
AI Knowledge Manager is a production-ready RAG system that:
- Supports 4 LLM providers (OpenAI, Anthropic, Google Gemini, Ollama)
- Uses Qdrant for vector storage
- Implements semantic chunking for better context retrieval
- Provides both simple RAG and agent-based tool calling
- Handles streaming responses
- Includes comprehensive health monitoring

### Key Metrics
- **Code Efficiency:** 54% reduction moving from Semantic Kernel to Agent Framework
- **Test Coverage:** 578 tests (166 MCP + 412 KnowledgeManager)
- **Response Quality:** 8.7/10 average on comprehensive knowledge retrieval
- **Performance:** Sub-second vector search, ~2-5s full RAG response

### Critical Success Factors
1. ✅ **Semantic Chunking:** Better than naive text splitting (SemanticChunker.NET)
2. ✅ **Configuration-Driven:** No hardcoded values, all via appsettings.json
3. ✅ **Multi-Provider Support:** Avoid vendor lock-in
4. ✅ **Feature Flags:** Safe gradual rollout
5. ✅ **Comprehensive Testing:** Unit, integration, and smoke tests

---

## Core RAG Architecture

### High-Level Flow

```
User Query
    ↓
1. Vector Search (retrieve relevant chunks)
    ↓
2. Context Building (format retrieved chunks)
    ↓
3. Prompt Construction (system + context + user message)
    ↓
4. LLM Inference (OpenAI/Anthropic/Google/Ollama)
    ↓
5. Response Streaming (progressive output)
```

### Components

```
┌─────────────────────────────────────────────────────────────┐
│                     Chat Service Layer                       │
│  (SqliteChatService, MongoChatService)                      │
│  - Conversation management                                   │
│  - History management                                        │
│  - Provider routing                                          │
└──────────────────┬──────────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────────────────────────────┐
│                  ChatCompleteAF (Core RAG)                   │
│  - AskAsync() - Simple RAG                                   │
│  - AskWithAgentAsync() - RAG + Tool Calling                 │
│  - AskStreamingAsync() - Streaming RAG                       │
│  - AskWithAgentStreamingAsync() - Streaming + Tools         │
└──────────────────┬──────────────────────────────────────────┘
                   ↓
┌──────────────────┴──────────────────┐
│                                     │
▼                                     ▼
┌──────────────────────┐    ┌─────────────────────┐
│  KnowledgeManager    │    │   AgentFactory      │
│  - Vector search     │    │   - ChatClient      │
│  - Chunking          │    │   - Multi-provider  │
│  - Embeddings        │    │   - Tool calling    │
└──────────┬───────────┘    └─────────────────────┘
           ↓
┌─────────────────────────────────────────────────────────────┐
│                    Vector Store Layer                        │
│  - QdrantVectorStoreStrategy (primary)                      │
│  - MongoVectorStoreStrategy (alternative)                   │
│  - IVectorStoreStrategy (abstraction)                       │
└─────────────────────────────────────────────────────────────┘
```

### Key Files Reference

| Component | File Path | Lines | Description |
|-----------|-----------|-------|-------------|
| Core Chat | `KnowledgeEngine/ChatCompleteAF.cs` | 950+ | Main RAG implementation |
| Agent Factory | `KnowledgeEngine/Agents/AgentFramework/AgentFactory.cs` | 300+ | Multi-provider LLM creation |
| Knowledge Manager | `KnowledgeEngine/KnowledgeManager.cs` | 800+ | Vector search & document management |
| Vector Store | `KnowledgeEngine/Persistence/VectorStores/QdrantVectorStoreStrategy.cs` | 200+ | Qdrant integration |
| Chat Service | `KnowledgeEngine/Chat/SqliteChatService.cs` | 500+ | Conversation orchestration |

---

## Framework Decision Tree

### .NET 8 with Agent Framework (RECOMMENDED)

**✅ Use When:**
- Starting a new project or microservice
- Can deploy .NET 8 runtime
- Want latest features and performance
- Need multi-agent orchestration

**Benefits:**
- 54% less code than Semantic Kernel
- Direct ChatClient usage (no Kernel abstraction)
- Better streaming support
- Active development and support
- Microsoft's recommended path forward

**Packages:**
```xml
<PackageReference Include="Microsoft.Extensions.AI" Version="9.2.0" />
<PackageReference Include="Microsoft.Extensions.AI.Ollama" Version="9.2.0" />
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="9.2.0" />
<PackageReference Include="Azure.AI.Inference" Version="1.0.0" />
<PackageReference Include="Anthropic.SDK" Version="0.2.9" />
```

**Core Pattern:**
```csharp
// Create chat client directly
var chatClient = new ChatClientBuilder()
    .Use(new OpenAIClient(apiKey).AsChatClient(model))
    .Build();

// Simple invocation
var response = await chatClient.CompleteAsync(messages, cancellationToken: ct);
```

### .NET 4.8 with Semantic Kernel (FALLBACK)

**⚠️ Use When:**
- MUST support .NET 4.8 
- Cannot upgrade runtime yet
- Need stable, proven solution

**Challenges:**
- Semantic Kernel requires .NET 6.0 minimum (SK 1.0+)
- **CRITICAL:** Semantic Kernel does NOT support .NET 4.8

**Options for .NET 4.8:**

#### Option A: Separate Microservice (RECOMMENDED)
- Deploy RAG as separate .NET 8 service
- (.NET 4.8) app calls via HTTP API
- Best separation of concerns
- Can upgrade RAG independently

```
(.NET 4.8) app →  HTTP API  →  RAG Service (.NET 8)
    ↓                                         ↓
SQL Server                              Qdrant/Azure AI Search
```

#### Option B: Direct LLM SDK Integration
- Use provider SDKs directly in .NET 4.8
- Implement vector search manually
- More code, less abstraction
- Proven pattern (our health checkers use this)

**Packages (if attempting .NET 4.8):**
```xml
<!-- Direct SDKs that MAY support .NET Standard 2.0 -->
<PackageReference Include="Azure.AI.OpenAI" Version="1.0.0" />
<PackageReference Include="Anthropic.SDK" Version="0.2.9" />
```

#### Option C: Bridge Service (.NET 6/8 Console App)
- Run as Windows Service on same machine
- .net 4.8 app communicates via named pipes/HTTP
- RAG logic in modern .NET

### Decision Matrix

| Requirement | .NET 8 AF | .NET 8 SK | Separate Service | Direct SDK (.NET 4.8) |
|-------------|-----------|-----------|------------------|----------------------|
| Modern APIs | ✅ Best | ✅ Good | ✅ Best | ❌ Manual |
| Code Size | ✅ Smallest | 🟡 Larger | ✅ Smallest | ❌ Largest |
| .NET 4.8 | ❌ No | ❌ No | ✅ Yes (via API) | ✅ Yes |
| Complexity | 🟢 Low | 🟡 Medium | 🟡 Medium | 🔴 High |
| Maintenance | ✅ Easy | 🟡 Medium | 🟡 Medium | ❌ Hard |
| Tool Calling | ✅ Native | ✅ Native | ✅ Native | ❌ Manual |

**Recommendation for .NET 4.8 APP:** Option A (Separate Microservice)

---

## Vector Store Options

### Qdrant (Our Choice)

**Why We Chose It:**
- ✅ Open source, self-hostable
- ✅ Excellent performance (sub-second searches)
- ✅ Rich filtering capabilities
- ✅ Docker deployment
- ✅ Cloud option available (Qdrant Cloud)

**Deployment Options:**

#### 1. Docker Local (Development)
```yaml
version: '3.8'
services:
  qdrant:
    image: qdrant/qdrant:v1.7.4
    ports:
      - "6333:6333"
      - "6334:6334"
    volumes:
      - ./qdrant_storage:/qdrant/storage
```

**Pros:** Fast, cheap, full control
**Cons:** Requires Docker, manual scaling

#### 2. Qdrant Cloud (Production)
- Managed service
- Auto-scaling
- $0.50/GB/month (approximate)

**Setup:**
```csharp
var client = new QdrantClient(
    host: "xyz-example.eu-central.aws.cloud.qdrant.io",
    port: 6334,
    https: true,
    apiKey: "your-api-key"
);
```

### Azure AI Search (Alternative)

**Why Consider It:**
- ✅ Fully managed (Azure PaaS)
- ✅ Integrated with Azure ecosystem
- ✅ Hybrid search (vector + keyword)
- ✅ Built-in security (Azure AD)
- ⚠️ Higher cost ($250-$2500/month depending on tier)

**Setup:**
```csharp
var credential = new AzureKeyCredential(apiKey);
var client = new SearchClient(
    new Uri("https://your-service.search.windows.net"),
    "your-index-name",
    credential
);

// Vector search
var vectorQuery = new VectorizedQuery(embedding) {
    KNearestNeighborsCount = 10,
    Fields = { "contentVector" }
};

var results = await client.SearchAsync<SearchDocument>(
    searchText: null,
    new SearchOptions { VectorSearch = new() { Queries = { vectorQuery } } }
);
```

**Packages:**
```xml
<PackageReference Include="Azure.Search.Documents" Version="11.5.1" />
```

### MongoDB Vector Search (Alternative)

**Why Consider It:**
- ✅ If already using MongoDB
- ✅ Single database for documents + vectors
- ✅ Familiar query syntax
- ⚠️ Newer feature, less mature

**Our Implementation:** See `KnowledgeEngine/Persistence/VectorStores/MongoVectorStoreStrategy.cs`

### Comparison Matrix

| Feature | Qdrant | Azure AI Search | MongoDB Vector |
|---------|--------|-----------------|----------------|
| Self-Hosted | ✅ Easy | ❌ No | ✅ Yes |
| Cloud Option | ✅ Yes | ✅ Yes | ✅ Atlas |
| Cost (Small) | 🟢 Low | 🔴 High | 🟡 Medium |
| Performance | ✅ Excellent | ✅ Excellent | 🟡 Good |
| Filtering | ✅ Rich | ✅ Rich | 🟡 Basic |
| Hybrid Search | 🟡 Limited | ✅ Native | ❌ No |
| Setup Time | 🟢 5 min | 🟡 30 min | 🟡 20 min |

**Recommendation for TDI:**
- **Development:** Qdrant (Docker local)
- **Production:** Azure AI Search (if budget allows) OR Qdrant Cloud

---

## Document Management & Templates

### Our Markdown Structure

We use Markdown for knowledge base documents because:
- ✅ Easy to write and maintain
- ✅ Semantic chunking preserves structure
- ✅ Version control friendly (Git)
- ✅ Human-readable

### Document Template for TDI

```markdown
# [Module/Report Name] - User Guide

**Category:** [Dashboard/Report/Widget]
**Access Level:** [Admin/User/Power User]
**Last Updated:** [Date]

---

## Overview

Brief description of what this module/report does and why users would need it.

## Key Use Cases

1. **[Use Case 1]:** When to use this report
2. **[Use Case 2]:** What questions this answers
3. **[Use Case 3]:** Who typically uses this

## Access Instructions

**Navigation Path:** Dashboard > [Section] > [Subsection] > [Report Name]

**Direct URL (if applicable):** `/reports/[report-id]`

**Required Permissions:**
- Permission 1
- Permission 2

## Filters & Parameters

### [Filter Name 1]
- **Type:** [Dropdown/Date/Text]
- **Default:** [Value]
- **Description:** What this filter does
- **Customer-Specific Terms:**
  - Customer A calls this: [Term A]
  - Customer B calls this: [Term B]

### [Filter Name 2]
- **Type:** [Dropdown/Date/Text]
- **Default:** [Value]
- **Description:** What this filter does

## Report Sections

### Section 1: [Name]
Description of what data is shown here and what insights it provides.

### Section 2: [Name]
Description of what data is shown here and what insights it provides.

## Common Questions

**Q: How do I filter by [X]?**
A: Use the [Filter Name] dropdown at the top right. Select [value] to see [result].

**Q: What does [column/metric] mean?**
A: [Definition and context]

**Q: Why am I seeing [unexpected result]?**
A: Check [filter/permission/data issue]. Contact support if persists.

## Related Reports

- [Report 1]: Similar report for [different data]
- [Report 2]: Detailed drill-down from this report
- [Report 3]: Complementary data view

## Troubleshooting

### Issue: [Common problem]
**Solution:** [Steps to resolve]

### Issue: [Another common problem]
**Solution:** [Steps to resolve]

## Customer-Specific Configurations

### Customer A
- Custom terminology: [List]
- Special filters: [List]
- Additional sections: [List]

### Customer B
- Custom terminology: [List]
- Special filters: [List]
- Additional sections: [List]

---

**Keywords:** [keyword1], [keyword2], [keyword3]
**Tags:** #[category] #[feature] #[user-type]
```

### Document Generation Automation

**Approach:** SQL Server → Markdown Generator

```csharp
// Pseudocode for automated doc generation
public class ReportDocumentationGenerator
{
    public async Task<string> GenerateMarkdownFromDatabase(string reportId)
    {
        // 1. Query SQL Server for report metadata
        var report = await _db.QueryAsync<ReportMetadata>(
            "SELECT * FROM Reports WHERE ReportId = @id",
            new { id = reportId }
        );

        // 2. Get filters/parameters
        var filters = await _db.QueryAsync<FilterMetadata>(
            "SELECT * FROM ReportFilters WHERE ReportId = @id",
            new { id = reportId }
        );

        // 3. Get customer-specific terminology
        var terms = await _db.QueryAsync<CustomerTerminology>(
            "SELECT * FROM CustomerTerminology WHERE ReportId = @id",
            new { id = reportId }
        );

        // 4. Populate template
        var template = LoadTemplate("report-template.md");
        var markdown = template
            .Replace("{{REPORT_NAME}}", report.Name)
            .Replace("{{CATEGORY}}", report.Category)
            .Replace("{{OVERVIEW}}", report.Description);
            // ... etc

        return markdown;
    }
}
```

### Multi-Customer Strategy

**Option 1: Separate Knowledge Bases (RECOMMENDED)**
```
knowledge_bases/
  ├── customer_a_reports/
  │   ├── report_001.md
  │   └── report_002.md
  ├── customer_b_reports/
  │   ├── report_001.md
  │   └── report_002.md
  └── shared_core/
      └── common_features.md
```

**Benefits:**
- ✅ Complete isolation
- ✅ Easy to version control per customer
- ✅ No terminology conflicts

**Option 2: Single Knowledge Base with Metadata**
```markdown
# Report 001

**Customers:** A, B, C

## Customer-Specific Terms
- Customer A: "Revenue Report" → "Financial Summary"
- Customer B: "Revenue Report" → "Income Analysis"
```

**Benefits:**
- ✅ Less duplication
- ✅ Easier to maintain shared content
- ⚠️ Requires careful filtering at query time

**Recommendation:** Option 1 (Separate KBs) for TDI

---

## Multi-Provider LLM Support

### Why Multiple Providers?

1. **Cost Optimization:** Ollama for dev, OpenAI for production
2. **Redundancy:** Fallback if one provider is down
3. **Feature Parity:** Different models for different tasks
4. **Vendor Independence:** No lock-in

### Provider Configuration

**appsettings.json:**
```json
{
  "ChatCompleteSettings": {
    "DefaultProvider": "OpenAi",
    "ApiTemperature": 0.7,
    "OpenAi": {
      "ApiKey": "sk-...",
      "Model": "gpt-4o",
      "EmbeddingModel": "text-embedding-3-small"
    },
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "Model": "claude-sonnet-4-5-20250929"
    },
    "GoogleAI": {
      "ApiKey": "AI...",
      "Model": "gemini-2.0-flash-exp"
    },
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "Model": "llama3.2:latest",
      "EmbeddingModel": "mxbai-embed-large"
    }
  }
}
```

### AgentFactory Pattern

**File:** `KnowledgeEngine/Agents/AgentFramework/AgentFactory.cs`

```csharp
public class AgentFactory
{
    private readonly ChatCompleteSettings _settings;

    public ChatClient CreateChatClient(AiProvider provider, string? ollamaModel = null)
    {
        return provider switch
        {
            AiProvider.OpenAi => CreateOpenAIClient(),
            AiProvider.Anthropic => CreateAnthropicClient(),
            AiProvider.GoogleGemini => CreateGoogleClient(),
            AiProvider.Ollama => CreateOllamaClient(ollamaModel),
            _ => throw new ArgumentException($"Unsupported provider: {provider}")
        };
    }

    private ChatClient CreateOpenAIClient()
    {
        var client = new OpenAIClient(new ApiKeyCredential(_settings.OpenAi.ApiKey));
        return client.AsChatClient(_settings.OpenAi.Model);
    }

    private ChatClient CreateAnthropicClient()
    {
        var client = new AnthropicClient(_settings.Anthropic.ApiKey);
        return client.AsChatClient(_settings.Anthropic.Model);
    }

    // ... etc for other providers
}
```

### Provider Selection Strategy

**For TDI:**
```csharp
public class ProviderSelector
{
    public AiProvider SelectProvider(ChatRequest request)
    {
        // 1. User preference (if specified)
        if (request.PreferredProvider.HasValue)
            return request.PreferredProvider.Value;

        // 2. Customer-specific default
        var customerConfig = GetCustomerConfig(request.CustomerId);
        if (customerConfig.DefaultProvider.HasValue)
            return customerConfig.DefaultProvider.Value;

        // 3. Load-based routing (cost optimization)
        if (IsBusinessHours() && BudgetRemaining() > 0)
            return AiProvider.OpenAi; // Premium model
        else
            return AiProvider.Ollama; // Cost-effective

        // 4. Global default
        return _settings.DefaultProvider;
    }
}
```

---

## Chat Implementation Patterns

### Pattern 1: Simple RAG (No Tool Calling)

**Use Case:** User asks about TDI features → retrieve relevant docs → answer

```csharp
public async Task<string> AskAsync(
    string userMessage,
    string? knowledgeBaseId,
    List<ChatMessage> chatHistory,
    AiProvider provider,
    CancellationToken ct = default)
{
    // 1. Vector search for relevant context
    var searchResults = await _knowledgeManager.SearchAsync(
        knowledgeBaseId,
        userMessage,
        limit: 10,
        minRelevance: 0.3,
        ct
    );

    // 2. Build context from search results
    var context = BuildContext(searchResults);

    // 3. Construct prompt
    var systemMessage = $@"
You are a helpful assistant for the TDI telecommunications portal.
Use the following context to answer the user's question:

{context}

If the answer is not in the context, say so.
";

    // 4. Create chat client
    var chatClient = _agentFactory.CreateChatClient(provider);

    // 5. Build messages
    var messages = new List<ChatMessage>
    {
        new ChatMessage(ChatRole.System, systemMessage)
    };
    messages.AddRange(chatHistory);
    messages.Add(new ChatMessage(ChatRole.User, userMessage));

    // 6. Get completion
    var response = await chatClient.CompleteAsync(messages, cancellationToken: ct);

    return response.Message.Text ?? "No response from AI.";
}

private string BuildContext(IEnumerable<SearchResult> results)
{
    var sb = new StringBuilder();
    sb.AppendLine("--- RELEVANT DOCUMENTATION ---");

    foreach (var result in results)
    {
        sb.AppendLine($"[Relevance: {result.Score:F2}]");
        sb.AppendLine(result.Text);
        sb.AppendLine();
    }

    return sb.ToString();
}
```

### Pattern 2: RAG with Tool Calling

**Use Case:** User asks "Show me revenue reports" → AI calls `SearchReports` tool → returns specific reports

```csharp
public async Task<AgentChatResponse> AskWithAgentAsync(
    string userMessage,
    List<ChatMessage> chatHistory,
    AiProvider provider,
    CancellationToken ct = default)
{
    // 1. Define tools
    var tools = new Dictionary<string, Func<string, Task<string>>>
    {
        ["search_reports"] = async (query) => await SearchReportsAsync(query),
        ["get_report_url"] = async (reportName) => await GetReportUrlAsync(reportName),
        ["list_dashboards"] = async (_) => await ListDashboardsAsync()
    };

    // 2. Register tools with AI function factory
    var aiFunctions = tools.Select(kvp =>
        AIFunctionFactory.Create(kvp.Value, name: kvp.Key)
    ).ToList();

    // 3. Create agent with tools
    var chatClient = _agentFactory.CreateChatClient(provider);
    var agent = chatClient.CreateAIAgent(
        instructions: "You help users navigate the TDI portal.",
        tools: aiFunctions
    );

    // 4. Run agent
    var result = await agent.RunAsync(userMessage, cancellationToken: ct);

    return new AgentChatResponse
    {
        Response = result?.ToString() ?? "No response",
        ToolsCalled = result?.ToolCalls?.Select(tc => tc.Name).ToList() ?? []
    };
}
```

### Pattern 3: Streaming Responses

**Use Case:** Long responses, progressive UI updates

```csharp
public async IAsyncEnumerable<string> AskStreamingAsync(
    string userMessage,
    string? knowledgeBaseId,
    List<ChatMessage> chatHistory,
    AiProvider provider,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    // 1. Setup (same as Pattern 1)
    var searchResults = await _knowledgeManager.SearchAsync(...);
    var context = BuildContext(searchResults);
    var chatClient = _agentFactory.CreateChatClient(provider);
    var messages = BuildMessages(systemMessage, chatHistory, userMessage);

    // 2. Stream response
    await foreach (var chunk in chatClient.CompleteStreamingAsync(messages, cancellationToken: ct))
    {
        if (!string.IsNullOrEmpty(chunk.Text))
        {
            yield return chunk.Text;
        }
    }
}
```

### Conversation History Management

```csharp
public class ConversationManager
{
    private const int MaxHistoryMessages = 20;

    public List<ChatMessage> BuildChatHistory(
        string conversationId,
        string newUserMessage)
    {
        // 1. Load recent history from database
        var history = _db.GetRecentMessages(conversationId, MaxHistoryMessages);

        // 2. Convert to ChatMessage format
        var messages = history.Select(h => new ChatMessage(
            h.Role == "user" ? ChatRole.User : ChatRole.Assistant,
            h.Content
        )).ToList();

        // 3. Add new user message
        messages.Add(new ChatMessage(ChatRole.User, newUserMessage));

        // 4. Token counting and trimming (if needed)
        if (EstimateTokens(messages) > 4000)
        {
            messages = TrimOldestMessages(messages, targetTokens: 3000);
        }

        return messages;
    }
}
```

---

## Knowledge Base Management

### Document Ingestion Pipeline

```
Raw Document (MD/PDF/DOCX)
    ↓
1. Text Extraction
    ↓
2. Semantic Chunking (SemanticChunker.NET)
    ↓
3. Generate Embeddings (OpenAI/Ollama)
    ↓
4. Store in Vector DB (Qdrant)
    ↓
5. Store Metadata (SQL Server)
```

### Chunking Strategy (CRITICAL FOR RAG QUALITY)

**Our Evolution:**
- ❌ **Initial:** Naive text splitting (1000 chars, 200 overlap)
- ✅ **Current:** Semantic chunking (preserves meaning boundaries)

**SemanticChunker.NET Implementation:**

```csharp
// File: KnowledgeEngine/KnowledgeManager.cs

public async Task<List<Chunk>> ChunkDocumentAsync(
    string rawText,
    bool isMarkdown,
    CancellationToken ct = default)
{
    // 1. Configure semantic chunker
    var embedder = _embedderFactory.CreateEmbedder(); // OpenAI or Ollama
    var chunker = new SemanticChunker(embedder, new SemanticChunkerOptions
    {
        MaxChunkSize = 1500,        // Increased from 1000 (Phase 27 optimization)
        MinChunkSize = 300,
        OverlapSize = 300,          // Increased from 200
        SimilarityThreshold = 0.7   // Preserve semantic boundaries
    });

    // 2. Perform semantic chunking
    var chunks = await chunker.ChunkAsync(rawText, ct);

    // 3. Return chunks with metadata
    return chunks.Select((c, i) => new Chunk
    {
        Text = c.Text,
        Index = i,
        TokenCount = EstimateTokens(c.Text),
        EmbeddingVector = c.Embedding
    }).ToList();
}
```

**Key Parameters (from Milestone 27 optimization):**
- **MaxChunkSize:** 1500 tokens (was 1000) - better context
- **OverlapSize:** 300 tokens (was 200) - less fragmentation
- **SimilarityThreshold:** 0.7 - semantic boundary detection

### Embedding Generation

```csharp
public async Task<float[]> GenerateEmbeddingAsync(
    string text,
    AiProvider provider,
    CancellationToken ct = default)
{
    return provider switch
    {
        AiProvider.OpenAi => await GenerateOpenAIEmbedding(text, ct),
        AiProvider.Ollama => await GenerateOllamaEmbedding(text, ct),
        _ => throw new NotSupportedException($"Provider {provider} does not support embeddings")
    };
}

private async Task<float[]> GenerateOpenAIEmbedding(string text, CancellationToken ct)
{
    var client = new OpenAIClient(new ApiKeyCredential(_settings.OpenAi.ApiKey));
    var embedder = client.AsEmbeddingGenerator(_settings.OpenAi.EmbeddingModel);

    var embedding = await embedder.GenerateEmbeddingVectorAsync(text, cancellationToken: ct);

    return embedding.ToArray();
}
```

### Vector Search

```csharp
public async Task<List<SearchResult>> SearchAsync(
    string knowledgeBaseId,
    string query,
    int limit = 10,
    double minRelevance = 0.3,
    CancellationToken ct = default)
{
    // 1. Generate query embedding
    var queryEmbedding = await GenerateEmbeddingAsync(query, _defaultProvider, ct);

    // 2. Search vector store
    var vectorStore = _vectorStoreFactory.GetStrategy(); // Qdrant or Azure AI
    var results = await vectorStore.SearchAsync(
        collectionName: knowledgeBaseId,
        queryVector: queryEmbedding,
        limit: limit,
        scoreThreshold: minRelevance,
        ct
    );

    // 3. Hydrate with metadata (if needed)
    foreach (var result in results)
    {
        result.Metadata = await _db.GetChunkMetadataAsync(result.ChunkId, ct);
    }

    return results;
}
```

### Qdrant-Specific Implementation

```csharp
// File: KnowledgeEngine/Persistence/VectorStores/QdrantVectorStoreStrategy.cs

public class QdrantVectorStoreStrategy : IVectorStoreStrategy
{
    private readonly QdrantClient _client;

    public async Task CreateCollectionAsync(string collectionName, int vectorSize)
    {
        await _client.CreateCollectionAsync(
            collectionName: collectionName,
            vectorsConfig: new VectorParams
            {
                Size = (ulong)vectorSize,  // 1536 for OpenAI, 1024 for Ollama
                Distance = Distance.Cosine
            }
        );
    }

    public async Task UpsertAsync(
        string collectionName,
        string chunkId,
        float[] embedding,
        Dictionary<string, object> metadata)
    {
        var point = new PointStruct
        {
            Id = chunkId,
            Vectors = embedding,
            Payload = metadata.ToDictionary(
                kvp => kvp.Key,
                kvp => new Value { StringValue = kvp.Value.ToString() }
            )
        };

        await _client.UpsertAsync(collectionName, new[] { point });
    }

    public async Task<List<SearchResult>> SearchAsync(
        string collectionName,
        float[] queryVector,
        int limit,
        double scoreThreshold,
        CancellationToken ct)
    {
        var searchParams = new SearchParams
        {
            Limit = (ulong)limit,
            ScoreThreshold = (float)scoreThreshold
        };

        var results = await _client.SearchAsync(
            collectionName: collectionName,
            vector: queryVector,
            limit: (ulong)limit,
            scoreThreshold: (float)scoreThreshold,
            cancellationToken: ct
        );

        return results.Select(r => new SearchResult
        {
            ChunkId = r.Id.ToString(),
            Text = r.Payload["text"].StringValue,
            Score = r.Score,
            Metadata = r.Payload.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value.StringValue
            )
        }).ToList();
    }
}
```

---

## Testing Strategy

### Test Pyramid

```
         /\
        /  \    E2E Tests (5%)
       /────\   - Full workflow tests
      /      \
     /────────\ Integration Tests (25%)
    /          \ - DB + Vector + LLM
   /────────────\ Unit Tests (70%)
  /              \ - Pure logic, mocked dependencies
 /________________\
```

### Unit Test Example

```csharp
// File: KnowledgeManager.Tests/AgentFramework/ChatCompleteAFTests.cs

public class ChatCompleteAFTests
{
    [Fact]
    public async Task AskAsync_WithKnowledgeBase_RetrievesContext()
    {
        // Arrange
        var mockKnowledgeManager = new Mock<IKnowledgeManager>();
        mockKnowledgeManager
            .Setup(km => km.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult>
            {
                new() { Text = "TDI has 50+ reports", Score = 0.85 }
            });

        var mockAgentFactory = new Mock<IAgentFactory>();
        mockAgentFactory
            .Setup(af => af.CreateChatClient(It.IsAny<AiProvider>(), It.IsAny<string>()))
            .Returns(new FakeChatClient("Response with context"));

        var chatComplete = new ChatCompleteAF(
            mockKnowledgeManager.Object,
            mockAgentFactory.Object,
            Mock.Of<IOptions<ChatCompleteSettings>>()
        );

        // Act
        var response = await chatComplete.AskAsync(
            userMessage: "How many reports does TDI have?",
            knowledgeBaseId: "tdi-docs",
            chatHistory: new List<ChatMessage>(),
            apiTemperature: 0.7,
            provider: AiProvider.OpenAi
        );

        // Assert
        Assert.Contains("Response with context", response);
        mockKnowledgeManager.Verify(km => km.SearchAsync(
            "tdi-docs",
            "How many reports does TDI have?",
            10,
            0.3,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Integration Test Example

```csharp
public class RagIntegrationTests
{
    [Fact]
    public async Task EndToEnd_UserQuery_ReturnsRelevantAnswer()
    {
        // Arrange - Real services, test database
        var services = new ServiceCollection()
            .AddSingleton<IKnowledgeManager, KnowledgeManager>()
            .AddSingleton<IChatComplete, ChatCompleteAF>()
            .AddSingleton<IVectorStoreStrategy, QdrantVectorStoreStrategy>()
            // ... etc
            .BuildServiceProvider();

        var chatComplete = services.GetRequiredService<IChatComplete>();

        // Pre-populate test knowledge base
        await SeedTestKnowledgeBase("tdi-test-kb");

        // Act
        var response = await chatComplete.AskAsync(
            userMessage: "What is the Revenue Report used for?",
            knowledgeBaseId: "tdi-test-kb",
            chatHistory: new List<ChatMessage>(),
            apiTemperature: 0.7,
            provider: AiProvider.OpenAi
        );

        // Assert
        Assert.Contains("revenue", response.ToLower());
        Assert.Contains("financial", response.ToLower());
    }
}
```

### Smoke Test Example

```bash
#!/bin/bash
# tests/smoke/test_rag_basic.sh

echo "Testing basic RAG workflow..."

# 1. Upload document
KB_ID=$(curl -X POST http://localhost:7040/api/knowledge \
  -F "file=@test_doc.md" \
  -F "name=Test KB" | jq -r '.id')

echo "Created KB: $KB_ID"

# 2. Wait for processing
sleep 5

# 3. Query
RESPONSE=$(curl -X POST http://localhost:7040/api/chat \
  -H "Content-Type: application/json" \
  -d "{\"knowledgeId\":\"$KB_ID\",\"message\":\"What is this about?\",\"provider\":\"OpenAi\"}")

echo "Response: $RESPONSE"

# 4. Verify response is not empty
if [ -z "$RESPONSE" ]; then
  echo "FAIL: Empty response"
  exit 1
fi

echo "PASS: RAG workflow working"
```

### Test Coverage Targets

| Component | Target | Actual |
|-----------|--------|--------|
| Core Chat (ChatCompleteAF) | 80% | 85% ✅ |
| Knowledge Manager | 80% | 82% ✅ |
| Vector Store | 70% | 75% ✅ |
| Agent Factory | 90% | 92% ✅ |
| API Endpoints | 70% | 68% 🟡 |

**Our Stats:** 578 total tests (166 MCP + 412 KnowledgeManager)

---

## Deployment Considerations

### Architecture Options for TDI

#### Option 1: Microservice (RECOMMENDED)

```
┌─────────────────────────────────────────────────┐
│           TDI Portal (.NET 4.8)                 │
│  - Web UI (Knockout.js)                         │
│  - Business Logic                               │
│  - SQL Server                                   │
└────────────────┬────────────────────────────────┘
                 │ HTTP API
                 ↓
┌─────────────────────────────────────────────────┐
│         RAG Service (.NET 8)                    │
│  - ChatCompleteAF                               │
│  - KnowledgeManager                             │
│  - AgentFactory                                 │
└────────────────┬────────────────────────────────┘
                 │
     ┌───────────┴──────────┐
     ↓                      ↓
┌──────────┐         ┌─────────────┐
│  Qdrant  │         │ SQL Server  │
│ (Vectors)│         │ (Metadata)  │
└──────────┘         └─────────────┘
```

**Benefits:**
- ✅ Modern .NET 8 for RAG
- ✅ Independent scaling
- ✅ TDI remains on .NET 4.8
- ✅ Can deploy to separate machines

**Communication:**
```csharp
// TDI Portal (.NET 4.8) - Call RAG service
public async Task<string> AskChatbot(string userMessage, string customerId)
{
    using (var client = new HttpClient())
    {
        client.BaseAddress = new Uri("http://rag-service:5000");

        var request = new
        {
            message = userMessage,
            knowledgeBaseId = $"customer_{customerId}",
            provider = "OpenAi"
        };

        var response = await client.PostAsJsonAsync("/api/chat", request);
        var result = await response.Content.ReadAsAsync<ChatResponse>();

        return result.Response;
    }
}
```

#### Option 2: Shared SQL Server Strategy

```csharp
// TDI writes user queries to SQL table
// RAG service polls table, processes, writes responses

// TDI Portal
public void SendChatQuery(string message, string userId)
{
    _db.Execute(@"
        INSERT INTO ChatQueue (UserId, Message, Status, CreatedAt)
        VALUES (@userId, @message, 'Pending', GETDATE())",
        new { userId, message }
    );
}

public string? GetChatResponse(int queryId)
{
    return _db.QueryFirstOrDefault<string>(@"
        SELECT Response FROM ChatQueue
        WHERE Id = @queryId AND Status = 'Completed'",
        new { queryId }
    );
}

// RAG Service (background worker)
while (true)
{
    var pending = _db.Query<ChatQuery>("SELECT * FROM ChatQueue WHERE Status = 'Pending'");

    foreach (var query in pending)
    {
        var response = await _chatComplete.AskAsync(query.Message, ...);

        _db.Execute(@"
            UPDATE ChatQueue
            SET Response = @response, Status = 'Completed', CompletedAt = GETDATE()
            WHERE Id = @id",
            new { response, id = query.Id }
        );
    }

    await Task.Delay(1000);
}
```

### Docker Deployment

**docker-compose.yml:**
```yaml
version: '3.8'

services:
  qdrant:
    image: qdrant/qdrant:v1.7.4
    ports:
      - "6333:6333"
    volumes:
      - ./qdrant_storage:/qdrant/storage
    restart: unless-stopped

  rag-service:
    build: ./TdiRagService
    ports:
      - "5000:5000"
    environment:
      - OPENAI_API_KEY=${OPENAI_API_KEY}
      - ConnectionStrings__SqlServer=Server=tdi-db;Database=TDI;User Id=sa;Password=${SA_PASSWORD}
      - Qdrant__Endpoint=http://qdrant:6333
    depends_on:
      - qdrant
    restart: unless-stopped

  # Ollama (optional - for local LLM)
  ollama:
    image: ollama/ollama:latest
    ports:
      - "11434:11434"
    volumes:
      - ./ollama_data:/root/.ollama
    restart: unless-stopped
```

### Windows Service Deployment (Alternative)

```csharp
// Program.cs for Windows Service
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService() // <-- Enable Windows Service
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls("http://localhost:5000");
            });
}
```

**Install as Windows Service:**
```powershell
# Publish
dotnet publish -c Release -o C:\Services\TdiRagService

# Create service
sc create TdiRagService binPath="C:\Services\TdiRagService\TdiRagService.exe"
sc description TdiRagService "TDI RAG Chatbot Service"

# Start
sc start TdiRagService
```

### Health Checks

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddHealthChecks()
        .AddCheck<QdrantHealthCheck>("qdrant")
        .AddCheck<OpenAIHealthCheck>("openai")
        .AddCheck<SqlServerHealthCheck>("sqlserver");
}

public void Configure(IApplicationBuilder app)
{
    app.UseHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var json = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds
                })
            });
            await context.Response.WriteAsync(json);
        }
    });
}
```

---

## Code Examples & Patterns

### Complete RAG Service Example

```csharp
// File: TdiRagService/Services/TdiChatService.cs

public class TdiChatService : ITdiChatService
{
    private readonly IChatComplete _chatComplete;
    private readonly IKnowledgeManager _knowledgeManager;
    private readonly ITdiCustomerRepository _customerRepo;
    private readonly ILogger<TdiChatService> _logger;

    public TdiChatService(
        IChatComplete chatComplete,
        IKnowledgeManager knowledgeManager,
        ITdiCustomerRepository customerRepo,
        ILogger<TdiChatService> logger)
    {
        _chatComplete = chatComplete;
        _knowledgeManager = knowledgeManager;
        _customerRepo = customerRepo;
        _logger = logger;
    }

    public async Task<TdiChatResponse> AskAsync(TdiChatRequest request, CancellationToken ct)
    {
        try
        {
            // 1. Get customer-specific knowledge base
            var customer = await _customerRepo.GetByIdAsync(request.CustomerId, ct);
            var knowledgeBaseId = $"customer_{customer.Code}";

            // 2. Build conversation history
            var history = await BuildChatHistoryAsync(request.ConversationId, ct);

            // 3. Determine provider
            var provider = customer.PreferredProvider ?? AiProvider.OpenAi;

            // 4. Call RAG
            var response = await _chatComplete.AskAsync(
                userMessage: request.Message,
                knowledgeBaseId: knowledgeBaseId,
                chatHistory: history,
                apiTemperature: 0.7,
                provider: provider,
                useExtendedInstructions: true,
                ct: ct
            );

            // 5. Save conversation
            await SaveConversationAsync(request.ConversationId, request.Message, response, ct);

            // 6. Return response
            return new TdiChatResponse
            {
                Response = response,
                ConversationId = request.ConversationId,
                Provider = provider.ToString(),
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request for customer {CustomerId}",
                request.CustomerId);
            throw;
        }
    }

    private async Task<List<ChatMessage>> BuildChatHistoryAsync(
        string conversationId,
        CancellationToken ct)
    {
        var messages = await _customerRepo.GetConversationHistoryAsync(conversationId, ct);

        return messages.Select(m => new ChatMessage(
            m.Role == "user" ? ChatRole.User : ChatRole.Assistant,
            m.Content
        )).ToList();
    }

    private async Task SaveConversationAsync(
        string conversationId,
        string userMessage,
        string assistantResponse,
        CancellationToken ct)
    {
        await _customerRepo.SaveConversationAsync(new[]
        {
            new ConversationMessage
            {
                ConversationId = conversationId,
                Role = "user",
                Content = userMessage,
                Timestamp = DateTime.UtcNow
            },
            new ConversationMessage
            {
                ConversationId = conversationId,
                Role = "assistant",
                Content = assistantResponse,
                Timestamp = DateTime.UtcNow
            }
        }, ct);
    }
}
```

### API Controller Example

```csharp
// File: TdiRagService/Controllers/ChatController.cs

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ITdiChatService _chatService;

    public ChatController(ITdiChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<ActionResult<TdiChatResponse>> PostAsync(
        [FromBody] TdiChatRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message is required");

        if (string.IsNullOrWhiteSpace(request.CustomerId))
            return BadRequest("CustomerId is required");

        var response = await _chatService.AskAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("stream")]
    public async Task StreamAsync(
        [FromBody] TdiChatRequest request,
        CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";

        await foreach (var chunk in _chatService.AskStreamingAsync(request, ct))
        {
            await Response.WriteAsync($"data: {chunk}\n\n");
            await Response.Body.FlushAsync(ct);
        }
    }
}
```

### Dependency Injection Setup

```csharp
// File: Program.cs

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Services
builder.Services.Configure<ChatCompleteSettings>(
    builder.Configuration.GetSection("ChatCompleteSettings"));

builder.Services.AddSingleton<IAgentFactory, AgentFactory>();
builder.Services.AddSingleton<IKnowledgeManager, KnowledgeManager>();
builder.Services.AddSingleton<IChatComplete, ChatCompleteAF>();
builder.Services.AddScoped<ITdiChatService, TdiChatService>();

// Vector Store (Qdrant)
builder.Services.AddSingleton<IVectorStoreStrategy>(sp =>
{
    var config = sp.GetRequiredService<IOptions<QdrantSettings>>().Value;
    return new QdrantVectorStoreStrategy(
        endpoint: config.Endpoint,
        apiKey: config.ApiKey
    );
});

// Database (SQL Server)
builder.Services.AddScoped<ITdiCustomerRepository>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("SqlServer");
    return new TdiCustomerRepository(connectionString);
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<QdrantHealthCheck>("qdrant")
    .AddCheck<OpenAIHealthCheck>("openai");

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

---

## Common Pitfalls & Solutions

### Pitfall 1: Hardcoded Configuration Values

**❌ BAD:**
```csharp
var apiKey = "sk-proj-..."; // NEVER do this
var endpoint = "http://localhost:6333"; // NEVER do this
```

**✅ GOOD:**
```csharp
// appsettings.json
{
  "OpenAI": {
    "ApiKey": "sk-proj-..."
  },
  "Qdrant": {
    "Endpoint": "http://localhost:6333"
  }
}

// Code
var apiKey = _configuration["OpenAI:ApiKey"];
var endpoint = _configuration["Qdrant:Endpoint"];
```

### Pitfall 2: Not Handling Token Limits

**Problem:** Long context + long history = token overflow

**✅ Solution:**
```csharp
public List<ChatMessage> TrimToTokenLimit(
    List<ChatMessage> messages,
    int maxTokens = 4000)
{
    var totalTokens = 0;
    var result = new List<ChatMessage>();

    // Keep system message
    if (messages.FirstOrDefault()?.Role == ChatRole.System)
    {
        result.Add(messages[0]);
        totalTokens += EstimateTokens(messages[0].Content);
    }

    // Add messages from newest to oldest until limit
    for (int i = messages.Count - 1; i >= 0; i--)
    {
        var msg = messages[i];
        var msgTokens = EstimateTokens(msg.Content);

        if (totalTokens + msgTokens > maxTokens)
            break;

        result.Insert(0, msg);
        totalTokens += msgTokens;
    }

    return result;
}

private int EstimateTokens(string text)
{
    // Rough estimate: 1 token ≈ 4 characters
    return (int)Math.Ceiling(text.Length / 4.0);
}
```

### Pitfall 3: Not Testing with Real LLMs

**Problem:** Mocked tests pass, but real API calls fail

**✅ Solution:** Integration tests with real providers (use cheap models)

```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task RealOpenAI_SimpleQuery_ReturnsResponse()
{
    // Use real OpenAI with cheap model
    var chatComplete = new ChatCompleteAF(
        realKnowledgeManager,
        realAgentFactory,
        realSettings // gpt-4o-mini for testing
    );

    var response = await chatComplete.AskAsync(
        userMessage: "What is 2+2?",
        knowledgeBaseId: null,
        chatHistory: new List<ChatMessage>(),
        apiTemperature: 0.7,
        provider: AiProvider.OpenAi
    );

    Assert.Contains("4", response);
}
```

### Pitfall 4: Poor Vector Search Results

**Problem:** Search returns irrelevant chunks

**✅ Solutions:**

1. **Lower relevance threshold** (0.3 → 0.25)
2. **Increase result limit** (5 → 10)
3. **Use semantic chunking** (not naive splitting)
4. **Better embedding model** (text-embedding-3-small vs ada-002)
5. **Rerank results** (optional post-processing)

```csharp
public async Task<List<SearchResult>> SearchWithReranking(
    string knowledgeBaseId,
    string query,
    CancellationToken ct)
{
    // 1. Initial vector search (broad)
    var candidates = await _vectorStore.SearchAsync(
        knowledgeBaseId,
        query,
        limit: 20,           // Get more candidates
        minRelevance: 0.2,   // Lower threshold
        ct
    );

    // 2. Rerank with cross-encoder (optional)
    var reranked = await RerankResults(query, candidates, ct);

    // 3. Return top 10
    return reranked.Take(10).ToList();
}
```

### Pitfall 5: Not Monitoring Costs

**Problem:** Surprise $1000+ OpenAI bill

**✅ Solution:** Track usage and set alerts

```csharp
public class UsageTracker
{
    private readonly ILogger<UsageTracker> _logger;

    public async Task TrackCompletionAsync(
        AiProvider provider,
        int promptTokens,
        int completionTokens,
        string customerId)
    {
        var cost = CalculateCost(provider, promptTokens, completionTokens);

        await _db.ExecuteAsync(@"
            INSERT INTO UsageTracking
            (CustomerId, Provider, PromptTokens, CompletionTokens, Cost, Timestamp)
            VALUES (@customerId, @provider, @promptTokens, @completionTokens, @cost, GETDATE())",
            new { customerId, provider = provider.ToString(), promptTokens, completionTokens, cost }
        );

        _logger.LogInformation(
            "Usage tracked: Customer {CustomerId}, Provider {Provider}, Cost ${Cost:F4}",
            customerId, provider, cost
        );

        // Alert if daily cost exceeds threshold
        var dailyCost = await GetDailyCostAsync(customerId);
        if (dailyCost > 100.0m)
        {
            _logger.LogWarning(
                "High usage alert: Customer {CustomerId} has spent ${Cost:F2} today",
                customerId, dailyCost
            );
        }
    }

    private decimal CalculateCost(AiProvider provider, int promptTokens, int completionTokens)
    {
        // OpenAI GPT-4o pricing (as of Jan 2026)
        return provider switch
        {
            AiProvider.OpenAi =>
                (promptTokens * 0.0025m / 1000) +    // $2.50 per 1M prompt tokens
                (completionTokens * 0.01m / 1000),   // $10 per 1M completion tokens
            AiProvider.Anthropic =>
                (promptTokens * 0.003m / 1000) +
                (completionTokens * 0.015m / 1000),
            AiProvider.Ollama => 0m, // Free (self-hosted)
            _ => 0m
        };
    }
}
```

### Pitfall 6: Not Handling Streaming Errors

**Problem:** Stream disconnects mid-response

**✅ Solution:**
```csharp
public async IAsyncEnumerable<string> AskStreamingAsync(
    string userMessage,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    var retryCount = 0;
    const int maxRetries = 3;

    while (retryCount < maxRetries)
    {
        try
        {
            await foreach (var chunk in InternalStreamAsync(userMessage, ct))
            {
                yield return chunk;
            }
            yield break; // Success, exit
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            retryCount++;
            if (retryCount >= maxRetries)
            {
                _logger.LogError(ex, "Streaming failed after {Retries} retries", retryCount);
                yield return "\n\n[Error: Failed to complete response. Please try again.]";
                yield break;
            }

            _logger.LogWarning("Streaming interrupted, retrying ({Retry}/{Max})...",
                retryCount, maxRetries);
            await Task.Delay(1000 * retryCount, ct); // Exponential backoff
        }
    }
}
```

---

## Performance Optimization

### Current Metrics (from ChatComplete project)

| Metric | Value | Target |
|--------|-------|--------|
| Vector search latency | 50-200ms | < 500ms |
| Embedding generation | 200-500ms | < 1s |
| Full RAG response (OpenAI) | 2-5s | < 10s |
| Full RAG response (Ollama) | 5-15s | < 20s |
| Concurrent requests | 20/s | 50/s |

### Optimization 1: Caching Embeddings

**Problem:** Regenerating same query embeddings wastes time/money

**✅ Solution:**
```csharp
public class EmbeddingCache
{
    private readonly IMemoryCache _cache;
    private readonly IEmbeddingGenerator _generator;

    public async Task<float[]> GetOrGenerateAsync(string text, CancellationToken ct)
    {
        var cacheKey = $"embedding:{ComputeHash(text)}";

        if (_cache.TryGetValue<float[]>(cacheKey, out var cached))
        {
            return cached;
        }

        var embedding = await _generator.GenerateAsync(text, ct);

        _cache.Set(cacheKey, embedding, TimeSpan.FromHours(24));

        return embedding;
    }

    private string ComputeHash(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
```

### Optimization 2: Parallel Search

**Problem:** Searching multiple knowledge bases sequentially is slow

**✅ Solution:**
```csharp
public async Task<List<SearchResult>> SearchMultipleKnowledgeBasesAsync(
    List<string> knowledgeBaseIds,
    string query,
    CancellationToken ct)
{
    // Generate embedding once
    var queryEmbedding = await _embedder.GenerateAsync(query, ct);

    // Search all KBs in parallel
    var tasks = knowledgeBaseIds.Select(kbId =>
        _vectorStore.SearchAsync(kbId, queryEmbedding, limit: 5, minRelevance: 0.3, ct)
    );

    var results = await Task.WhenAll(tasks);

    // Merge and sort by relevance
    return results
        .SelectMany(r => r)
        .OrderByDescending(r => r.Score)
        .Take(10)
        .ToList();
}
```

### Optimization 3: Batch Chunking

**Problem:** Processing documents one chunk at a time is slow

**✅ Solution:**
```csharp
public async Task IndexDocumentAsync(string documentId, string text, CancellationToken ct)
{
    // 1. Chunk document
    var chunks = await _chunker.ChunkAsync(text, ct);

    // 2. Generate embeddings in batches
    const int batchSize = 100;
    for (int i = 0; i < chunks.Count; i += batchSize)
    {
        var batch = chunks.Skip(i).Take(batchSize).ToList();

        // Parallel embedding generation
        var embeddingTasks = batch.Select(chunk =>
            _embedder.GenerateAsync(chunk.Text, ct)
        );
        var embeddings = await Task.WhenAll(embeddingTasks);

        // Batch upsert to Qdrant
        var points = batch.Zip(embeddings, (chunk, embedding) => new PointStruct
        {
            Id = $"{documentId}_{chunk.Index}",
            Vectors = embedding,
            Payload = new Dictionary<string, Value>
            {
                ["text"] = chunk.Text,
                ["documentId"] = documentId
            }
        });

        await _vectorStore.UpsertBatchAsync(points, ct);
    }
}
```

### Optimization 4: RAG Parameter Tuning (Milestone 27)

**From our optimization phase:**

```csharp
// Before (baseline: 8.7/10 quality)
var results = await _knowledgeManager.SearchAsync(
    knowledgeId,
    query,
    limit: 5,              // Too few results
    minRelevance: 0.3      // Missing relevant chunks
);

// After (target: 9.5/10 quality)
var results = await _knowledgeManager.SearchAsync(
    knowledgeId,
    query,
    limit: 10,             // More comprehensive retrieval
    minRelevance: 0.25     // Capture borderline-relevant chunks
);

// Chunking improvements
var chunker = new SemanticChunker(embedder, new SemanticChunkerOptions
{
    MaxChunkSize = 1500,   // Up from 1000 (better context)
    MinChunkSize = 300,
    OverlapSize = 300,     // Up from 200 (less fragmentation)
    SimilarityThreshold = 0.7
});
```

---

## Multi-Tenancy & Customer Customization

### Strategy 1: Separate Knowledge Bases per Customer (RECOMMENDED)

```csharp
public class CustomerKnowledgeBaseService
{
    public async Task<string> GetKnowledgeBaseIdAsync(string customerId, CancellationToken ct)
    {
        // Convention: customer_{customerId}
        return $"customer_{customerId}";
    }

    public async Task CreateCustomerKnowledgeBaseAsync(
        string customerId,
        List<ReportMetadata> reports,
        CancellationToken ct)
    {
        var kbId = await GetKnowledgeBaseIdAsync(customerId, ct);

        // 1. Generate markdown documents from reports
        var documents = reports.Select(r =>
            _docGenerator.GenerateMarkdownFromReport(r, customerId)
        );

        // 2. Create knowledge base
        await _knowledgeManager.CreateKnowledgeBaseAsync(kbId, ct);

        // 3. Index documents
        foreach (var doc in documents)
        {
            await _knowledgeManager.AddDocumentAsync(kbId, doc, ct);
        }
    }

    public async Task UpdateCustomerTerminologyAsync(
        string customerId,
        Dictionary<string, string> terminology,
        CancellationToken ct)
    {
        // Store customer-specific terms
        await _db.ExecuteAsync(@"
            INSERT INTO CustomerTerminology (CustomerId, StandardTerm, CustomerTerm)
            VALUES (@customerId, @standardTerm, @customerTerm)
            ON CONFLICT (CustomerId, StandardTerm)
            DO UPDATE SET CustomerTerm = @customerTerm",
            terminology.Select(kvp => new
            {
                customerId,
                standardTerm = kvp.Key,
                customerTerm = kvp.Value
            })
        );

        // Regenerate customer knowledge base with new terms
        var reports = await GetCustomerReportsAsync(customerId, ct);
        await CreateCustomerKnowledgeBaseAsync(customerId, reports, ct);
    }
}
```

### Strategy 2: Metadata Filtering

```csharp
public async Task<List<SearchResult>> SearchWithCustomerFilterAsync(
    string knowledgeBaseId,
    string query,
    string customerId,
    CancellationToken ct)
{
    var queryEmbedding = await _embedder.GenerateAsync(query, ct);

    // Qdrant filter by customer
    var filter = new Filter
    {
        Must = new List<Condition>
        {
            new Condition
            {
                Field = "customerId",
                Match = new Match { Value = customerId }
            }
        }
    };

    var results = await _qdrantClient.SearchAsync(
        collectionName: knowledgeBaseId,
        vector: queryEmbedding,
        filter: filter,
        limit: 10,
        cancellationToken: ct
    );

    return results.Select(MapToSearchResult).ToList();
}
```

### Automated Knowledge Base Generation

```csharp
public class AutomatedKnowledgeBaseBuilder
{
    public async Task RebuildAllCustomerKnowledgeBasesAsync(CancellationToken ct)
    {
        var customers = await _db.QueryAsync<Customer>("SELECT * FROM Customers");

        foreach (var customer in customers)
        {
            _logger.LogInformation("Rebuilding KB for customer {CustomerId}", customer.Id);

            try
            {
                // 1. Load report metadata from SQL Server
                var reports = await LoadReportsFromDatabase(customer.Id, ct);

                // 2. Load customer-specific terminology
                var terminology = await LoadCustomerTerminology(customer.Id, ct);

                // 3. Generate markdown documents
                var documents = new List<string>();
                foreach (var report in reports)
                {
                    var markdown = GenerateReportMarkdown(report, terminology);
                    documents.Add(markdown);
                }

                // 4. Create/update knowledge base
                var kbId = $"customer_{customer.Code}";
                await RecreateKnowledgeBaseAsync(kbId, documents, ct);

                _logger.LogInformation("KB rebuilt for customer {CustomerId}", customer.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rebuild KB for customer {CustomerId}",
                    customer.Id);
            }
        }
    }

    private string GenerateReportMarkdown(
        ReportMetadata report,
        Dictionary<string, string> terminology)
    {
        var template = LoadTemplate("report-template.md");

        // Replace standard terms with customer-specific terms
        foreach (var (standard, custom) in terminology)
        {
            template = template.Replace($"{{TERM:{standard}}}", custom);
        }

        return template
            .Replace("{{REPORT_NAME}}", report.Name)
            .Replace("{{REPORT_DESCRIPTION}}", report.Description)
            .Replace("{{FILTERS}}", GenerateFiltersSection(report.Filters))
            .Replace("{{NAVIGATION}}", GenerateNavigationPath(report));
    }
}
```

---

## Conclusion & Next Steps

### Summary

This guide captures 41.5 hours of development learnings from the AI Knowledge Manager project, covering:

- ✅ Core RAG architecture (ChatCompleteAF, KnowledgeManager, AgentFactory)
- ✅ Framework decision tree (.NET 8 vs .NET 4.8)
- ✅ Vector store options (Qdrant, Azure AI Search, MongoDB)
- ✅ Multi-provider LLM support (OpenAI, Anthropic, Google, Ollama)
- ✅ Document management and templates
- ✅ Testing strategy (578 tests, 100% pass rate)
- ✅ Performance optimization (8.7/10 → 9.5/10 target)
- ✅ Multi-tenancy patterns
- ✅ Common pitfalls and solutions

### Recommended Approach for TDI

1. **Architecture:** Separate .NET 8 microservice (RAG API)
2. **Vector Store:** Qdrant (Docker local for dev, cloud for prod)
3. **LLM Provider:** OpenAI (GPT-4o-mini for cost efficiency)
4. **Knowledge Base Strategy:** Separate KBs per customer
5. **Automation:** SQL Server → Markdown generation → Auto-indexing
6. **Testing:** Start with smoke tests, expand to integration tests

### Implementation Phases for TDI

**Phase 1: Proof of Concept (1 week)**
- [ ] Clone ChatComplete project to Windows 11 machine
- [ ] Set up local Qdrant (Docker Desktop)
- [ ] Create single test knowledge base (1 customer, 3 reports)
- [ ] Build minimal .NET 8 RAG API
- [ ] Test end-to-end workflow

**Phase 2: Template Development (1 week)**
- [ ] Design report markdown template
- [ ] Build SQL → Markdown generator
- [ ] Create terminology mapping system
- [ ] Generate KBs for 2-3 test customers

**Phase 3: Integration (2 weeks)**
- [ ] Build HTTP API for TDI (.NET 4.8) to call RAG service
- [ ] Implement conversation history
- [ ] Add streaming support
- [ ] Create admin UI for KB management

**Phase 4: Production (2 weeks)**
- [ ] Deploy to test environment
- [ ] Performance testing
- [ ] Security review
- [ ] Rollout to pilot customers

### Key Success Metrics for TDI

| Metric | Target | How to Measure |
|--------|--------|----------------|
| Response Quality | 8.5/10 | Manual evaluation on 50 queries |
| Response Time | < 5s | P95 latency monitoring |
| User Adoption | 30% of users | Analytics tracking |
| Support Ticket Reduction | 20% | Compare pre/post deployment |
| Cost per Query | < $0.05 | OpenAI usage tracking |

### Resources for TDI Team

**From ChatComplete Project:**
- `KnowledgeEngine/ChatCompleteAF.cs` - Core RAG implementation
- `KnowledgeEngine/Agents/AgentFramework/AgentFactory.cs` - Multi-provider support
- `KnowledgeEngine/KnowledgeManager.cs` - Vector search and document management
- `KnowledgeEngine/Persistence/VectorStores/QdrantVectorStoreStrategy.cs` - Qdrant integration
- `KnowledgeManager.Tests/` - 412 unit tests for reference

**External Documentation:**
- Microsoft Agent Framework: https://learn.microsoft.com/en-us/agent-framework/
- Qdrant Documentation: https://qdrant.tech/documentation/
- OpenAI API Reference: https://platform.openai.com/docs/api-reference
- SemanticChunker.NET: https://github.com/microbian-systems/SemanticChunker.NET

---

**Document Version:** 1.0
**Last Updated:** 2026-01-12
**Total Pages:** 50+
**Source Project:** AI Knowledge Manager (ChatComplete)
**Target Project:** TDI Telecommunications Portal
**Author:** Wayne

**Questions?** Refer to this guide or the source code at `/home/wayne/repos/ChatComplete`
