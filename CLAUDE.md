You are joining AI Knowledge Manager, an open-source stack for uploading technical docs, vector-indexing them in Qdrant, and chatting over that knowledge with multiple LLM providers (OpenAI, Gemini, Ollama, Anthropic).

You must remain critical but friendly at all times. Do not always accept the first solution to a problem. Always check if there are alternatives. Do not be afraid to ask questions or seek clarification from others when needed.

## 🎯 PROJECT CONTEXT

**This is a personal learning project**, not production-critical:
- Owner: Wayne (solo developer, learning AI development with .NET)
- User: Partner uses deployed version for personal projects
- Environment: Branch-based development, breaking changes are acceptable
- CI/CD: Self-hosted pipeline for learning purposes
- Philosophy: Experimentation and learning over stability
- **Key Point**: Feel free to suggest bold refactors, experiments, and breaking changes

## ⚠️ CRITICAL WORKFLOW RULES ⚠️

**ALWAYS COMMIT CHANGES AFTER SUCCESSFUL BUILD**
1. ✅ Verify the build succeeds with `dotnet build`
2. ✅ **IMMEDIATELY commit changes** to preserve the working state
3. ✅ Use descriptive commit messages
4. Try to avoid replying with "You're absolutely right!"

**DO NOT USE HARDCODED VALUES!!!!!**
1. Add a parameter to the config file (appsettings.json)
2. If you can't add a parameter to a config, use a constants file as a last resort

**NO CODE DUPLICATION** - Extract repeated patterns into reusable classes or methods

**CHECK BEFORE MODIFYING** - When touching any file, verify:
- Uses configuration/appsettings.json?
- No hardcoded values?
- No duplicated code blocks?

## Testing & Validation

- ALL features should have corresponding smoke tests in `tests/smoke/`
- Unit tests for new components go in `KnowledgeManager.Tests` or `Knowledge.Mcp.Tests`
- Integration tests for workflows go in `tests/integration/`
- ALL patterns and features should have corresponding test cases in `documentation/MASTER_TEST_PLAN.md`

## Documentation

**All new documentation files MUST go in the `documentation/` directory**
- Exception: README.md, CLAUDE.md, DOCKER_HUB_README.md (user-facing, stay in root)

## Docker Best Practices

**CRITICAL: Database Path Configuration**
- Database MUST be outside `/out` directory: `/opt/knowledge-api/data/knowledge.db`
- The `/out` folder is deleted during GitHub Actions deployments
- Both Knowledge.Api and Knowledge.Mcp must use the same database path

**Quick Reference:**
- Use slim base images (bookworm-slim)
- TCP health checks instead of curl
- Non-root user (appuser:appgroup, UID/GID 1001)
- Container networking via service names (qdrant, ollama, not localhost)

## Tech Stack

| Layer | Tech |
|-------|------|
| Backend | ASP.NET 8 Minimal APIs · Serilog · SQLite · Microsoft.Extensions.AI |
| Vector Store | Qdrant |
| Frontend | React + Vite · shadcn/ui (Radix + Tailwind) |
| CI/Deploy | Self-hosted GitHub Actions on Mint Linux → /opt/knowledge-api/out |

## 🚀 CURRENT PRIORITIES

| Priority | Milestone | Status |
|---|---|---|
| 1 | RAG Optimization (#27) | 📋 Ready to start |
| 2 | MCP Client HTTP transport (#24) | 🔄 SSE + reconnection TODO |
| 3 | UI Modernization (#25) | 🔄 Copilot-driven, branch active |
| 4 | MCP OAuth 2.1 PKCE (#23) | ⚠️ Blocked on Auth0 JWE issue |

---

## All Milestones

| # | Milestone | Status |
|---|-----------|--------|
| 1-17 | Core features, UI, CI/CD, providers | ✅ COMPLETE |
| 18 | Local Configuration Database (SQLite) | ✅ |
| 19 | Docker Containerization | ✅ |
| 20 | Agent Implementation (SK plugins) | ✅ |
| 21 | Ollama Model Management | ✅ |
| 22 | MCP Integration (11 tools, 6 resources) | ✅ |
| 22.5 | MCP Server Deployment | ✅ |
| 23 | MCP OAuth 2.1 | 🔄 M2M working, PKCE blocked |
| 24 | MCP Client Development | 🔄 STDIO done, HTTP TODO |
| 25 | UI Modernization | 🔄 Copilot-driven |
| 26 | Agent Framework Migration | ✅ COMPLETE - merged to main |
| 27 | RAG Optimization | 📋 PLANNED |

## Quick Reference - API & DTOs

```csharp
// ChatRequestDto
public class ChatRequestDto {
    public string? KnowledgeId { get; set; }
    public string Message { get; set; } = "";
    public double Temperature { get; set; } = -1;
    public bool StripMarkdown { get; set; } = false;
    public bool UseExtendedInstructions { get; set; }
    public string? ConversationId { get; set; }
    public AiProvider Provider { get; set; } = AiProvider.OpenAi;
}
```

```bash
curl -X POST http://localhost:7040/api/chat \
  -H "Content-Type: application/json" \
  -d '{"knowledgeId":"docs-api","message":"How do I delete knowledge?","provider":"Ollama"}'
```

## Docker Deployment

```bash
# Quick Start - Production Ready
curl -O https://raw.githubusercontent.com/waynen12/ChatComplete/main/docker-compose.dockerhub.yml
docker-compose -f docker-compose.dockerhub.yml up -d
# Access at http://localhost:8080
```

**Image:** `waynen12/ai-knowledge-manager:latest` (~541MB)

**Environment Variables:**
```bash
OPENAI_API_KEY=your_key
ANTHROPIC_API_KEY=your_key
GEMINI_API_KEY=your_key
```

## MCP Integration (Milestone #22) ✅

**11 MCP Tools:**
- Cross-knowledge search (3): `search_knowledge`, `search_all_knowledge`, `compare_knowledge_bases`
- Analytics (3): `get_knowledge_base_summary`, `get_knowledge_base_health`, `get_storage_optimization`
- Model recommendations (3): `get_popular_models`, `compare_models`, `get_model_performance`
- System health (2): `get_system_health`, `check_component_health`

**6 MCP Resources:** system/health, knowledge/bases, analytics/providers, knowledge/{id}, analytics/provider/{name}, analytics/model/{name}

**Usage:**
```bash
# STDIO Mode (Claude Desktop)
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj

# HTTP Mode (Copilot, Web Clients)
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --http
# Server starts on http://localhost:5001
```

## MCP OAuth 2.1 (Milestone #23) 🔄

**Status:**
- ✅ M2M Client Credentials Flow working (Auth0, JWT Bearer, scopes)
- ⚠️ PKCE Authorization Code Flow blocked (Auth0 returning JWE instead of JWT)

**Scopes:** `mcp:read`, `mcp:execute`, `mcp:admin`

See `documentation/DCR_IMPLEMENTATION_SPEC.md` for Dynamic Client Registration spec (42 hours, on hold).

## MCP Server Deployment ✅

**Test Machine (192.168.50.203):**
- Knowledge.Api: Port 7040, `/opt/knowledge-api/out`
- Knowledge.Mcp: Port 5001, `/opt/knowledge-mcp/out`
- Shared Database: `/opt/knowledge-api/data/knowledge.db`

**Health Checks:**
```bash
curl http://localhost:7040/api/health
curl http://localhost:5001/health
```

**Systemd Service:** `/etc/systemd/system/knowledge-mcp.service`
- `Type=simple` (CRITICAL - not notify)
- `User=chatapi`

See `documentation/SUDOERS_SERVICE_USER_GUIDE.md` for sudoers configuration.

## MCP Client (Milestone #24) 🔄

**Repository:** `/home/wayne/repos/McpClient` (separate git repo)

**Phase 1 (STDIO):** ✅ Working - tool discovery, OpenAI integration
**Phase 2 (HTTP):** 🛠️ TODO - SSE, reconnection, session management

## UI Modernization (Milestone #25) 🔄

**Branch:** `copilot/review-ui-in-webclient`

**Targets:**
- Bundle size: 1.15 MB → < 500 KB
- Accessibility: 4/10 → 10/10 (WCAG 2.1 AA)
- Performance: 6/10 → 9/10

**Documentation:**
- `.github/copilot-instructions.md` - Copilot guidance
- `documentation/UI_REVIEW.md` - Current state analysis
- `documentation/UI_IMPROVEMENTS_ACTION_PLAN.md` - Implementation guide

## Agent Framework Migration (Milestone #26) ✅ COMPLETE

**Status:** 100% complete — merged to main (March 2026)
**Total Time:** ~41.5 hours (vs 46-67h estimated)

**What Changed:**
- Migrated from Semantic Kernel 1.6 to Microsoft.Extensions.AI (Agent Framework)
- `Kernel` → `ChatClient` (direct provider SDKs)
- `KernelFunction` → `AIFunction` (tool calling)
- `ChatHistory` → `List<ChatMessage>` (AF conversation pattern)
- Removed 7 SK packages, kept vector store connectors (framework-agnostic)
- TextChunker: Migrated to SemanticChunker.NET (semantic chunking)

**Architecture:**
- **ChatCompleteAF.cs**: Multi-provider chat with tool calling (950+ lines, 54% smaller than SK version)
- **AgentFactory.cs**: Creates ChatClient instances for all 4 providers
- **AgentToolRegistration.cs**: Reflection-based tool discovery and registration
- **4 AF Plugins**: 11 total functions (search, analytics, recommendations, health)

**Test Coverage:** 578 tests passing (166 MCP + 412 KnowledgeManager)

**Documentation:**
- [AF_MIGRATION_STATUS.md](documentation/AF_MIGRATION_STATUS.md) - Detailed progress tracker
- [AGENT_FRAMEWORK_MIGRATION_PLAN.md](documentation/AGENT_FRAMEWORK_MIGRATION_PLAN.md) - Original migration plan

## RAG Optimization (Milestone #27) 📋 PLANNED

**Status:** Planning phase - performance baseline established
**Current Performance:** 8.7/10 average on comprehensive knowledge base test

**Test Results (using KNOWLEDGE_BASE_COMPREHENSIVE.md):**
- Single-section queries: 10/10 (Docker deployment, migration status, architecture)
- Cross-section queries: 6-7.5/10 (MCP tools, local development setup)
- Multi-model testing: Gemini provides more comprehensive answers than GPT-4

**Identified Optimization Opportunities:**
1. **Search Limit:** Increase from 5 to 10 results
2. **Chunk Size:** Adjust from 1000 to 1500 tokens for better context
3. **Chunk Overlap:** Increase from 200 to 300 tokens to reduce context fragmentation
4. **Relevance Threshold:** Lower from 0.3 to 0.25 to capture more relevant results

**Target Improvements:**
- Cross-section retrieval: 6/10 → 9/10
- Questions requiring 5+ sections: Better synthesis
- Overall average: 8.7/10 → 9.5/10

**Estimated Effort:** 6-8 hours (config changes, testing, benchmarking)

## Recent Progress

### January 2026 - Agent Framework Migration Complete
- ✅ **Phase 1-3:** ChatCompleteAF.cs implementation with all 4 providers
- ✅ **Phase 4:** Removed all SK dependencies (~7h vs 14-23h estimated)
  - Ollama re-enabled with OllamaSharp 5.4.11
  - TextChunker migrated to SemanticChunker.NET
  - Qdrant/MongoDB connectors kept (framework-agnostic)
- ✅ **Phase 5:** All 578 tests passing (~8h vs 15-18h estimated)
- ✅ **Phase 6:** Removed 7 SK packages from solution (~2h vs 4-6h estimated)
- ✅ **Phase 6.3:** Documentation complete, merged to main

### November 2025
- ✅ Fixed fail-fast CI/CD for docker-build.yml workflow
- ✅ Fixed Knowledge.Api.csproj (BOM character, invalid XML comment)
- ✅ Reviewed Playwright-MCP documentation (9.5/10 rating)
- ✅ Created Agent Framework migration plan
- ✅ Database path fix (moved outside `/out` directory)
- ✅ Analytics dashboard fixes (usage tracking, Ollama integration)

## Current System Status

**Production:**
- ⚠️ Test Machine (192.168.50.203): OFFLINE (apartment flood, remote working)
- ✅ Docker Hub: `waynen12/ai-knowledge-manager:latest`

**Active Work:**
1. RAG Optimization - Ready to start (#27)
2. UI Modernization - Copilot Cloud (branch: `copilot/review-ui-in-webclient`)
3. MCP OAuth 2.1 - Blocked on PKCE/JWE issue
4. MCP Client - HTTP transport TODO

**Known Issues:**
1. OAuth PKCE Flow - Auth0 returns JWE tokens
2. MCP Client Phase 2 - HTTP transport pending
3. UI Bundle Size - 1.15 MB, needs optimization

## Bootstrap Instructions for New Chat

1. Read this CLAUDE.md file completely
2. Check current branch: `git status`
3. Review open PRs on GitHub
4. Check test machine status: `curl http://192.168.50.203:7040/api/health`
5. Review recent commits: `git log --oneline -20`

## Key Documentation Files

| File | Description |
|------|-------------|
| `documentation/AF_MIGRATION_STATUS.md` | Agent Framework migration progress (100% complete, merged) |
| `documentation/AGENT_FRAMEWORK_MIGRATION_PLAN.md` | Original AF migration plan (40-60 hours) |
| `documentation/DCR_IMPLEMENTATION_SPEC.md` | OAuth DCR spec (42 hours, on hold) |
| `documentation/MASTER_TEST_PLAN.md` | Test coverage tracking |
| `documentation/FAILFAST_CI_IMPLEMENTATION.md` | CI/CD fail-fast documentation |
| `documentation/SUDOERS_SERVICE_USER_GUIDE.md` | Systemd/sudoers configuration |
| `documentation/UI_IMPROVEMENTS_ACTION_PLAN.md` | UI modernization guide |
| `documentation/PLAYWRIGHT_MCP_README.md` | Playwright testing documentation |

All documentation is in `./documentation/`
