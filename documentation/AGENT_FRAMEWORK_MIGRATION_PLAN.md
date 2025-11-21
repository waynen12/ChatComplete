# Semantic Kernel to Microsoft Agent Framework Migration Plan

**Project:** AI Knowledge Manager
**Status:** Planning Phase
**Target Timeline:** TBD (After exploratory sample projects)
**Estimated Effort:** 40-60 hours

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current State Analysis](#current-state-analysis)
3. [Migration Benefits](#migration-benefits)
4. [Key Concept Mapping](#key-concept-mapping)
5. [Affected Files Inventory](#affected-files-inventory)
6. [Migration Phases](#migration-phases)
7. [Code Transformation Examples](#code-transformation-examples)
8. [Risk Assessment](#risk-assessment)
9. [Pre-Migration Checklist](#pre-migration-checklist)
10. [Success Criteria](#success-criteria)
11. [Components That Stay As-Is](#components-that-stay-as-is)
12. [Text Chunking Considerations](#text-chunking-considerations)

---

## Executive Summary

### What is Microsoft Agent Framework?

Microsoft Agent Framework is the **unified successor to Semantic Kernel and AutoGen**, combining their strengths:
- **From Semantic Kernel:** Type-safe skills, state/threads, filters, telemetry, enterprise model support
- **From AutoGen:** Simple multi-agent patterns, explicit workflow control

Think of it as **Semantic Kernel v2.0** - built by the same team, designed to be the future of AI agent development in .NET and Python.

### Why Migrate?

**Now:**
- Semantic Kernel will continue to receive critical bug fixes and security patches
- New features will be built for Agent Framework
- SK support guaranteed for at least 1 year after AF reaches GA

**Future:**
- Agent Framework is where all innovation is happening
- Better multi-agent orchestration patterns
- Simplified API (no more Kernel abstraction layer)
- Unified tool/function model

### Migration Summary

| Metric | Current (SK) | After (AF) |
|--------|--------------|------------|
| Files Affected | 29 | - |
| Agent Plugins | 4 | 4 (tools) |
| Kernel Factory | 1 | Removed |
| Chat Services | 2 | 2 (simplified) |
| NuGet Packages | ~8 SK packages | ~4 AF packages |

---

## Current State Analysis

### Semantic Kernel Usage in ChatComplete

Your codebase uses Semantic Kernel for:

1. **Multi-Provider LLM Integration**
   - OpenAI, Azure OpenAI, Anthropic, Google AI, Ollama
   - Via `KernelFactory` and `KernelHelper`

2. **Agent Plugins** (4 plugins with KernelFunction attributes)
   - `CrossKnowledgeSearchPlugin` - Search across knowledge bases
   - `KnowledgeAnalyticsAgent` - Knowledge base analytics
   - `ModelRecommendationAgent` - Model recommendations
   - `SystemHealthAgent` - System health checks

3. **Chat Completion**
   - `ChatComplete.cs` - Main chat orchestration
   - Provider selection and kernel configuration
   - Tool calling with automatic function invocation

4. **Vector Store Integration**
   - Qdrant vector store via SK connectors
   - Embedding generation

5. **Chat History Management**
   - ChatHistory from SK
   - History reduction/trimming

### Files Using Semantic Kernel (29 total)

**Core Engine (13 files):**
- `KnowledgeEngine/KernelFactory.cs` - Kernel creation per provider
- `KnowledgeEngine/KernelHelper.cs` - Kernel configuration helpers
- `KnowledgeEngine/ChatComplete.cs` - Main chat with tool calling
- `KnowledgeEngine/AutoInvoke.cs` - Auto function invocation settings
- `KnowledgeEngine/KnowledgeManager.cs` - Knowledge base management
- `KnowledgeEngine/EmbeddingsHelper.cs` - Embedding generation
- `KnowledgeEngine/Extensions/ServiceCollectionExtensions.cs` - DI registration

**Agent Plugins (4 files):**
- `KnowledgeEngine/Agents/Plugins/CrossKnowledgeSearchPlugin.cs`
- `KnowledgeEngine/Agents/Plugins/KnowledgeAnalyticsAgent.cs`
- `KnowledgeEngine/Agents/Plugins/ModelRecommendationAgent.cs`
- `KnowledgeEngine/Agents/Plugins/SystemHealthAgent.cs`

**Chat Services (2 files):**
- `KnowledgeEngine/Chat/SqliteChatService.cs`
- `KnowledgeEngine/Chat/MongoChatService.cs`

**Persistence (2 files):**
- `KnowledgeEngine/Persistence/IndexManagers/QdrantIndexManager.cs`
- `KnowledgeEngine/Persistence/VectorStores/QdrantVectorStoreStrategy.cs`

**Health Checkers (3 files):**
- `KnowledgeEngine/Services/HealthCheckers/OpenAIHealthChecker.cs`
- `KnowledgeEngine/Services/HealthCheckers/AnthropicHealthChecker.cs`
- `KnowledgeEngine/Services/HealthCheckers/GoogleAIHealthChecker.cs`

**Tests (8 files):**
- `KnowledgeManager.Tests/KnowledgeEngine/SpyChatComplete.cs`
- `KnowledgeManager.Tests/KnowledgeEngine/KernerlSelectionTests.cs`
- `KnowledgeManager.Tests/KnowledgeEngine/ChatHistoryReducerTests.cs`
- `KnowledgeManager.Tests/KnowledgeEngine/KnowledgeAnalyticsAgentTests.cs`
- `KnowledgeManager.Tests/KnowledgeEngine/ModelRecommendationAgentTests.cs`
- `KnowledgeManager.Tests/KnowledgeEngine/HealthCheckers/*Tests.cs` (3 files)

**MCP (2 files):**
- `Knowledge.Mcp/TestQdrantCollections.cs`
- `Knowledge.Mcp.Tests/QdrantConnectionTests.cs`

**Other (1 file):**
- `Summarizer.cs`

---

## Migration Benefits

### Immediate Benefits

1. **Simplified Agent Creation**
   ```csharp
   // Before (SK): Complex setup with Kernel, plugins, settings
   var kernel = Kernel.CreateBuilder()
       .AddOpenAIChatCompletion(modelId, apiKey)
       .Build();
   kernel.Plugins.Add(plugin);
   var agent = new ChatCompletionAgent { Kernel = kernel, Instructions = "..." };

   // After (AF): Direct, simple
   var agent = chatClient.CreateAIAgent(
       instructions: "...",
       tools: [AIFunctionFactory.Create(MyFunction)]
   );
   ```

2. **No More Kernel Abstraction**
   - Remove `KernelFactory`, `KernelHelper`
   - Direct `ChatClient` usage
   - Cleaner dependency injection

3. **Unified Tool Model**
   - Remove `[KernelFunction]` attributes
   - Use simple functions with descriptions
   - Better IDE support

4. **Better Multi-Agent Patterns**
   - Sequential, concurrent, hand-off orchestration built-in
   - Type-safe message routing
   - State checkpointing for long-running workflows

### Future Benefits

1. **Active Development**
   - All new features in Agent Framework
   - Better community support
   - More samples and documentation

2. **MCP Integration**
   - First-class MCP client support
   - Agent Framework + MCP = powerful tool ecosystem

3. **Enterprise Features**
   - Better telemetry and observability
   - Workflow state persistence
   - Human-in-the-loop patterns

---

## Key Concept Mapping

### Core Concepts

| Semantic Kernel | Agent Framework | Notes |
|-----------------|-----------------|-------|
| `Kernel` | Removed | Use `ChatClient` directly |
| `KernelBuilder` | `ChatClient.CreateAIAgent()` | Simpler creation |
| `ChatCompletionAgent` | `ChatClientAgent` | Unified agent type |
| `KernelFunction` | `AIFunction` | No more attributes required |
| `KernelPlugin` | Tool collection | Just pass functions |
| `ChatHistory` | `AgentThread` | Thread-based state |
| `InvokeAsync` | `RunAsync` | Method renamed |
| `KernelArguments` | Direct parameters | Simplified |

### Provider Mapping

| SK Class | AF Class |
|----------|----------|
| `OpenAIChatCompletion` | `OpenAI.ChatClient` |
| `AzureOpenAIChatCompletion` | `AzureAI.ChatClient` |
| `AnthropicChatCompletion` | `Microsoft.Extensions.AI` adapter |
| `GoogleAIChatCompletion` | `Microsoft.Extensions.AI` adapter |
| `OllamaChatCompletion` | `Microsoft.Extensions.AI.Ollama` |

### Package Mapping

| SK Package | AF Package |
|------------|------------|
| `Microsoft.SemanticKernel` | `Microsoft.Agents.AI` |
| `Microsoft.SemanticKernel.Connectors.OpenAI` | `Microsoft.Agents.AI.OpenAI` |
| `Microsoft.SemanticKernel.Connectors.Qdrant` | TBD (may remain SK package) |
| `Microsoft.SemanticKernel.Planners.*` | Workflows built-in |

---

## Affected Files Inventory

### Phase 1: Foundation (Must Do First)

| File | Changes | Effort | Risk |
|------|---------|--------|------|
| `KernelFactory.cs` | **DELETE** - Replace with ChatClient providers | 4h | High |
| `KernelHelper.cs` | **DELETE** - Logic absorbed into new agent setup | 2h | Medium |
| `ServiceCollectionExtensions.cs` | Update DI registration for new types | 3h | High |
| `ChatComplete.cs` | Major rewrite - core chat orchestration | 8h | High |

### Phase 2: Plugins → Tools

| File | Changes | Effort | Risk |
|------|---------|--------|------|
| `CrossKnowledgeSearchPlugin.cs` | Remove `[KernelFunction]`, convert to `AIFunction` | 2h | Low |
| `KnowledgeAnalyticsAgent.cs` | Remove attributes, simplify | 2h | Low |
| `ModelRecommendationAgent.cs` | Remove attributes, simplify | 2h | Low |
| `SystemHealthAgent.cs` | Remove attributes, simplify | 2h | Low |

### Phase 3: Chat Services

| File | Changes | Effort | Risk |
|------|---------|--------|------|
| `SqliteChatService.cs` | Update ChatHistory → AgentThread | 3h | Medium |
| `MongoChatService.cs` | Update ChatHistory → AgentThread | 3h | Medium |

### Phase 4: Vector Store & Embeddings

| File | Changes | Effort | Risk |
|------|---------|--------|------|
| `QdrantIndexManager.cs` | May remain SK (check AF support) | 2h | Medium |
| `QdrantVectorStoreStrategy.cs` | May remain SK (check AF support) | 2h | Medium |
| `EmbeddingsHelper.cs` | Update to AF embedding model | 2h | Low |

### Phase 5: Health Checkers

| File | Changes | Effort | Risk |
|------|---------|--------|------|
| `OpenAIHealthChecker.cs` | Update client creation | 1h | Low |
| `AnthropicHealthChecker.cs` | Update client creation | 1h | Low |
| `GoogleAIHealthChecker.cs` | Update client creation | 1h | Low |

### Phase 6: Tests

| File | Changes | Effort | Risk |
|------|---------|--------|------|
| `SpyChatComplete.cs` | Update to match new ChatComplete | 2h | Medium |
| `KernerlSelectionTests.cs` | Rewrite for new architecture | 2h | Medium |
| `ChatHistoryReducerTests.cs` | Update for AgentThread | 1h | Low |
| `KnowledgeAnalyticsAgentTests.cs` | Update for AIFunction | 1h | Low |
| `ModelRecommendationAgentTests.cs` | Update for AIFunction | 1h | Low |
| Health checker tests (3) | Update client mocking | 2h | Low |

### Phase 7: MCP Integration

| File | Changes | Effort | Risk |
|------|---------|--------|------|
| `TestQdrantCollections.cs` | Minor updates | 1h | Low |
| `QdrantConnectionTests.cs` | Minor updates | 1h | Low |

---

## Migration Phases

### Phase 0: Exploration (Current - You Are Here)

**Goal:** Learn Agent Framework through sample projects

**Tasks:**
- [ ] Create sample project with basic chat agent
- [ ] Try tool/function registration
- [ ] Test multi-agent workflow
- [ ] Experiment with different providers (OpenAI, Ollama)
- [ ] Understand thread/state management
- [ ] Document learnings and gotchas

**Duration:** 1-2 weeks (depending on exploration depth)

---

### Phase 1: Foundation Setup (8 hours)

**Goal:** Replace Kernel infrastructure with Agent Framework

**Tasks:**
- [ ] Add Agent Framework NuGet packages
- [ ] Create new `ChatClientProvider.cs` (replaces `KernelFactory`)
- [ ] Update `ServiceCollectionExtensions.cs` for new DI
- [ ] Remove `KernelHelper.cs`
- [ ] Update configuration structure if needed

**Deliverables:**
- ✅ Agent Framework packages installed
- ✅ DI container configured for AF
- ✅ Basic agent creation working

---

### Phase 2: Core Chat Migration (12 hours)

**Goal:** Migrate `ChatComplete.cs` to Agent Framework

**Tasks:**
- [ ] Rewrite `ChatComplete.cs` using `ChatClientAgent`
- [ ] Update tool registration pattern
- [ ] Migrate chat history to AgentThread
- [ ] Update invocation from `InvokeAsync` to `RunAsync`
- [ ] Handle streaming responses with new return types
- [ ] Test all providers (OpenAI, Anthropic, Google, Ollama)

**Deliverables:**
- ✅ Chat functionality working with AF
- ✅ All providers supported
- ✅ Tool calling functional

---

### Phase 3: Plugin → Tool Migration (8 hours)

**Goal:** Convert SK plugins to AF tools

**Tasks:**
- [ ] Remove `[KernelFunction]` attributes from all plugins
- [ ] Convert to simple methods or `AIFunction`
- [ ] Update descriptions using docstrings/attributes
- [ ] Register tools with agent creation
- [ ] Verify tool invocation works

**Deliverables:**
- ✅ All 4 plugins converted to tools
- ✅ Tool calling verified
- ✅ No SK plugin infrastructure remaining

---

### Phase 4: Chat Services Migration (6 hours)

**Goal:** Update chat services for new patterns

**Tasks:**
- [ ] Update `SqliteChatService.cs` for AgentThread
- [ ] Update `MongoChatService.cs` for AgentThread
- [ ] Migrate chat history persistence
- [ ] Update conversation tracking

**Deliverables:**
- ✅ Chat history persisted correctly
- ✅ Conversation context maintained
- ✅ Thread management working

---

### Phase 5: Vector Store & Embeddings (4 hours)

**Goal:** Update or maintain vector store integration

**Tasks:**
- [ ] Check if AF has Qdrant connector (may stay SK)
- [ ] Update embedding generation
- [ ] Test vector search functionality
- [ ] Update index management

**Deliverables:**
- ✅ Vector search working
- ✅ Embeddings generated correctly
- ✅ Qdrant integration functional

---

### Phase 6: Health Checkers & Cleanup (4 hours)

**Goal:** Update remaining components

**Tasks:**
- [ ] Update health checker client creation
- [ ] Remove any remaining SK references
- [ ] Clean up unused code
- [ ] Update `Summarizer.cs`

**Deliverables:**
- ✅ Health checks working
- ✅ No unused SK code
- ✅ Clean codebase

---

### Phase 7: Test Migration (8 hours)

**Goal:** Update all tests for new architecture

**Tasks:**
- [ ] Update `SpyChatComplete.cs` mock
- [ ] Rewrite kernel selection tests
- [ ] Update chat history tests
- [ ] Update plugin/tool tests
- [ ] Update health checker tests
- [ ] Verify all tests pass

**Deliverables:**
- ✅ All tests passing
- ✅ Test coverage maintained
- ✅ CI/CD green

---

### Phase 8: Documentation & Polish (4 hours)

**Goal:** Update documentation and finalize

**Tasks:**
- [ ] Update CLAUDE.md with new architecture
- [ ] Update API documentation
- [ ] Document breaking changes
- [ ] Create migration notes
- [ ] Performance testing

**Deliverables:**
- ✅ Documentation updated
- ✅ Migration complete
- ✅ Production ready

---

## Code Transformation Examples

### Example 1: Kernel Factory → ChatClient Provider

**Before (`KernelFactory.cs`):**
```csharp
public static Kernel CreateKernel(AiProvider provider, ChatCompleteSettings settings)
{
    var builder = Kernel.CreateBuilder();

    switch (provider)
    {
        case AiProvider.OpenAi:
            builder.AddOpenAIChatCompletion(
                modelId: settings.OpenAi.ModelId,
                apiKey: settings.OpenAi.ApiKey
            );
            break;
        case AiProvider.Ollama:
            builder.AddOllamaChatCompletion(
                modelId: settings.Ollama.ModelId,
                endpoint: new Uri(settings.OllamaBaseUrl)
            );
            break;
        // ... other providers
    }

    return builder.Build();
}
```

**After (`ChatClientProvider.cs`):**
```csharp
public static IChatClient CreateChatClient(AiProvider provider, ChatCompleteSettings settings)
{
    return provider switch
    {
        AiProvider.OpenAi => new OpenAIChatClient(
            settings.OpenAi.ModelId,
            new OpenAIClientOptions { ApiKey = settings.OpenAi.ApiKey }
        ),
        AiProvider.Ollama => new OllamaChatClient(
            new Uri(settings.OllamaBaseUrl),
            settings.Ollama.ModelId
        ),
        // ... other providers
        _ => throw new ArgumentException($"Unsupported provider: {provider}")
    };
}
```

---

### Example 2: Plugin → Tool

**Before (`CrossKnowledgeSearchPlugin.cs`):**
```csharp
public class CrossKnowledgeSearchPlugin
{
    private readonly IKnowledgeRepository _repository;

    [KernelFunction("SearchAllKnowledgeBases")]
    [Description("Search across all knowledge bases for relevant information")]
    public async Task<string> SearchAllKnowledgeBasesAsync(
        [Description("The search query")] string query,
        [Description("Maximum results per KB")] int maxResults = 5)
    {
        // Implementation
    }
}
```

**After:**
```csharp
public class CrossKnowledgeSearchTool
{
    private readonly IKnowledgeRepository _repository;

    /// <summary>
    /// Search across all knowledge bases for relevant information
    /// </summary>
    /// <param name="query">The search query</param>
    /// <param name="maxResults">Maximum results per KB</param>
    public async Task<string> SearchAllKnowledgeBasesAsync(
        string query,
        int maxResults = 5)
    {
        // Implementation unchanged
    }
}

// Registration:
var agent = chatClient.CreateAIAgent(
    instructions: "...",
    tools: [
        AIFunctionFactory.Create(searchTool.SearchAllKnowledgeBasesAsync)
    ]
);
```

---

### Example 3: Chat Invocation

**Before (`ChatComplete.cs`):**
```csharp
public async Task<string> AskAsync(string userMessage, ChatHistory history, ...)
{
    var kernel = KernelFactory.CreateKernel(provider, _settings);
    kernel.Plugins.Add(_crossKnowledgePlugin);

    var agent = new ChatCompletionAgent
    {
        Kernel = kernel,
        Instructions = _systemPrompt
    };

    var executionSettings = new OpenAIPromptExecutionSettings
    {
        Temperature = temperature,
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    var result = new StringBuilder();
    await foreach (var content in kernel.InvokePromptStreamingAsync(
        userMessage,
        new KernelArguments(executionSettings)))
    {
        result.Append(content);
    }

    return result.ToString();
}
```

**After:**
```csharp
public async Task<string> AskAsync(string userMessage, AgentThread thread, ...)
{
    var chatClient = ChatClientProvider.CreateChatClient(provider, _settings);

    var agent = chatClient.CreateAIAgent(
        instructions: _systemPrompt,
        tools: [
            AIFunctionFactory.Create(_searchTool.SearchAllKnowledgeBasesAsync),
            AIFunctionFactory.Create(_analyticsTool.GetSummaryAsync),
            // ... other tools
        ]
    );

    var response = await agent.RunAsync(
        userMessage,
        thread,
        new ChatClientAgentRunOptions(new ChatOptions
        {
            Temperature = temperature
        })
    );

    return response.Message.Text;
}
```

---

### Example 4: Chat History → AgentThread

**Before:**
```csharp
var history = new ChatHistory();
history.AddSystemMessage(systemPrompt);
history.AddUserMessage(userMessage);

// After response
history.AddAssistantMessage(response);

// Persistence
await _repository.SaveMessagesAsync(conversationId, history.ToMessages());
```

**After:**
```csharp
var thread = agent.GetNewThread();
// Or load existing: var thread = await agent.GetThreadAsync(threadId);

var response = await agent.RunAsync(userMessage, thread);

// Thread automatically maintains history
// Persistence may need custom implementation depending on thread type
```

---

## Risk Assessment

### High Risk Areas

1. **ChatComplete.cs Core Logic**
   - Central to entire application
   - Complex provider switching
   - Tool calling orchestration
   - **Mitigation:** Comprehensive testing, gradual rollout, feature flags

2. **Chat History/Thread Migration**
   - Persistence format may change
   - Existing conversations may be incompatible
   - **Mitigation:** Data migration script, backward compatibility layer

3. **Provider Support**
   - Not all SK providers may have AF equivalents yet
   - Anthropic and Google AI may need adapters
   - **Mitigation:** Check AF provider support, create adapters if needed

### Medium Risk Areas

1. **DI Registration Changes**
   - Many services need re-registration
   - **Mitigation:** Careful testing of DI container

2. **Qdrant Integration**
   - May need to keep SK Qdrant connector
   - **Mitigation:** Check AF vector store support

3. **Test Mocking**
   - Different interfaces to mock
   - **Mitigation:** Update all mocks systematically

### Low Risk Areas

1. **Plugin → Tool Conversion**
   - Mostly removing attributes
   - Business logic unchanged
   - **Mitigation:** Straightforward transformation

2. **Health Checkers**
   - Minor client creation changes
   - **Mitigation:** Simple updates

---

## Pre-Migration Checklist

### Before Starting Migration

- [ ] **Complete exploration phase** with sample projects
- [ ] **Document Agent Framework patterns** discovered
- [ ] **Verify provider support** for all needed LLMs
- [ ] **Check Qdrant support** in Agent Framework
- [ ] **Create feature branch** for migration
- [ ] **Set up rollback plan** (keep SK code as backup)
- [ ] **Notify team** of upcoming changes
- [ ] **Update CI/CD** to handle migration

### Technical Prerequisites

- [ ] Agent Framework NuGet packages available
- [ ] .NET 8 compatibility confirmed
- [ ] All current tests passing
- [ ] Database backup taken (chat history)
- [ ] Configuration migration plan ready

---

## Success Criteria

### Functional Criteria

- [ ] All existing chat functionality works
- [ ] All providers supported (OpenAI, Anthropic, Google, Ollama)
- [ ] All tools/plugins functional
- [ ] Chat history persisted correctly
- [ ] Vector search working
- [ ] MCP integration unaffected

### Quality Criteria

- [ ] All tests passing (>80% coverage)
- [ ] No SK namespaces remaining (except Qdrant if needed)
- [ ] Performance equal or better than SK
- [ ] Memory usage equal or better
- [ ] No regressions in functionality

### Documentation Criteria

- [ ] CLAUDE.md updated
- [ ] Code comments updated
- [ ] Migration notes documented
- [ ] Breaking changes documented

---

## Timeline Estimate

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Phase 0: Exploration | 1-2 weeks | None |
| Phase 1: Foundation | 2 days | Phase 0 |
| Phase 2: Core Chat | 3 days | Phase 1 |
| Phase 3: Tools | 2 days | Phase 2 |
| Phase 4: Chat Services | 1-2 days | Phase 2 |
| Phase 5: Vector Store | 1 day | Phase 1 |
| Phase 6: Cleanup | 1 day | Phases 3-5 |
| Phase 7: Tests | 2 days | All above |
| Phase 8: Documentation | 1 day | Phase 7 |

**Total:** ~2-3 weeks of development time (after exploration)

---

## Next Steps

1. **Complete exploration phase** - Build 2-3 sample projects with Agent Framework
2. **Document findings** - Note any issues, patterns, or gotchas
3. **Verify provider support** - Ensure Anthropic, Google, Ollama work in AF
4. **Check Qdrant status** - Determine if SK connector can stay
5. **Create migration branch** - Start implementation when ready
6. **Implement Phase 1** - Get foundation working first
7. **Iterate through phases** - One phase at a time with testing

---

## Resources

### Official Documentation
- [Agent Framework Overview](https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview)
- [Migration Guide from Semantic Kernel](https://learn.microsoft.com/en-us/agent-framework/migration-guide/from-semantic-kernel/)
- [Migration Samples](https://learn.microsoft.com/en-us/agent-framework/migration-guide/from-semantic-kernel/samples)

### GitHub
- [Agent Framework Repository](https://github.com/microsoft/agent-framework)
- [SK Discussion on AF](https://github.com/microsoft/semantic-kernel/discussions/13215)
- [Migration Issues](https://github.com/microsoft/agent-framework/issues/338)

### Blog Posts
- [Semantic Kernel and Microsoft Agent Framework](https://devblogs.microsoft.com/semantic-kernel/semantic-kernel-and-microsoft-agent-framework/)
- [Visual Studio Magazine: SK + AutoGen = Agent Framework](https://visualstudiomagazine.com/articles/2025/10/01/semantic-kernel-autogen--open-source-microsoft-agent-framework.aspx)

---

## Components That Stay As-Is

### Microsoft.SemanticKernel.Connectors.Qdrant

**Status:** ✅ No migration required

Despite being labeled as a "Semantic Kernel" package, `Microsoft.SemanticKernel.Connectors.Qdrant` is **functionally independent** of the SK core libraries:

**Why It Can Stay:**
1. **No Kernel dependency** - Uses standard .NET patterns, not SK abstractions
2. **No AF equivalent** - Agent Framework has no vector store connectors
3. **Standalone functionality** - Just wraps Qdrant client library
4. **Stable API** - Vector store operations are well-defined

**Affected Files (No Changes Needed):**
- `KnowledgeEngine/Persistence/IndexManagers/QdrantIndexManager.cs`
- `KnowledgeEngine/Persistence/VectorStores/QdrantVectorStoreStrategy.cs`
- `Knowledge.Mcp/TestQdrantCollections.cs`
- `Knowledge.Mcp.Tests/QdrantConnectionTests.cs`

**NuGet Package:**
```xml
<!-- Keep this package - it's SK in name only -->
<PackageReference Include="Microsoft.SemanticKernel.Connectors.Qdrant" Version="x.x.x" />
```

**Validation:**
During exploration phase, verify that removing other SK packages doesn't break Qdrant integration. If it works independently, no action needed.

---

## Text Chunking Considerations

### Current Implementation

The application uses Semantic Kernel's `TextChunker` for document splitting in [KnowledgeManager.cs](../KnowledgeEngine/KnowledgeManager.cs#L103-L108):

```csharp
var lines = markdown
    ? TextChunker.SplitMarkDownLines(rawText, maxLine)
    : TextChunker.SplitPlainTextLines(rawText, maxLine);
var paragraphs = markdown
    ? TextChunker.SplitMarkdownParagraphs(lines, maxPara, overlap)
    : TextChunker.SplitPlainTextParagraphs(lines, maxPara, overlap);
```

**TextChunker Features Used:**
- `SplitMarkDownLines()` - Markdown-aware line splitting
- `SplitPlainTextLines()` - Plain text line splitting
- `SplitMarkdownParagraphs()` - Chunk markdown with overlap
- `SplitPlainTextParagraphs()` - Chunk plain text with overlap

### Agent Framework Status

**⚠️ No equivalent in Agent Framework**

The Microsoft Agent Framework does not include a text chunking/splitting utility. This is a gap that needs to be addressed.

### Options Analysis

#### Option 1: Keep SK TextChunker (Recommended)

**Approach:** Continue using `Microsoft.SemanticKernel.Text` package for chunking only.

**Pros:**
- ✅ Zero code changes
- ✅ Well-tested, production-ready
- ✅ Markdown-aware chunking
- ✅ Configurable overlap
- ✅ Already working

**Cons:**
- ❌ Maintains SK dependency (but isolated to text chunking only)
- ❌ Mixed package ecosystem

**Implementation:**
```xml
<!-- Keep only for TextChunker -->
<PackageReference Include="Microsoft.SemanticKernel.Core" Version="x.x.x" />
<!-- Or if TextChunker is in a separate package -->
```

**Recommendation:** Start with this option during migration. It's low risk and allows focusing on core migration.

---

#### Option 2: Azure AI Search Text Splitting

**Approach:** Use Azure AI Search's text splitting capabilities.

**Pros:**
- ✅ Part of Microsoft ecosystem
- ✅ Enterprise-grade
- ✅ Multiple splitting strategies

**Cons:**
- ❌ Requires Azure dependency
- ❌ May need Azure subscription
- ❌ Overhead for local deployments
- ❌ More complex than simple chunking

**Implementation:**
```csharp
// Would require Azure.Search.Documents package
// Implementation details TBD
```

**Recommendation:** Only if already using Azure AI Search for other features.

---

#### Option 3: Third-Party Library

**Approach:** Use a dedicated text splitting library.

**Potential Libraries:**
1. **LangChain.NET** - Has text splitters similar to Python LangChain
2. **Unstructured.io** - Document processing with splitting
3. **Custom implementation** - Build your own based on SK source

**Pros:**
- ✅ Purpose-built for RAG/chunking
- ✅ Active development
- ✅ Various splitting strategies

**Cons:**
- ❌ New dependency to evaluate
- ❌ Different API to learn
- ❌ Migration effort

**Implementation Example (LangChain.NET):**
```csharp
// Hypothetical - needs validation
using LangChain.Splitters;

var splitter = new RecursiveCharacterTextSplitter(
    chunkSize: maxPara,
    chunkOverlap: overlap
);
var chunks = splitter.SplitText(rawText);
```

**Recommendation:** Evaluate during exploration phase if removing all SK packages is a goal.

---

#### Option 4: Custom Implementation

**Approach:** Implement text chunking from scratch based on SK source code.

**Pros:**
- ✅ No external dependencies
- ✅ Full control over behavior
- ✅ Can optimize for specific needs

**Cons:**
- ❌ Development effort (4-8 hours)
- ❌ Maintenance burden
- ❌ Need to handle edge cases
- ❌ Need to implement markdown parsing

**Implementation Sketch:**
```csharp
public static class TextChunkerCustom
{
    public static List<string> SplitPlainTextParagraphs(
        IEnumerable<string> lines, int maxTokensPerParagraph, int overlapTokens)
    {
        var paragraphs = new List<string>();
        var currentParagraph = new StringBuilder();
        var currentTokenCount = 0;

        foreach (var line in lines)
        {
            var lineTokens = EstimateTokens(line);

            if (currentTokenCount + lineTokens > maxTokensPerParagraph && currentParagraph.Length > 0)
            {
                paragraphs.Add(currentParagraph.ToString());
                // Handle overlap...
                currentParagraph.Clear();
                currentTokenCount = 0;
            }

            currentParagraph.AppendLine(line);
            currentTokenCount += lineTokens;
        }

        if (currentParagraph.Length > 0)
            paragraphs.Add(currentParagraph.ToString());

        return paragraphs;
    }

    private static int EstimateTokens(string text) => text.Length / 4; // Rough estimate
}
```

**Recommendation:** Only if absolutely need to eliminate all SK packages and no third-party alternatives work.

---

### Recommended Approach

**Phase 0 (Exploration):**
1. Keep SK TextChunker initially
2. Test if `Microsoft.SemanticKernel.Core` works standalone (no other SK packages)
3. Evaluate LangChain.NET or other alternatives

**Phase 5 (Vector Store & Embeddings):**
1. If SK TextChunker works standalone → keep it ✅
2. If SK TextChunker requires other SK packages → migrate to alternative
3. Document decision and rationale

**Decision Matrix:**

| Scenario | Recommended Option |
|----------|-------------------|
| SK TextChunker works standalone | Keep SK (Option 1) |
| Must eliminate all SK packages | LangChain.NET or custom (Option 3/4) |
| Already using Azure AI Search | Azure AI (Option 2) |
| Need advanced document processing | Unstructured.io |

### Testing Requirements

Regardless of chosen option, verify:
- [ ] Plain text splitting produces same chunk count
- [ ] Markdown splitting preserves code blocks
- [ ] Overlap tokens work correctly
- [ ] Token estimation is consistent
- [ ] Large documents (>100KB) handled efficiently
- [ ] Unicode characters preserved

---

**Last Updated:** 2025-11-18
**Author:** Claude (AI Assistant)
**Review Status:** Initial Planning - Pending exploration phase completion
