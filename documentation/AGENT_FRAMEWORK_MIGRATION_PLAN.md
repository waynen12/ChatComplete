# Semantic Kernel to Microsoft Agent Framework Migration Plan

**Project:** AI Knowledge Manager
**Status:** 🟡 IN PROGRESS - 15% Complete
**Last Updated:** 2025-12-17
**Estimated Remaining Effort:** 54-81 hours (7-10 working days)

---

## 📊 Migration Progress

```
█████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ 15% Complete

✅ Completed:
- ChatCompleteAF.cs (590 lines, 54% code reduction vs SK)
- 4 AF plugins migrated (CrossKnowledgeSearch, ModelRecommendation, KnowledgeAnalytics, SystemHealth)
- AgentFactory.cs for agent creation
- API integration with feature flag routing
- Unit tests for ChatCompleteAF (6/7 passing)
- Health checker fixes (Anthropic, OpenAI)

🔄 In Progress:
- Integration testing with real providers
- Feature parity verification

⏳ Remaining:
- 35+ SK files to migrate or remove
- Streaming support in ChatCompleteAF
- TextChunker replacement
- Test updates
- SK deprecation and cleanup
```

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current State Analysis](#current-state-analysis)
3. [Comprehensive SK Files Inventory](#comprehensive-sk-files-inventory)
4. [Migration Strategy](#migration-strategy)
5. [Code Transformation Examples](#code-transformation-examples)
6. [Critical Blockers](#critical-blockers)
7. [Risk Assessment](#risk-assessment)
8. [Success Criteria](#success-criteria)

---

## Executive Summary

### What is Microsoft Agent Framework?

Microsoft Agent Framework is the **unified successor to Semantic Kernel and AutoGen**, combining their strengths:
- **From Semantic Kernel:** Type-safe skills, state/threads, filters, telemetry, enterprise model support
- **From AutoGen:** Simple multi-agent patterns, explicit workflow control

Think of it as **Semantic Kernel v2.0** - built by the same team, designed to be the future of AI agent development in .NET and Python.

### Why Migrate?

**Current Reality:**
- Semantic Kernel will continue to receive critical bug fixes and security patches
- New features are being built for Agent Framework
- SK support guaranteed for at least 1 year after AF reaches GA

**Future Direction:**
- Agent Framework is where all innovation is happening
- Better multi-agent orchestration patterns
- Simplified API (no more Kernel abstraction layer)
- Direct ChatClient usage

### Migration Summary

| Metric | Before (SK) | After (AF) | Status |
|--------|-------------|------------|--------|
| Total Files | 40+ | TBD | 🔄 |
| Core Chat | ChatComplete.cs (1,290 lines) | ChatCompleteAF.cs (590 lines) | ✅ |
| Agent Plugins | 4 (SK) | 4 (AF) | ✅ |
| Factories | KernelFactory + KernelHelper | AgentFactory | ✅ |
| Chat Services | 2 (dual routing) | 2 (to simplify) | 🔄 |
| Tests | 15+ (SK) | 7 (AF) + 15+ to update | 🔄 |
| Code Reduction | - | 54% in core chat | ✅ |

---

## Current State Analysis

### What We've Accomplished ✅

#### 1. **ChatCompleteAF.cs** - Core Chat Migration
**File:** [KnowledgeEngine/ChatCompleteAF.cs](../KnowledgeEngine/ChatCompleteAF.cs)
**Status:** ✅ Complete
**Lines:** 590 (vs 1,290 in SK version = 54% reduction)

**Features Implemented:**
- ✅ AskAsync() - Simple RAG chat without tools
- ✅ AskWithAgentAsync() - Chat with tool calling support
- ✅ All 4 providers: OpenAI, Anthropic, Google, Ollama
- ✅ Vector search integration
- ✅ Prompt template reuse
- ✅ Usage tracking and error handling
- ✅ Graceful fallbacks
- ⏳ Streaming support (TODO)

**Key Differences from SK:**
```csharp
// SK: Complex kernel setup
var kernel = Kernel.CreateBuilder().AddOpenAI(...).Build();
var agent = new ChatCompletionAgent { Kernel = kernel, ... };
var result = await kernel.InvokeAsync(...);

// AF: Direct agent creation
var agent = _agentFactory.CreateAgent(provider, systemMessage);
var result = await agent.RunAsync(prompt, cancellationToken: ct);
var response = result?.ToString(); // Simple string extraction
```

#### 2. **Agent Framework Plugins** - 4 Plugins Migrated
**Directory:** [KnowledgeEngine/Agents/AgentFramework/](../KnowledgeEngine/Agents/AgentFramework/)
**Status:** ✅ Complete

| Plugin | SK Lines | AF Lines | Status |
|--------|----------|----------|--------|
| CrossKnowledgeSearchPlugin | ~200 | ~180 | ✅ |
| ModelRecommendationPlugin | ~150 | ~140 | ✅ |
| KnowledgeAnalyticsPlugin | ~200 | ~190 | ✅ |
| SystemHealthPlugin | ~150 | ~140 | ✅ |

**Key Changes:**
- Removed `[KernelFunction]` attributes
- Using `[Description]` attributes for tool metadata
- Registration via `AIFunctionFactory.Create()` pattern
- Registered in DI in Program.cs

#### 3. **AgentFactory.cs** - Agent Creation
**File:** [KnowledgeEngine/Agents/AgentFramework/AgentFactory.cs](../KnowledgeEngine/Agents/AgentFramework/AgentFactory.cs)
**Status:** ✅ Complete

**Methods:**
- `CreateAgent()` - Simple agent without tools
- `CreateAgentWithPlugins()` - Agent with tool calling
- Supports all 4 providers (OpenAI, Anthropic, Google, Ollama)
- Handles API key validation and error cases

#### 4. **API Integration with Feature Flag**
**Files:**
- [Knowledge.Api/Program.cs](../Knowledge.Api/Program.cs) - DI registration
- [KnowledgeEngine/Chat/SqliteChatService.cs](../KnowledgeEngine/Chat/SqliteChatService.cs) - Routing
- [KnowledgeEngine/Chat/MongoChatService.cs](../KnowledgeEngine/Chat/MongoChatService.cs) - Routing

**Status:** ✅ Complete

**Feature Flag:** `UseAgentFramework` boolean in appsettings.json

**Routing Logic:**
```csharp
if (_settings.UseAgentFramework) {
    // Route to ChatCompleteAF (Agent Framework)
    var afMessages = ConvertToAFChatHistory(historyForLLM);
    replyText = await _chatAF.AskAsync(...);
} else {
    // Route to ChatComplete (Semantic Kernel)
    replyText = await _chatSK.AskAsync(...);
}
```

**Benefits:**
- A/B testing capability
- Safe gradual rollout
- Easy rollback if issues occur
- Both SK and AF registered in DI simultaneously

#### 5. **Unit Tests**
**File:** [KnowledgeManager.Tests/AgentFramework/ChatCompleteAFTests.cs](../KnowledgeManager.Tests/AgentFramework/ChatCompleteAFTests.cs)
**Status:** ✅ 6/7 tests passing (1 skipped)

**Test Coverage:**
- Constructor initialization
- AskAsync with valid input
- AskAsync with knowledge base
- Exception handling
- AskWithAgentAsync with tools enabled
- AskWithAgentAsync with tools disabled
- Console logging (skipped - interferes with other tests)

**Test Pattern:** Using FakeChatCompleteAF test double (similar to SpyChatComplete pattern)

---

## Comprehensive SK Files Inventory

### PRIORITY 1: CRITICAL - Delete/Deprecate (6 files) ⚠️

#### Files to DELETE Immediately ✂️

| File | Reason | Effort | Risk |
|------|--------|--------|------|
| [KnowledgeEngine/KernelHelper.cs](../KnowledgeEngine/KernelHelper.cs) | Marked [Obsolete], replaced by KernelFactory | 1h | LOW |
| [KnowledgeEngine/Agents/Plugins/CrossKnowledgeSearchPlugin.cs](../KnowledgeEngine/Agents/Plugins/CrossKnowledgeSearchPlugin.cs) | AF version exists | 15min | LOW |
| [KnowledgeEngine/Agents/Plugins/KnowledgeAnalyticsAgent.cs](../KnowledgeEngine/Agents/Plugins/KnowledgeAnalyticsAgent.cs) | AF version exists | 15min | LOW |
| [KnowledgeEngine/Agents/Plugins/ModelRecommendationAgent.cs](../KnowledgeEngine/Agents/Plugins/ModelRecommendationAgent.cs) | AF version exists | 15min | LOW |
| [KnowledgeEngine/Agents/Plugins/SystemHealthAgent.cs](../KnowledgeEngine/Agents/Plugins/SystemHealthAgent.cs) | AF version exists | 15min | LOW |
| [KnowledgeEngine/EmbeddingsHelper.cs](../KnowledgeEngine/EmbeddingsHelper.cs) | Legacy, not used in main flow | 1h | LOW |
| [Summarizer.cs](../Summarizer.cs) | Example/demo code (has class LibrAIan) | 1h | LOW |

**Total Deletion Effort:** 2-3 hours

#### Files to DEPRECATE (After Feature Parity)

| File | AF Equivalent | Blocking Issue | Effort | Risk |
|------|---------------|----------------|--------|------|
| [KnowledgeEngine/ChatComplete.cs](../KnowledgeEngine/ChatComplete.cs) (1,290 lines) | ChatCompleteAF.cs | Needs streaming support | 0h (already done) | MEDIUM |
| [KnowledgeEngine/KernelFactory.cs](../KnowledgeEngine/KernelFactory.cs) | AgentFactory.cs | None | 1-2h | LOW |

**Total Deprecation Effort:** 1-2 hours (verification + removal)

---

### PRIORITY 2: HIGH - Chat Services (2 files)

| File | Current State | Action Needed | Effort | Risk |
|------|---------------|---------------|--------|------|
| [KnowledgeEngine/Chat/SqliteChatService.cs](../KnowledgeEngine/Chat/SqliteChatService.cs) | Dual routing (SK + AF) | Simplify to AF-only after deprecating ChatComplete | 3-4h | MEDIUM |
| [KnowledgeEngine/Chat/MongoChatService.cs](../KnowledgeEngine/Chat/MongoChatService.cs) | Dual routing (SK + AF) | Simplify to AF-only after deprecating ChatComplete | 3-4h | MEDIUM |

**Current Dual Routing Pattern:**
```csharp
public SqliteChatService(
    ChatComplete chatSK,           // SK version
    ChatCompleteAF chatAF,         // AF version
    IConversationRepository convos,
    IOptions<ChatCompleteSettings> cfg)
{
    _chatSK = chatSK;
    _chatAF = chatAF;
    // ... feature flag routing in GetReplyAsync()
}
```

**After Simplification:**
```csharp
public SqliteChatService(
    ChatCompleteAF chat,           // AF only
    IConversationRepository convos,
    IOptions<ChatCompleteSettings> cfg)
{
    _chat = chat;
    // ... direct AF calls, no routing
}
```

**Effort:** 6-8 hours total

---

### PRIORITY 3: MEDIUM - Health Checkers (3 files)

**Problem:** Currently use SK connectors to test provider connectivity

| File | SK Dependencies | Migration Path | Effort | Risk |
|------|-----------------|----------------|--------|------|
| [KnowledgeEngine/Services/HealthCheckers/OpenAIHealthChecker.cs](../KnowledgeEngine/Services/HealthCheckers/OpenAIHealthChecker.cs) | SK OpenAI connector | Use OpenAI SDK directly | 3-4h | MEDIUM |
| [KnowledgeEngine/Services/HealthCheckers/AnthropicHealthChecker.cs](../KnowledgeEngine/Services/HealthCheckers/AnthropicHealthChecker.cs) | SK Anthropic connector | Use Anthropic SDK directly | 3-4h | MEDIUM |
| [KnowledgeEngine/Services/HealthCheckers/GoogleAIHealthChecker.cs](../KnowledgeEngine/Services/HealthCheckers/GoogleAIHealthChecker.cs) | SK Google connector | Use Google Generative AI SDK directly | 3-4h | MEDIUM |

**Migration Pattern:**
```csharp
// Before (SK):
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(model, apiKey)
    .Build();
var service = kernel.GetRequiredService<IChatCompletionService>();
await service.GetChatMessageContentAsync(...);

// After (Direct SDK):
var client = new OpenAIClient(apiKey);
var chatClient = client.GetChatClient(model);
var response = await chatClient.CompleteChatAsync("test");
```

**Effort:** 9-12 hours total

---

### PRIORITY 4: CRITICAL BLOCKERS - Text Processing (2 components) 🔴

#### 1. **TextChunker in KnowledgeManager.cs**

**File:** [KnowledgeEngine/KnowledgeManager.cs](../KnowledgeEngine/KnowledgeManager.cs)
**Lines:** 103-108
**Impact:** HIGH - Affects RAG quality

**Current Usage:**
```csharp
var lines = markdown
    ? TextChunker.SplitMarkDownLines(rawText, maxLine)
    : TextChunker.SplitPlainTextLines(rawText, maxLine);
var paragraphs = markdown
    ? TextChunker.SplitMarkdownParagraphs(lines, maxPara, overlap)
    : TextChunker.SplitPlainTextParagraphs(lines, maxPara, overlap);
```

**Problem:** `TextChunker` is from `Microsoft.SemanticKernel.Text` - no AF equivalent

**Options:**

| Option | Pros | Cons | Effort | Recommendation |
|--------|------|------|--------|----------------|
| **Keep SK TextChunker** | Zero code changes, tested | Maintains SK dependency | 0h | ✅ Start here |
| **Custom Implementation** | No dependencies, full control | Dev effort, maintenance | 4-8h | If must remove SK |
| **Third-party (LangChain.NET)** | Purpose-built for RAG | New dependency to learn | 3-6h | Evaluate in exploration |

**Decision:** Start with Option 1 (keep SK TextChunker), migrate later if needed

**Effort:** 0-8 hours (depending on option)
**Risk:** HIGH (affects RAG quality)

#### 2. **QdrantVectorStoreStrategy.cs**

**File:** [KnowledgeEngine/Persistence/VectorStores/QdrantVectorStoreStrategy.cs](../KnowledgeEngine/Persistence/VectorStores/QdrantVectorStoreStrategy.cs)
**Lines:** 200+
**Impact:** MEDIUM - Core vector operations

**Current Dependencies:**
```csharp
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.Extensions.AI; // Already using modern APIs
```

**Migration Path:**
- Replace SK's `QdrantVectorStore` with direct `Qdrant.Client` SDK
- OR keep SK connector if it works standalone (name is SK but may be independent)

**Effort:** 6-10 hours
**Risk:** MEDIUM

---

### PRIORITY 5: LOW - Configuration & Utilities (4 files)

| File | Changes Needed | Effort | Risk |
|------|----------------|--------|------|
| [KnowledgeEngine/Extensions/ServiceCollectionExtensions.cs](../KnowledgeEngine/Extensions/ServiceCollectionExtensions.cs) | Remove SK connector imports | 2-3h | LOW |
| [Knowledge.Api/Services/SemanticKernelEmbeddingService.cs](../Knowledge.Api/Services/SemanticKernelEmbeddingService.cs) | Rename (misleading - doesn't use SK) | 1h | LOW |
| [KnowledgeEngine/AutoInvoke.cs](../KnowledgeEngine/AutoInvoke.cs) | Update auto-invoke logic | 2-4h | MEDIUM |

**Effort:** 5-8 hours total

---

### PRIORITY 6: LOW - Test Files (15+ files)

**Update After Main Code Migration**

| File | Changes Needed | Effort |
|------|----------------|--------|
| SqliteChatServiceTests.cs | Update for AF-only routing | 2h |
| MongoChatServiceTests.cs | Update for AF-only routing | 2h |
| SpyChatComplete.cs | Update test double | 2h |
| ChatHistoryReducerTests.cs | Update for AgentThread | 1h |
| KernerlSelectionTests.cs (typo in name) | Rewrite for AF | 2h |
| AnthropicHealthCheckerTests.cs | Update client mocking | 1h |
| GoogleAIHealthCheckerTests.cs | Update client mocking | 1h |
| OpenAIHealthCheckerTests.cs | Update client mocking | 1h |
| KnowledgeAnalyticsAgentTests.cs | Update for AF plugin | 1h |
| ModelRecommendationAgentTests.cs | Update for AF plugin | 1h |
| Other test files | Various updates | 2-3h |

**Total Test Effort:** 15-18 hours

---

## Migration Strategy

### Recommended Migration Order

#### **Phase 1: Quick Wins (2-3 hours)** ✂️

**Goal:** Remove obsolete SK files immediately

**Tasks:**
- [ ] Delete KernelHelper.cs
- [ ] Delete 4 legacy SK plugins (AF versions exist)
- [ ] Delete EmbeddingsHelper.cs
- [ ] Delete Summarizer.cs
- [ ] Commit deletions

**Deliverables:**
- ✅ 7 fewer SK files
- ✅ Cleaner codebase
- ✅ No functional changes

---

#### **Phase 2: Streaming Support (4-6 hours)**

**Goal:** Add streaming to ChatCompleteAF for feature parity

**Tasks:**
- [ ] Add AskStreamingAsync() method to ChatCompleteAF
- [ ] Add AskWithAgentStreamingAsync() method
- [ ] Test streaming with all providers
- [ ] Update API endpoints to support streaming
- [ ] Verify streaming works with tools

**Deliverables:**
- ✅ ChatCompleteAF has full feature parity with ChatComplete
- ✅ Ready to deprecate SK ChatComplete

**Blocking:** Phase 1 complete

---

#### **Phase 3: Core Deprecation (8-10 hours)**

**Goal:** Remove SK core classes (ChatComplete, KernelFactory)

**Tasks:**
- [ ] Verify ChatCompleteAF streaming works
- [ ] Remove ChatComplete.cs (1,290 lines)
- [ ] Remove KernelFactory.cs
- [ ] Update SqliteChatService to AF-only (remove dual routing)
- [ ] Update MongoChatService to AF-only (remove dual routing)
- [ ] Update Program.cs DI registration (remove SK versions)
- [ ] Test all chat functionality
- [ ] Update feature flag documentation

**Deliverables:**
- ✅ No more Kernel abstraction
- ✅ Simplified chat services
- ✅ ~1,400 lines of SK code removed

**Blocking:** Phase 2 complete (streaming support)

---

#### **Phase 4: Infrastructure Updates (12-20 hours)**

**Goal:** Refactor supporting infrastructure

**Tasks:**
- [ ] **Health Checkers (9-12h)**
  - [ ] Migrate OpenAIHealthChecker to OpenAI SDK directly
  - [ ] Migrate AnthropicHealthChecker to Anthropic SDK directly
  - [ ] Migrate GoogleAIHealthChecker to Google SDK directly
  - [ ] Test health checks with real API calls

- [ ] **Text Chunking (0-8h)**
  - [ ] Test if SK TextChunker works standalone
  - [ ] If yes: keep it (0h)
  - [ ] If no: implement custom or use third-party (4-8h)

- [ ] **Vector Store (6-10h)**
  - [ ] Evaluate QdrantVectorStoreStrategy independence
  - [ ] If dependent: migrate to Qdrant.Client SDK directly
  - [ ] Test vector search functionality

- [ ] **Configuration (3-5h)**
  - [ ] Update ServiceCollectionExtensions (remove SK imports)
  - [ ] Rename SemanticKernelEmbeddingService
  - [ ] Update AutoInvoke.cs if needed

**Deliverables:**
- ✅ Health checkers use direct SDKs
- ✅ Text chunking resolved
- ✅ Vector store migrated or verified standalone
- ✅ Configuration cleaned up

**Blocking:** Phase 3 complete

---

#### **Phase 5: Test Updates (15-18 hours)**

**Goal:** Update all test files for AF patterns

**Tasks:**
- [ ] Update SqliteChatServiceTests
- [ ] Update MongoChatServiceTests
- [ ] Update or remove SpyChatComplete
- [ ] Update ChatHistoryReducerTests
- [ ] Update/rename KernerlSelectionTests
- [ ] Update health checker tests (3 files)
- [ ] Update plugin/agent tests (2 files)
- [ ] Update other test files
- [ ] Verify all tests pass (>80% coverage)

**Deliverables:**
- ✅ All tests passing
- ✅ AF test patterns established
- ✅ CI/CD green

**Blocking:** Phase 4 complete

---

#### **Phase 6: Final Cleanup & Documentation (4-6 hours)**

**Goal:** Remove all remaining SK references, update docs

**Tasks:**
- [ ] Remove all `#pragma warning disable SKEXP*` pragmas
- [ ] Remove `[Experimental("SKEXP0070")]` attributes
- [ ] Remove SK NuGet packages (except standalone ones like TextChunker if kept)
- [ ] Verify no SK namespaces remain (grep check)
- [ ] Update CLAUDE.md
- [ ] Update README.md
- [ ] Update AGENT_FRAMEWORK_MIGRATION_PLAN.md (mark complete)
- [ ] Document breaking changes
- [ ] Performance testing (AF vs SK baseline)
- [ ] Memory usage comparison

**Deliverables:**
- ✅ Documentation updated
- ✅ Migration complete
- ✅ Production ready
- ✅ Performance validated

**Blocking:** Phase 5 complete

---

### Timeline Estimate

| Phase | Duration | Prerequisites | Status |
|-------|----------|---------------|--------|
| Phase 0: Exploration | 1-2 weeks | None | ✅ DONE |
| Phase 1: Quick Wins | 2-3 hours | None | ⏳ READY |
| Phase 2: Streaming | 4-6 hours | Phase 1 | ⏳ BLOCKED |
| Phase 3: Core Deprecation | 8-10 hours | Phase 2 | ⏳ BLOCKED |
| Phase 4: Infrastructure | 12-20 hours | Phase 3 | ⏳ BLOCKED |
| Phase 5: Tests | 15-18 hours | Phase 4 | ⏳ BLOCKED |
| Phase 6: Cleanup & Docs | 4-6 hours | Phase 5 | ⏳ BLOCKED |

**Total Remaining:** 45-63 hours (~6-8 working days)

**Current Progress:** 15% (ChatCompleteAF + 4 plugins + tests + API integration done)

---

## Code Transformation Examples

### Example 1: Simple Chat (Before/After)

**Before (SK - ChatComplete.cs):**
```csharp
public async Task<string> AskAsync(
    string userMessage,
    string? knowledgeId,
    ChatHistory chatHistory,
    double apiTemperature,
    AiProvider provider,
    bool useExtendedInstructions = false,
    string? ollamaModel = null,
    CancellationToken ct = default)
{
    // 1. Create kernel
    var kernel = KernelFactory.CreateKernel(provider, _settings);

    // 2. Configure execution settings
    var executionSettings = new OpenAIPromptExecutionSettings
    {
        Temperature = apiTemperature,
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    // 3. Get system prompt
    var systemPrompt = await GetSystemPromptAsync(useExtendedInstructions, false);
    chatHistory.AddSystemMessage(systemPrompt);

    // 4. Vector search
    if (knowledgeId != null)
    {
        var results = await _knowledgeManager.SearchAsync(knowledgeId, userMessage, 10, 0.3, ct);
        var context = BuildContext(results);
        userMessage = $"{context}\n\n{userMessage}";
    }

    // 5. Invoke
    chatHistory.AddUserMessage(userMessage);
    var result = await kernel.InvokePromptAsync(chatHistory, executionSettings, ct);

    return result.ToString();
}
```

**After (AF - ChatCompleteAF.cs):**
```csharp
public virtual async Task<string> AskAsync(
    string userMessage,
    string? knowledgeId,
    List<ChatMessage> chatHistory,
    double apiTemperature,
    AiProvider provider,
    bool useExtendedInstructions = false,
    string? ollamaModel = null,
    CancellationToken ct = default)
{
    // 1. Get system prompt
    var systemMessage = await GetSystemPromptAsync(useExtendedInstructions, false);

    // 2. Create agent (no Kernel needed!)
    var agent = _agentFactory.CreateAgent(provider, systemMessage, ollamaModel);

    // 3. Vector search
    if (knowledgeId != null)
    {
        var results = await _knowledgeManager.SearchAsync(knowledgeId, userMessage, 10, 0.3, ct);
        var context = BuildContext(results);
        userMessage = $"{context}\n\n{userMessage}";
    }

    // 4. Convert history
    var messages = ConvertToAFMessages(chatHistory, userMessage);

    // 5. Invoke (simpler API!)
    var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
    var promptText = lastUserMessage?.Text ?? userMessage;
    var agentResponse = await agent.RunAsync(promptText, cancellationToken: ct);

    return agentResponse?.ToString() ?? "There was no response from the AI.";
}
```

**Key Improvements:**
- ❌ No Kernel creation
- ❌ No complex execution settings
- ✅ Direct agent creation
- ✅ Simpler API (`RunAsync` instead of `InvokePromptAsync`)
- ✅ 30% less code

---

### Example 2: Agent with Tool Calling (Before/After)

**Before (SK):**
```csharp
public async Task<string> AskWithAgentAsync(...)
{
    // 1. Create kernel
    var kernel = KernelFactory.CreateKernel(provider, _settings);

    // 2. Register plugins
    kernel.Plugins.Add(_crossKnowledgePlugin);
    kernel.Plugins.Add(_analyticsPlugin);
    kernel.Plugins.Add(_modelRecommendationPlugin);
    kernel.Plugins.Add(_healthPlugin);

    // 3. Configure tool calling
    var executionSettings = new OpenAIPromptExecutionSettings
    {
        Temperature = apiTemperature,
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    // 4. Create agent
    var agent = new ChatCompletionAgent
    {
        Kernel = kernel,
        Instructions = systemPrompt,
        ExecutionSettings = executionSettings
    };

    // 5. Invoke
    var result = await agent.InvokeAsync(chatHistory, ct);

    return result.Message.Content;
}
```

**After (AF):**
```csharp
public virtual async Task<AgentChatResponse> AskWithAgentAsync(...)
{
    // 1. Get system prompt
    var systemMessage = await GetSystemPromptAsync(useExtendedInstructions, true);

    // 2. Register plugins (simple dictionary!)
    var plugins = RegisterAgentPlugins(); // Gets from DI

    // 3. Create agent with tools (one call!)
    var agent = _agentFactory.CreateAgentWithPlugins(
        provider,
        systemMessage,
        plugins,
        ollamaModel
    );

    // 4. Invoke (simpler!)
    var agentResult = await agent.RunAsync(promptText, cancellationToken: ct);
    var responseText = agentResult?.ToString() ?? "There was no response from the AI.";

    return new AgentChatResponse
    {
        Response = responseText,
        UsedAgentCapabilities = shouldUseTools
    };
}

private Dictionary<string, object> RegisterAgentPlugins()
{
    var plugins = new Dictionary<string, object>();
    plugins["CrossKnowledgeSearch"] = _serviceProvider.GetRequiredService<CrossKnowledgeSearchPlugin>();
    plugins["ModelRecommendation"] = _serviceProvider.GetRequiredService<ModelRecommendationPlugin>();
    plugins["KnowledgeAnalytics"] = _serviceProvider.GetRequiredService<KnowledgeAnalyticsPlugin>();
    plugins["SystemHealth"] = _serviceProvider.GetRequiredService<SystemHealthPlugin>();
    return plugins;
}
```

**Key Improvements:**
- ❌ No Kernel creation or management
- ❌ No complex ExecutionSettings
- ❌ No ChatCompletionAgent setup
- ✅ Single method creates agent with tools
- ✅ Plugins from DI (cleaner)
- ✅ 40% less code

---

### Example 3: Plugin/Tool Definition (Before/After)

**Before (SK Plugin):**
```csharp
using Microsoft.SemanticKernel;

public class CrossKnowledgeSearchPlugin
{
    private readonly IKnowledgeRepository _repository;
    private readonly KnowledgeManager _knowledgeManager;

    public CrossKnowledgeSearchPlugin(
        IKnowledgeRepository repository,
        KnowledgeManager knowledgeManager)
    {
        _repository = repository;
        _knowledgeManager = knowledgeManager;
    }

    [KernelFunction("search_knowledge")]
    [Description("Search a specific knowledge base for information")]
    public async Task<string> SearchKnowledgeAsync(
        [Description("Knowledge base ID to search")] string knowledgeId,
        [Description("Search query")] string query,
        [Description("Maximum number of results")] int limit = 10)
    {
        var results = await _knowledgeManager.SearchAsync(
            knowledgeId, query, limit, 0.3);

        if (!results.Any())
            return "No relevant information found.";

        var sb = new StringBuilder();
        foreach (var result in results)
        {
            sb.AppendLine($"[Score: {result.Score:F2}]");
            sb.AppendLine(result.Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
```

**After (AF Plugin):**
```csharp
using System.ComponentModel; // For [Description]

namespace KnowledgeEngine.Agents.AgentFramework;

public class CrossKnowledgeSearchPlugin
{
    private readonly IKnowledgeRepository _repository;
    private readonly KnowledgeManager _knowledgeManager;

    public CrossKnowledgeSearchPlugin(
        IKnowledgeRepository repository,
        KnowledgeManager knowledgeManager)
    {
        _repository = repository;
        _knowledgeManager = knowledgeManager;
    }

    [Description("Search a specific knowledge base for information")]
    public async Task<string> SearchKnowledgeAsync(
        string knowledgeId,
        string query,
        int limit = 10)
    {
        var results = await _knowledgeManager.SearchAsync(
            knowledgeId, query, limit, 0.3);

        if (!results.Any())
            return "No relevant information found.";

        var sb = new StringBuilder();
        foreach (var result in results)
        {
            sb.AppendLine($"[Score: {result.Score:F2}]");
            sb.AppendLine(result.Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
```

**Key Changes:**
- ❌ Removed `using Microsoft.SemanticKernel`
- ❌ Removed `[KernelFunction("search_knowledge")]` attribute
- ❌ Removed `[Description]` on parameters (not needed for AF)
- ✅ Kept class-level `[Description]` on method
- ✅ Business logic unchanged
- ✅ Simpler, cleaner code

**Registration (in ChatCompleteAF):**
```csharp
// SK: Complex plugin registration
kernel.Plugins.AddFromType<CrossKnowledgeSearchPlugin>();

// AF: Simple function registration
var tools = new Dictionary<string, object>();
tools["CrossKnowledgeSearch"] = _serviceProvider.GetRequiredService<CrossKnowledgeSearchPlugin>();
var agent = _agentFactory.CreateAgentWithPlugins(provider, systemPrompt, tools);
```

---

## Critical Blockers

### 🔴 **BLOCKER 1: Streaming Support in ChatCompleteAF**

**Status:** ⏳ NOT IMPLEMENTED
**Impact:** HIGH - Blocks deprecation of SK ChatComplete
**Estimated Effort:** 4-6 hours

**Problem:**
- ChatComplete.cs (SK) supports streaming responses
- ChatCompleteAF.cs (AF) does not have streaming yet
- Cannot deprecate SK until feature parity achieved

**Solution Required:**
```csharp
// Need to add these methods to ChatCompleteAF:
public virtual async IAsyncEnumerable<string> AskStreamingAsync(...)
{
    var agent = _agentFactory.CreateAgent(provider, systemMessage, ollamaModel);

    // TODO: Implement streaming with agent.RunAsync()
    // Agent Framework supports streaming, need to wire it up
    await foreach (var chunk in agent.RunStreamingAsync(...))
    {
        yield return chunk.ToString();
    }
}

public virtual async IAsyncEnumerable<AgentChatResponse> AskWithAgentStreamingAsync(...)
{
    // Similar streaming implementation with tools
}
```

**Action Items:**
- [ ] Research Agent Framework streaming API
- [ ] Implement AskStreamingAsync()
- [ ] Implement AskWithAgentStreamingAsync()
- [ ] Test with all providers
- [ ] Update API endpoints

---

### 🟡 **BLOCKER 2: TextChunker Replacement**

**Status:** ⏳ DECISION NEEDED
**Impact:** MEDIUM-HIGH - Affects RAG quality
**Estimated Effort:** 0-8 hours (depending on decision)

**Problem:**
- `TextChunker` is from `Microsoft.SemanticKernel.Text`
- Used in KnowledgeManager for document chunking (critical for RAG)
- No Agent Framework equivalent

**Current Usage:**
```csharp
// KnowledgeManager.cs:103-108
var lines = markdown
    ? TextChunker.SplitMarkDownLines(rawText, maxLine)
    : TextChunker.SplitPlainTextLines(rawText, maxLine);
var paragraphs = markdown
    ? TextChunker.SplitMarkdownParagraphs(lines, maxPara, overlap)
    : TextChunker.SplitPlainTextParagraphs(lines, maxPara, overlap);
```

**Options:**

**Option A: Keep SK TextChunker (RECOMMENDED START)**
- Pros: Zero code changes, production-ready
- Cons: Maintains SK dependency
- Effort: 0 hours
- Risk: LOW
- Action: Test if works standalone without other SK packages

**Option B: Custom Implementation**
- Pros: No dependencies, full control
- Cons: Development effort, maintenance burden, need to handle edge cases
- Effort: 4-8 hours
- Risk: MEDIUM-HIGH (affects RAG quality)
- Action: Only if Option A fails

**Option C: Third-Party Library (e.g., LangChain.NET)**
- Pros: Purpose-built for RAG, active development
- Cons: New dependency to evaluate
- Effort: 3-6 hours
- Risk: MEDIUM
- Action: Evaluate during Phase 4

**Decision Path:**
1. Try Option A first (test standalone SK TextChunker)
2. If works: Keep it ✅
3. If doesn't work: Evaluate Option C vs Option B
4. Document decision and rationale

---

### 🟡 **BLOCKER 3: Qdrant Vector Store Connector**

**Status:** ⏳ NEEDS INVESTIGATION
**Impact:** MEDIUM - Core vector operations
**Estimated Effort:** 0-10 hours (depending on finding)

**Problem:**
- Uses `Microsoft.SemanticKernel.Connectors.Qdrant`
- May be independent of SK core (name only)
- No Agent Framework equivalent

**Current Usage:**
```csharp
// QdrantVectorStoreStrategy.cs
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.Extensions.AI;

// Uses QdrantVectorStore class
var vectorStore = new QdrantVectorStore(...);
await vectorStore.UpsertAsync(collectionName, chunkId, embedding);
```

**Investigation Path:**
1. Test if SK Qdrant connector works without other SK packages
2. If yes: Keep it (0 hours) ✅
3. If no: Migrate to direct `Qdrant.Client` SDK (6-10 hours)

**Fallback Plan:**
- Use `Qdrant.Client` NuGet package directly
- Implement vector operations without SK wrapper
- Effort: 6-10 hours
- Risk: MEDIUM

---

## Risk Assessment

### High Risk Areas ⚠️

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **ChatCompleteAF missing features** | HIGH | MEDIUM | Add streaming support before deprecating SK |
| **TextChunker replacement affects RAG quality** | HIGH | MEDIUM | Keep SK TextChunker initially, extensive testing |
| **Chat history migration breaks existing conversations** | HIGH | LOW | Feature flag allows rollback, data validation |
| **Provider support gaps (Anthropic, Google)** | HIGH | LOW | Already tested in ChatCompleteAF, working |

### Medium Risk Areas

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Qdrant integration breaks** | MEDIUM | LOW | Test standalone SK connector first |
| **DI registration issues** | MEDIUM | MEDIUM | Careful testing, keep both SK+AF during transition |
| **Test coverage gaps** | MEDIUM | MEDIUM | Update tests in Phase 5, verify coverage |
| **Performance regression** | MEDIUM | LOW | Benchmark AF vs SK, compare metrics |

### Low Risk Areas ✅

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Plugin/tool conversion issues** | LOW | LOW | Business logic unchanged, simple attribute removal |
| **Health checker migration** | LOW | LOW | Direct SDK usage, well-documented |
| **Documentation outdated** | LOW | HIGH | Update in Phase 6, part of plan |

---

## Success Criteria

### Functional Criteria ✅

- [ ] All existing chat functionality works
- [ ] All providers supported (OpenAI, Anthropic, Google, Ollama)
- [ ] All 4 tools/plugins functional
- [ ] Tool calling works correctly
- [ ] Streaming responses work
- [ ] Chat history persisted correctly
- [ ] Vector search working
- [ ] RAG quality maintained
- [ ] MCP integration unaffected
- [ ] Feature flag removed (AF is default)

### Quality Criteria ✅

- [ ] All tests passing (>80% coverage maintained)
- [ ] No SK namespaces remaining (except standalone components if kept)
- [ ] Performance equal or better than SK baseline
- [ ] Memory usage equal or better than SK baseline
- [ ] No regressions in functionality
- [ ] Build time improved (fewer dependencies)
- [ ] CI/CD green

### Code Quality Criteria ✅

- [ ] No `#pragma warning disable SKEXP*` pragmas
- [ ] No `[Experimental("SKEXP0070")]` attributes
- [ ] No unused code or files
- [ ] Clean DI registration
- [ ] Consistent naming conventions
- [ ] Comments updated
- [ ] Code reduction achieved (target: 30-50% less code)

### Documentation Criteria ✅

- [ ] CLAUDE.md updated with AF architecture
- [ ] AGENT_FRAMEWORK_MIGRATION_PLAN.md marked complete
- [ ] README.md updated
- [ ] API documentation updated
- [ ] Breaking changes documented
- [ ] Migration notes created
- [ ] Performance benchmark results documented

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

**Last Updated:** 2025-12-17
**Author:** Claude (AI Assistant) + Wayne
**Review Status:** In Progress - Updated with comprehensive inspection findings
**Next Review:** After Phase 1 completion (Quick Wins)
