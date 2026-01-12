# Breaking Changes - Agent Framework Migration

**Migration:** Semantic Kernel 1.6 → Microsoft.Extensions.AI (Agent Framework)
**Date:** January 2026
**Branch:** `feature/agent-framework-tool-calling`

---

## Summary

The Agent Framework migration removed Semantic Kernel dependencies and replaced them with Microsoft.Extensions.AI (Agent Framework). While most changes are internal, there are some breaking changes and important notes for developers and users.

## 🔴 Breaking Changes

### 1. Removed Packages (7 total)

The following NuGet packages have been removed from the solution:

- `Microsoft.SemanticKernel` (main package)
- `Microsoft.SemanticKernel.Abstractions`
- `Microsoft.SemanticKernel.Connectors.Google`
- `Microsoft.SemanticKernel.Connectors.OpenAI`
- `Microsoft.SemanticKernel.Connectors.Ollama`
- `Microsoft.SemanticKernel.PromptTemplates.Handlebars`
- `Microsoft.SemanticKernel.Yaml`
- `Lost.SemanticKernel.Connectors.Anthropic` (third-party)

**Impact:** Any custom code depending on these packages will break. Migration to Agent Framework equivalents required.

### 2. Deprecated Classes

The following classes are deprecated but still present for reference:

- `ChatComplete.cs` - Replaced by `ChatCompleteAF.cs`
- `KernelFactory.cs` - Replaced by `AgentFactory.cs`

**Impact:** These files will be removed in a future release. Update any references to use AF equivalents.

### 3. API Method Signatures Changed

**ChatCompleteAF Methods** now include `conversationId` parameter:

```csharp
// OLD (SK version)
public Task<string> AskAsync(
    string userMessage,
    string? knowledgeId,
    List<ChatMessage> chatHistory,
    double apiTemperature,
    AiProvider provider,
    bool useExtendedInstructions = false,
    string? ollamaModel = null,
    CancellationToken ct = default)

// NEW (AF version)
public Task<string> AskAsync(
    string userMessage,
    string? knowledgeId,
    List<ChatMessage> chatHistory,
    double apiTemperature,
    AiProvider provider,
    bool useExtendedInstructions = false,
    string? ollamaModel = null,
    string? conversationId = null,  // ← NEW PARAMETER
    CancellationToken ct = default)
```

**Impact:** Direct callers of ChatCompleteAF methods need to update their signatures. The parameter is optional (defaults to null).

**Affected Methods:**
- `AskAsync()`
- `AskWithAgentAsync()`
- `AskStreamingAsync()`
- `AskWithAgentStreamingAsync()`

### 4. Plugin System Changes

**Old (SK Plugins):**
```csharp
[KernelFunction]
public async Task<string> SearchAllKnowledgeBasesAsync(...)
```

**New (AF Plugins):**
```csharp
[Description("Search across all knowledge bases")]
public async Task<string> SearchAllKnowledgeBasesAsync(...)
```

**Impact:** Plugin development must use AF conventions (`AIFunction`, `Description` attributes instead of `KernelFunction`).

## 🟡 Important Changes (Non-Breaking)

### 1. Text Chunking Strategy Changed

**Old:** Semantic Kernel `Microsoft.SemanticKernel.Text.TextChunker`
**New:** SemanticChunker.NET with embedding-based semantic chunking

**Impact:**
- Better semantic coherence in chunks
- Slight performance difference (semantic chunking uses embeddings)
- Chunk boundaries may differ from SK version

**Mitigation:** Existing knowledge bases continue to work. Re-index for optimal results.

### 2. Agent Creation Pattern Changed

**Old (SK):**
```csharp
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(model, apiKey)
    .Build();
```

**New (AF):**
```csharp
var client = new OpenAIClient(apiKey);
var chatClient = client.GetChatClient(model);
var agent = chatClient.CreateAIAgent(instructions, tools);
```

**Impact:** Custom agent creation code must migrate to AF pattern.

### 3. Conversation Tracking Now Enabled

**Old:** Conversation IDs were hardcoded to `null` (disabled)
**New:** Conversation IDs properly tracked and passed through the system

**Impact:**
- System health metrics now show accurate conversation counts
- Usage analytics properly linked to conversations
- No action needed - works automatically

### 4. Tool Calling Now Universal

**Old:** Only OpenAI had tool calling properly configured
**New:** All providers (OpenAI, Gemini, Anthropic, Ollama) support tool calling

**Impact:**
- Agent tools now work for all providers
- Better functionality across all LLM providers
- No action needed - works automatically

## ✅ What Stays the Same

### 1. Vector Store Connectors (Kept)

- `Microsoft.SemanticKernel.Connectors.Qdrant` - **KEPT**
- `Microsoft.SemanticKernel.Connectors.MongoDB` - **KEPT**

**Reason:** These packages are built on `Microsoft.Extensions.VectorData.Abstractions` (framework-agnostic). The "SemanticKernel" namespace is just a naming convention.

**Impact:** No changes needed for Qdrant or MongoDB usage.

### 2. API Endpoints (Unchanged)

All REST API endpoints remain the same:
- `POST /api/chat` - Chat with knowledge
- `POST /api/knowledge` - Upload documents
- `GET /api/knowledge` - List knowledge bases
- `DELETE /api/knowledge/{id}` - Delete knowledge
- All analytics endpoints

**Impact:** No API client changes needed.

### 3. Configuration (Unchanged)

All `appsettings.json` configuration remains compatible:
- `ChatCompleteSettings`
- `OllamaSettings`
- `QdrantSettings`
- API keys via environment variables

**Impact:** No configuration changes needed.

### 4. Database Schema (Unchanged)

SQLite database schema remains the same:
- `Conversations` table
- `Messages` table
- `UsageMetrics` table
- `Knowledge` table

**Impact:** No database migration needed.

## 📋 Migration Guide

### For Users (No Action Required)

If you're using the system via:
- Docker deployment
- REST API
- Web UI

**No action is required.** The migration is transparent. Just update to the latest Docker image or pull the latest code.

### For Developers (Custom Integrations)

If you have custom code that:
1. **Directly uses ChatComplete.cs or KernelFactory.cs**
   - Migrate to `ChatCompleteAF.cs` and `AgentFactory.cs`
   - Update method signatures to include `conversationId` parameter

2. **Creates custom SK plugins**
   - Convert to AF plugins using `AIFunction` and `Description` attributes
   - Use `AgentToolRegistration.CreateToolsFromPlugins()` for registration

3. **Depends on removed SK packages**
   - Migrate to Agent Framework equivalents
   - See [AF_MIGRATION_STATUS.md](AF_MIGRATION_STATUS.md) for examples

### For Plugin Developers

**Old SK Plugin:**
```csharp
public class MyPlugin
{
    [KernelFunction("search_documents")]
    [Description("Searches documents")]
    public async Task<string> SearchAsync(string query) { }
}
```

**New AF Plugin:**
```csharp
public class MyPlugin
{
    [Description("Searches documents")]
    public async Task<string> SearchAsync(
        [Description("Search query")] string query)
    {
        // Implementation
    }
}
```

**Registration:**
```csharp
// Old (SK)
kernel.Plugins.AddFromObject(new MyPlugin());

// New (AF)
var plugins = new Dictionary<string, object>
{
    { "MyPlugin", new MyPlugin() }
};
var tools = AgentToolRegistration.CreateToolsFromPlugins(plugins);
var agent = client.CreateAIAgent(instructions, tools);
```

## 🐛 Bug Fixes Included

The migration also fixed two critical bugs:

### Bug #1: Tool Calling Not Working (Google/Anthropic/Ollama)
**Symptom:** Tools were registered but never called - LLMs generated text responses instead
**Fix:** All providers now use `CreateAIAgent()` with tools

### Bug #2: Conversation Tracking Disabled
**Symptom:** System health reported 0 conversations despite active sessions
**Fix:** Added `conversationId` parameter throughout the stack

## 📊 Testing

**Test Coverage:** 578 tests passing (100% pass rate)
- 166 MCP tests
- 412 KnowledgeManager tests

**Testing Recommendations:**
1. Run full test suite: `dotnet test`
2. Smoke test all 4 providers with agent tools
3. Verify conversation tracking in analytics
4. Check system health metrics

## 📚 Documentation

For more details, see:
- [AF_MIGRATION_STATUS.md](AF_MIGRATION_STATUS.md) - Complete migration tracker
- [AGENT_FRAMEWORK_MIGRATION_PLAN.md](AGENT_FRAMEWORK_MIGRATION_PLAN.md) - Original migration plan
- [CLAUDE.md](../CLAUDE.md) - Updated project context
- [README.md](../README.md) - Updated architecture overview

## 🆘 Support

If you encounter issues related to the migration:

1. **Check logs** - Look for `[AF]` prefixed log messages
2. **Verify configuration** - Ensure `UseAgentFramework: true` in appsettings.json
3. **Run tests** - `dotnet test` to verify system integrity
4. **Report issues** - https://github.com/waynen12/ChatComplete/issues

---

**Migration Completed:** January 12, 2026
**Total Effort:** 41.5 hours
**Branch:** `feature/agent-framework-tool-calling`
