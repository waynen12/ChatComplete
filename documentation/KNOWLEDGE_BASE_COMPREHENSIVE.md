# AI Knowledge Manager - Comprehensive Technical Documentation

> **Last Updated:** December 22, 2025
> **Version:** Phase 3 Complete (Agent Framework Migration - 35% Complete)
> **Status:** Active Development - Milestone #26

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Technology Stack](#technology-stack)
4. [Current Status](#current-status)
5. [Installation & Deployment](#installation--deployment)
6. [Core Features](#core-features)
7. [MCP Integration](#mcp-integration)
8. [Agent Framework Migration](#agent-framework-migration)
9. [Development Guidelines](#development-guidelines)
10. [API Reference](#api-reference)

---

## Project Overview

**AI Knowledge Manager** is an open-source stack for uploading technical documentation, vector-indexing it in Qdrant, and chatting over that knowledge with multiple LLM providers (OpenAI, Gemini, Ollama, Anthropic).

### Key Information
- **Owner:** Wayne (solo developer, learning AI development with .NET)
- **User:** Partner uses deployed version for personal projects
- **Philosophy:** Experimentation and learning over stability
- **Environment:** Branch-based development, breaking changes acceptable
- **CI/CD:** Self-hosted pipeline on Mint Linux

### Project Goals
- Learn AI development with .NET
- Build practical RAG (Retrieval-Augmented Generation) system
- Experiment with multiple LLM providers
- Understand vector databases and semantic search
- Implement modern chat interfaces with React

---

## Architecture

### High-Level Architecture

```
┌─────────────────┐
│   React + Vite  │  Frontend (Vite dev server / static build)
│   shadcn/ui     │
└────────┬────────┘
         │ HTTP/REST
         ▼
┌─────────────────┐
│ Knowledge.Api   │  ASP.NET 8 Minimal APIs
│ (Port 7040)     │
└────────┬────────┘
         │
         ├─────────────┬──────────────┬─────────────┐
         │             │              │             │
         ▼             ▼              ▼             ▼
┌──────────────┐ ┌──────────┐ ┌───────────┐ ┌──────────┐
│ ChatCompleteAF│ │  Qdrant  │ │  SQLite   │ │ Ollama   │
│ Agent Framework│ │ (Vector) │ │ (Metadata)│ │ (Local)  │
└──────────────┘ └──────────┘ └───────────┘ └──────────┘
         │
         └─── Calls: OpenAI | Anthropic | Gemini | Ollama
```

### Component Responsibilities

| Component | Responsibility | Technology |
|-----------|---------------|------------|
| **Frontend** | User interface for chat, knowledge upload, analytics | React + Vite, shadcn/ui |
| **Knowledge.Api** | REST API, business logic, routing | ASP.NET 8 Minimal APIs |
| **ChatCompleteAF** | LLM interaction, RAG orchestration | Microsoft Agent Framework |
| **Qdrant** | Vector storage, semantic search | Qdrant vector database |
| **SQLite** | Metadata, conversations, usage tracking | SQLite with Entity Framework |
| **Ollama** | Local LLM hosting | Ollama (optional) |

---

## Technology Stack

### Backend
- **Runtime:** .NET 8
- **API Framework:** ASP.NET 8 Minimal APIs
- **AI Framework:** Microsoft Agent Framework (migrating from Semantic Kernel)
- **Vector Store:** Qdrant
- **Database:** SQLite + Entity Framework Core
- **Logging:** Serilog
- **Embeddings:** text-embedding-3-small (OpenAI) or nomic-embed-text (Ollama)

### Frontend
- **Framework:** React 18 + Vite
- **UI Components:** shadcn/ui (Radix + Tailwind CSS)
- **State Management:** React Context API
- **HTTP Client:** Fetch API
- **Charts:** Recharts (d3.js)

### LLM Providers
- **OpenAI:** GPT-4, GPT-3.5-turbo
- **Anthropic:** Claude Sonnet 4.5, Claude Opus 4.5
- **Google:** Gemini 1.5 Pro
- **Ollama:** Local models (llama3, mistral, etc.)

### DevOps
- **CI/CD:** Self-hosted GitHub Actions
- **Deployment:** Systemd services on Mint Linux
- **Containerization:** Docker + Docker Compose
- **Docker Hub:** `waynen12/ai-knowledge-manager:latest`

---

## Current Status

### Milestones (1-26)

| # | Milestone | Status |
|---|-----------|--------|
| 1-17 | Core features, UI, CI/CD, providers | ✅ COMPLETE |
| 18 | Local Configuration Database (SQLite) | ✅ COMPLETE |
| 19 | Docker Containerization | ✅ COMPLETE |
| 20 | Agent Implementation (SK plugins) | ✅ COMPLETE |
| 21 | Ollama Model Management | ✅ COMPLETE |
| 22 | MCP Integration (11 tools, 6 resources) | ✅ COMPLETE |
| 22.5 | MCP Server Deployment | ✅ COMPLETE |
| 23 | MCP OAuth 2.1 | 🔄 M2M working, PKCE blocked |
| 24 | MCP Client Development | 🔄 STDIO done, HTTP TODO |
| 25 | UI Modernization | 🔄 Copilot-driven |
| **26** | **Agent Framework Migration** | **🟡 IN PROGRESS - 35%** |

### Phase 3 Complete (December 22, 2025)
- ✅ Deleted ChatComplete.cs (1,290 lines SK code)
- ✅ Deleted KernelFactory.cs (~200 lines)
- ✅ Deleted 4 SK plugin files
- ✅ Refactored chat services to AF-only (removed dual routing)
- ✅ Removed UseAgentFramework feature flag
- ✅ Fixed foreign key constraint errors in usage tracking
- ✅ Build succeeded - 0 errors

### Next Steps
- **Phase 4:** Refactor health checkers, TextChunker, Qdrant (12-20 hours)
- **Phase 5:** Update all tests (15-18 hours)
- **Phase 6:** Final cleanup and documentation (4-6 hours)

---

## Installation & Deployment

### Quick Start - Docker Hub (Production Ready)

```bash
# Download docker-compose file
curl -O https://raw.githubusercontent.com/waynen12/ChatComplete/main/docker-compose.dockerhub.yml

# Set environment variables
export OPENAI_API_KEY="your_openai_key"
export ANTHROPIC_API_KEY="your_anthropic_key"
export GEMINI_API_KEY="your_gemini_key"

# Start services
docker-compose -f docker-compose.dockerhub.yml up -d

# Access at http://localhost:8080
```

**Image:** `waynen12/ai-knowledge-manager:latest` (~541MB)

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `OPENAI_API_KEY` | Optional | OpenAI API key for GPT models |
| `ANTHROPIC_API_KEY` | Optional | Anthropic API key for Claude models |
| `GEMINI_API_KEY` | Optional | Google API key for Gemini models |
| `OLLAMA_BASE_URL` | Optional | Ollama server URL (default: localhost:11434) |

### Database Configuration

**CRITICAL:** Database must be outside `/out` directory (GitHub Actions deletes it during deployment)

```
Correct Path: /opt/knowledge-api/data/knowledge.db
Wrong Path:   /opt/knowledge-api/out/knowledge.db  ❌
```

Both Knowledge.Api and Knowledge.Mcp must use the same database path.

### Test Machine Deployment

**Server:** 192.168.50.203
**Services:**
- Knowledge.Api: Port 7040, `/opt/knowledge-api/out`
- Knowledge.Mcp: Port 5001, `/opt/knowledge-mcp/out`
- Shared Database: `/opt/knowledge-api/data/knowledge.db`

**Health Checks:**
```bash
curl http://192.168.50.203:7040/api/health
curl http://192.168.50.203:5001/health
```

---

## Core Features

### 1. Knowledge Upload & Management
- Upload PDF, TXT, MD, DOCX files
- Automatic chunking (configurable size: 500-2000 tokens)
- Vector embedding generation (OpenAI or Ollama)
- Qdrant vector storage with metadata
- Delete/update knowledge bases

### 2. Multi-Provider Chat
- **OpenAI:** GPT-4, GPT-3.5-turbo
- **Anthropic:** Claude Sonnet 4.5, Claude Opus 4.5  
- **Google:** Gemini 1.5 Pro
- **Ollama:** Local models with auto-download

### 3. RAG (Retrieval-Augmented Generation)
- Semantic search across knowledge bases
- Configurable relevance threshold (default: 0.3)
- Context injection into LLM prompts
- Cross-knowledge search (search multiple bases)

### 4. Conversation Management
- Save/load conversation history
- SQLite persistence
- Export conversations
- Token usage tracking

### 5. Analytics Dashboard
- Model usage statistics
- Token consumption tracking
- Response time metrics
- Provider comparison
- Cost analysis

### 6. Ollama Model Management
- List installed models
- Download models with progress tracking
- Model metadata (size, family, parameters)
- Auto-sync with Ollama server
- Model recommendations based on usage

---

## MCP Integration

### Overview
Model Context Protocol (MCP) integration provides 11 tools and 6 resources for external access to knowledge management capabilities.

### MCP Tools (11)

#### Cross-Knowledge Search (3 tools)
1. **search_knowledge** - Search single knowledge base
2. **search_all_knowledge** - Search across all bases
3. **compare_knowledge_bases** - Compare search results

#### Analytics (3 tools)
4. **get_knowledge_base_summary** - Comprehensive summary
5. **get_knowledge_base_health** - Health status + sync analysis
6. **get_storage_optimization** - Storage recommendations

#### Model Recommendations (3 tools)
7. **get_popular_models** - Most used models by period
8. **compare_models** - Side-by-side comparison
9. **get_model_performance** - Detailed performance analysis

#### System Health (2 tools)
10. **get_system_health** - Overall system status
11. **check_component_health** - Component-specific health

### MCP Resources (6)

1. **system/health** - Real-time system health
2. **knowledge/bases** - All knowledge bases list
3. **analytics/providers** - Provider analytics
4. **knowledge/{id}** - Specific knowledge base details
5. **analytics/provider/{name}** - Provider-specific analytics
6. **analytics/model/{name}** - Model-specific analytics

### MCP Modes

```bash
# STDIO Mode (Claude Desktop)
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj

# HTTP Mode (Copilot, Web Clients)
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --http
# Server starts on http://localhost:5001
```

### MCP OAuth 2.1 (Milestone #23)

**Status:**
- ✅ M2M Client Credentials Flow working (Auth0, JWT Bearer, scopes)
- ⚠️ PKCE Authorization Code Flow blocked (Auth0 returning JWE instead of JWT)

**Scopes:** `mcp:read`, `mcp:execute`, `mcp:admin`

---

## Agent Framework Migration

### Background
Migrating from Semantic Kernel (SK) to Microsoft Agent Framework (AF) to leverage unified agent capabilities and improved tooling.

### Migration Progress: 35% Complete

**Phase 1 (COMPLETE):** Partial SK cleanup
- ✅ Deleted KernelHelper.cs, EmbeddingsHelper.cs, Summarizer.cs

**Phase 2 (COMPLETE):** Streaming support
- ✅ AskStreamingAsync() - 130 lines
- ✅ AskWithAgentStreamingAsync() - 180+ lines
- ✅ GetContextInstructionsAsync() helper - 68 lines

**Phase 3 (COMPLETE):** Deprecate Semantic Kernel
- ✅ Deleted ChatComplete.cs (1,290 lines)
- ✅ Deleted KernelFactory.cs (~200 lines)
- ✅ Deleted 4 SK plugin files
- ✅ Refactored chat services to AF-only
- ✅ Removed UseAgentFramework feature flag
- ✅ Fixed usage tracking foreign key constraints

**Phase 4 (READY):** Refactor health checkers, TextChunker, Qdrant (8-10 hours)

**Phase 5 (TODO):** Update all tests (15-18 hours)

**Phase 6 (TODO):** Final cleanup and documentation (4-6 hours)

### Key Architecture Changes

#### Before (Semantic Kernel)
```csharp
var kernel = kernelFactory.CreateKernel(provider);
var chatHistory = new ChatHistory();
var result = await kernel.InvokePromptAsync(prompt, arguments);
```

#### After (Agent Framework)
```csharp
var chatClient = agentFactory.CreateAgent(provider);
var agent = new ChatClientAgent(chatClient);
await foreach (var update in agent.RunStreamingAsync(prompt)) {
    yield return update.Text;
}
```

### Critical Blockers (Resolved)
- ✅ Foreign key constraint in usage tracking (conversationId → NULL)
- ✅ Streaming support implementation
- ✅ SK dependency removal from chat services

---

## Development Guidelines

### Critical Workflow Rules

**1. ALWAYS COMMIT CHANGES AFTER SUCCESSFUL BUILD**
```bash
# 1. Verify build succeeds
dotnet build

# 2. IMMEDIATELY commit changes
git add -A
git commit -m "Descriptive message"

# 3. Use descriptive commit messages
```

**2. NO HARDCODED VALUES**
- Add parameters to `appsettings.json`
- Use constants file as last resort
- Never hardcode API keys, URLs, or configuration

**3. NO CODE DUPLICATION**
- Extract repeated patterns into reusable classes
- Use inheritance/composition appropriately
- Create shared utilities

**4. CHECK BEFORE MODIFYING**
When touching any file, verify:
- ✅ Uses configuration/appsettings.json?
- ✅ No hardcoded values?
- ✅ No duplicated code blocks?

### Testing Requirements

**Test Coverage:**
- ALL features should have smoke tests in `tests/smoke/`
- Unit tests for new components in `KnowledgeManager.Tests` or `Knowledge.Mcp.Tests`
- Integration tests for workflows in `tests/integration/`
- ALL patterns documented in `MASTER_TEST_PLAN.md`

### Documentation Standards

**All new documentation files MUST go in `documentation/` directory**

**Exceptions (stay in root):**
- README.md (user-facing setup)
- CLAUDE.md (developer context)
- DOCKER_HUB_README.md (Docker Hub description)

### Docker Best Practices

**Database Path:**
- ✅ `/opt/knowledge-api/data/knowledge.db` (outside /out)
- ❌ `/opt/knowledge-api/out/knowledge.db` (deleted during deployments)

**Image Configuration:**
- Slim base images (bookworm-slim)
- TCP health checks (not curl)
- Non-root user (appuser:appgroup, UID/GID 1001)
- Container networking via service names (qdrant, ollama)

---

## API Reference

### Quick Reference - ChatRequestDto

```csharp
public class ChatRequestDto {
    public string? KnowledgeId { get; set; }
    public string Message { get; set; } = "";
    public double Temperature { get; set; } = -1;  // -1 = use default
    public bool StripMarkdown { get; set; } = false;
    public bool UseExtendedInstructions { get; set; }
    public string? ConversationId { get; set; }
    public AiProvider Provider { get; set; } = AiProvider.OpenAi;
}
```

### Example: Chat Request

```bash
curl -X POST http://localhost:7040/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "knowledgeId": "docs-api",
    "message": "How do I delete knowledge?",
    "provider": "Ollama"
  }'
```

### Example: Upload Knowledge

```bash
curl -X POST http://localhost:7040/api/knowledge \
  -F "file=@document.pdf" \
  -F "knowledgeId=my-docs" \
  -F "chunkSize=1000" \
  -F "chunkOverlap=200"
```

### Example: List Knowledge Bases

```bash
curl http://localhost:7040/api/knowledge
```

### Example: Search Knowledge

```bash
curl -X POST http://localhost:7040/api/knowledge/search \
  -H "Content-Type: application/json" \
  -d '{
    "knowledgeId": "docs-api",
    "query": "authentication",
    "limit": 5,
    "minRelevance": 0.4
  }'
```

---

## Key Documentation Files

| File | Description |
|------|-------------|
| `CLAUDE.md` | Project context and current status (source of truth) |
| `README.md` | User-facing installation and setup |
| `documentation/AGENT_FRAMEWORK_MIGRATION_PLAN.md` | SK → AF migration (40-60 hours) |
| `documentation/MASTER_TEST_PLAN.md` | Test coverage tracking |
| `documentation/DCR_IMPLEMENTATION_SPEC.md` | OAuth DCR spec (42 hours, on hold) |
| `documentation/FAILFAST_CI_IMPLEMENTATION.md` | CI/CD fail-fast documentation |
| `documentation/SUDOERS_SERVICE_USER_GUIDE.md` | Systemd/sudoers configuration |
| `documentation/PLAYWRIGHT_MCP_README.md` | Playwright testing (9.5/10 rating) |

---

## Recent Changes

### December 22, 2025 - Phase 3 Complete
- Completed Phase 3 of Agent Framework migration
- Deleted 1,290+ lines of Semantic Kernel code
- Refactored chat services to AF-only architecture
- Fixed foreign key constraint errors in usage tracking
- Cleaned up 42 obsolete documentation files
- Build succeeded with 0 errors

### Previous Milestones
- ✅ MCP Integration (11 tools, 6 resources)
- ✅ Ollama model management with progress tracking
- ✅ Analytics dashboard with usage tracking
- ✅ Docker containerization and Docker Hub deployment
- ✅ Multi-provider LLM support

---

## Support & Contact

**Repository:** https://github.com/waynen12/ChatComplete
**Docker Hub:** https://hub.docker.com/r/waynen12/ai-knowledge-manager
**Issues:** https://github.com/waynen12/ChatComplete/issues

---

*This documentation is automatically generated from project files and maintained by Claude Code.*

