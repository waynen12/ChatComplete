# Agent Framework Migration Status

**Branch:** `feature/agent-framework-tool-calling`
**Started:** 2025-01-24
**Target Completion:** TBD
**Status:** 🔄 IN PROGRESS

---

## Migration Strategy

### Guiding Principles
1. ✅ **Feature Flag Controlled**: `UseAgentFramework` setting (default: false)
2. ✅ **Phased by Provider**: OpenAI → Gemini → Ollama → Anthropic
3. ✅ **Read-Only First**: Search/health tools before write operations
4. ✅ **Test Each Step**: Smoke tests before moving to next provider
5. ✅ **No Rollback Plan**: Branch-only development, merge when 100% complete

### Branch Strategy
- All work in `feature/agent-framework-tool-calling`
- No merge to `main` until fully working
- SK stays as fallback until AF 100% complete

---

## Phase 1: Foundation ✅

| Task | Status | Notes |
|------|--------|-------|
| Create `UseAgentFramework` config flag | ✅ DONE | Added to ChatCompleteSettings.cs |
| Add flag to appsettings.json (Api) | ✅ DONE | Default: false |
| Add flag to appsettings.json (Mcp) | ✅ DONE | Default: false |
| Create AgentFactory.cs | ✅ DONE | Multi-provider support |
| Create AgentToolRegistration.cs | ✅ DONE | Reflection-based tool creation |
| Create AF CrossKnowledgeSearchPlugin | ✅ DONE | First AF plugin |

---

## Phase 2: Core Chat Migration (OpenAI First) 🔄

### 2.1: ChatComplete.cs - OpenAI Provider

| Task | Status | Files |
|------|--------|-------|
| Create AF version of AskWithAgentAsync | ⏳ IN PROGRESS | ChatComplete.cs |
| Replace Kernel with ChatClient (OpenAI) | ⏳ TODO | ChatComplete.cs |
| Replace ChatHistory with AF conversation | ⏳ TODO | ChatComplete.cs |
| Handle tool calling with AF | ⏳ TODO | ChatComplete.cs |
| Add feature flag check | ⏳ TODO | ChatComplete.cs |
| Test OpenAI with CrossKnowledgeSearchPlugin | ⏳ TODO | Manual testing |

### 2.2: Plugin Migration (Read-Only Tools)

| Plugin | Functions | Status | Priority |
|--------|-----------|--------|----------|
| CrossKnowledgeSearchPlugin | 1 function | ✅ DONE | HIGH |
| SystemHealthAgent | 6 functions | ⏳ TODO | HIGH |
| ModelRecommendationAgent | 3 functions | ⏳ TODO | MEDIUM |
| KnowledgeAnalyticsAgent | 1 function | ⏳ TODO | MEDIUM |

---

## Phase 3: Provider Migration 🔜

### 3.1: Gemini Provider
| Task | Status |
|------|--------|
| Add Gemini support to AgentFactory | ⏳ TODO |
| Test tool calling with Gemini | ⏳ TODO |
| Verify all plugins work | ⏳ TODO |

### 3.2: Ollama Provider
| Task | Status |
|------|--------|
| Resolve OllamaSharp version conflict | ⏳ TODO |
| Add Ollama support to AgentFactory | ⏳ TODO |
| Test tool calling with Ollama | ⏳ TODO |
| Verify all plugins work | ⏳ TODO |

### 3.3: Anthropic Provider
| Task | Status |
|------|--------|
| Replace Lost.SK connector with Anthropic.SDK | ⏳ TODO |
| Add Anthropic support to AgentFactory | ⏳ TODO |
| Enable tool calling (currently disabled) | ⏳ TODO |
| Test all plugins work | ⏳ TODO |

---

## Phase 4: Supporting Infrastructure 🔜

### 4.1: Chat Services
| File | Status | Notes |
|------|--------|-------|
| SqliteChatService.cs | ⏳ TODO | Replace ChatHistory usage |
| MongoChatService.cs | ⏳ TODO | Replace ChatHistory usage |

### 4.2: Health Checkers
| File | Status |
|------|--------|
| OpenAIHealthChecker.cs | ⏳ TODO |
| GoogleAIHealthChecker.cs | ⏳ TODO |
| AnthropicHealthChecker.cs | ⏳ TODO |

### 4.3: Embedding Services
| File | Status |
|------|--------|
| ServiceCollectionExtensions.cs | ⏳ TODO |
| EmbeddingsHelper.cs | ⏳ TODO |
| QdrantIndexManager.cs | ⏳ TODO |
| QdrantVectorStoreStrategy.cs | ⏳ TODO |

---

## Phase 5: Cleanup & Package Removal 🔜

### 5.1: File Deletion
| File | Reason | Status |
|------|--------|--------|
| KernelFactory.cs | Replaced by AgentFactory | ⏳ TODO |
| KernelHelper.cs | Obsolete | ⏳ TODO |

### 5.2: NuGet Package Removal
| Package | Replacement | Status |
|---------|-------------|--------|
| Microsoft.SemanticKernel | Microsoft.Agents.AI | ⏳ TODO |
| Microsoft.SemanticKernel.Connectors.Google | Google.GenerativeAI.Microsoft | ⏳ TODO |
| Microsoft.SemanticKernel.Connectors.Ollama | OllamaSharp | ⏳ TODO |
| Microsoft.SemanticKernel.Connectors.Qdrant | Keep (works with AF) | ⏳ TODO |
| Microsoft.SemanticKernel.PromptTemplates.Handlebars | Remove | ⏳ TODO |
| Microsoft.SemanticKernel.Yaml | Remove | ⏳ TODO |
| Lost.SemanticKernel.Connectors.Anthropic | Anthropic.SDK | ⏳ TODO |
| Microsoft.SemanticKernel.Connectors.MongoDb | TBD (check usage) | ⏳ TODO |

### 5.3: Code Cleanup
| Task | Status |
|------|--------|
| Remove #pragma warning SKEXP* suppressions | ⏳ TODO |
| Update all using statements | ⏳ TODO |
| Remove SK-specific attributes | ⏳ TODO |

---

## Phase 6: Testing & Documentation 🔜

### 6.1: Smoke Tests
| Test | Status |
|------|--------|
| OpenAI tool calling smoke test | ⏳ TODO |
| Gemini tool calling smoke test | ⏳ TODO |
| Ollama tool calling smoke test | ⏳ TODO |
| Anthropic tool calling smoke test | ⏳ TODO |
| Cross-knowledge search smoke test | ⏳ TODO |
| System health smoke test | ⏳ TODO |

### 6.2: Integration Tests
| Test | Status |
|------|--------|
| Update SpyChatComplete.cs | ⏳ TODO |
| Update ChatHistoryReducerTests.cs | ⏳ TODO |
| Update health checker tests | ⏳ TODO |
| Update agent plugin tests | ⏳ TODO |

### 6.3: Documentation
| Document | Status |
|----------|--------|
| Update CLAUDE.md | ⏳ TODO |
| Update AGENT_FRAMEWORK_MIGRATION_PLAN.md | ⏳ TODO |
| Update MASTER_TEST_PLAN.md | ⏳ TODO |
| Create AF_ARCHITECTURE.md | ⏳ TODO |

---

## Known Issues & Blockers

### Resolved ✅
1. ✅ **Anthropic tool calling error** - Third-party SK connector incompatible
   - **Solution**: Disabled tool calling for Anthropic in SK mode
   - **AF Fix**: Will use official Anthropic.SDK

2. ✅ **Nullable parameter JSON serialization** - OllamaSharp couldn't serialize `double?`
   - **Solution**: Changed to sentinel value pattern (`-1.0` = use config default)

### Active 🔄
1. 🔄 **OpenAI repeated tool calling** - No iteration limits in SK 1.64
   - **Status**: Accepted as SK behavior (thorough but slow)
   - **AF Fix**: May have better iteration controls

2. 🔄 **OllamaSharp version conflict** - SK connector vs standalone package
   - **Status**: OllamaSharp removed, Ollama disabled in AgentFactory
   - **AF Fix**: Need to resolve before Ollama AF support

### Pending ⏳
None currently

---

## Testing Checklist

### Per-Provider Testing
For each provider (OpenAI, Gemini, Ollama, Anthropic):
- [ ] Tool calling works with CrossKnowledgeSearchPlugin
- [ ] Tool calling works with SystemHealthAgent
- [ ] Tool calling works with ModelRecommendationAgent
- [ ] Tool calling works with KnowledgeAnalyticsAgent
- [ ] Regular chat (no tools) still works
- [ ] Error handling works correctly
- [ ] Performance is acceptable

### Integration Testing
- [ ] Multiple tool calls in single conversation
- [ ] Tool call results are properly integrated into responses
- [ ] Switching providers works correctly
- [ ] Feature flag toggle works (SK ↔ AF)

### Regression Testing
- [ ] Existing chat conversations still work
- [ ] All API endpoints functional
- [ ] MCP server still operational
- [ ] Analytics tracking still works
- [ ] Health checks still work

---

## Success Criteria

Migration is complete when:
1. ✅ All 4 providers support tool calling in AF mode
2. ✅ All 4 plugins converted to AF tools
3. ✅ All smoke tests passing
4. ✅ Integration tests updated and passing
5. ✅ SK packages removed from solution
6. ✅ Documentation updated
7. ✅ Feature flag set to `UseAgentFramework: true` by default
8. ✅ Tested on local dev environment
9. ✅ Tested on remote test server (if needed)
10. ✅ Approved for merge to main

---

## Timeline Estimate

| Phase | Estimated Hours | Status |
|-------|-----------------|--------|
| Phase 1: Foundation | 2-3 hours | ✅ COMPLETE |
| Phase 2: Core Chat (OpenAI) | 8-12 hours | 🔄 IN PROGRESS |
| Phase 3: Provider Migration | 6-10 hours | ⏳ TODO |
| Phase 4: Supporting Infrastructure | 5-8 hours | ⏳ TODO |
| Phase 5: Cleanup | 3-5 hours | ⏳ TODO |
| Phase 6: Testing & Docs | 4-6 hours | ⏳ TODO |
| **Total** | **28-44 hours** | **~5% complete** |

---

## Next Steps

1. **Implement AF mode in ChatComplete.cs for OpenAI**
   - Add `if (UseAgentFramework)` branching
   - Create `AskWithAgentAsync_AF()` method
   - Use AgentFactory to create OpenAI agent
   - Register CrossKnowledgeSearchPlugin

2. **Test OpenAI with AF mode**
   - Enable flag: `"UseAgentFramework": true`
   - Test cross-knowledge search
   - Verify tool calling works
   - Compare results with SK mode

3. **Add remaining plugins**
   - SystemHealthAgent (6 functions)
   - ModelRecommendationAgent (3 functions)
   - KnowledgeAnalyticsAgent (1 function)

---

**Last Updated:** 2025-01-24
**Updated By:** Claude Code
**Next Review:** After OpenAI AF implementation complete
