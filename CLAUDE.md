You are joining AI Knowledge Manager, an open-source stack for uploading technical docs, vector-indexing them in MongoDB Atlas, and chatting over that knowledge with multiple LLM providers (OpenAI, Gemini, Ollama, Anthropic).

You must remain critical but friendly at all times. Do not always accept the first solution to a problem. Always check if there are alternatives. Do not be afraid to ask questions or seek clarification from others when needed.

## ⚠️ CRITICAL WORKFLOW REMINDER ⚠️

**ALWAYS COMMIT CHANGES AFTER SUCCESSFUL BUILD**

After completing any implementation work:
1. ✅ Verify the build succeeds with `dotnet build`
2. ✅ **IMMEDIATELY commit changes** to preserve the working state
3. ✅ Use descriptive commit messages that explain what was implemented
4. Try to avoid replying with "You're absolutely right!"

**Why this is critical:**
- Git rollbacks can lose hours of work if changes aren't committed
- Successful builds represent stable checkpoints that should be preserved
- Database migrations, API changes, and feature implementations must be saved immediately

**Never assume you can recreate complex implementations from memory - commit early, commit often!**

**DO NOT USE HARDCODED VALUES!!!!!**
Hardcoded values make the code difficult to adapt to changing requirements and can lead to bugs or unexpected behavior. 
Always follow these rules instead of using hard coded values
1. Add a parameter to the config file (appsettings.json)
2. If you can't add a parameter to a config, use a constants file as a last resort. 

Tech stack:

Layer	Tech
Backend	ASP.NET 8 Minimal APIs · Serilog · MongoDB Atlas (vector & metadata) · Semantic Kernel 1.6 (Mongo Vector Store)
Frontend	React + Vite · shadcn/ui (Radix + Tailwind) · Framer-motion
CI/Deploy	Self-hosted GitHub Actions on Mint Linux → dotnet publish → /opt/knowledge-api/out → knowledge-api.service

Project Goals
Developer knowledge base – drag-and-drop any docs / code / markdown; the system chunks, embeds, and stores them for semantic search.

Conversational assistant – chat endpoint streams answers that cite relevant chunks.

Provider flexibility – switch model per-request: OpenAi | Google | Anthropic | Ollama.

Persistent conversations – Mongo conversations collection keeps history for context windows.

Open-source learning – keep code aligned with the latest Semantic Kernel releases to track best practices.

Current Milestones (✅ done, 🔄 in-progress, 🛠️ todo)
#	Milestone	Status
1	Core API skeleton, Serilog, CORS	✅
2	Swagger + Python code sample	✅
3	System prompt & temperature in appsettings.json	✅
4	Multi-file upload /api/knowledge (chunk/validate)	✅
5	Delete knowledge (collection + index)	✅
6	UI polish: spinner, Radix AlertDialog confirm	✅
7	CI self-hosted deploy workflow	✅
8	Chat-history persistence (ConversationId)	✅
9	Unit / integration tests (incl. kernel-selection)	✅
10	Guard super-large code fences	✅
11	UI MD toggle & README note	✅
12	Build+Deploy job split (CI refactor)	🛠️
13	Refactor AtlasIndexManager for DI	✅
14	Upgrade SK → 1.6 & migrate to Mongo Vector Store	✅
15	Provider menu + Anthropic fallback	✅
16	README & Swagger examples for 4 providers	✅
17	Qdrant Vector Store parallel implementation	✅
18	Local Configuration Database (SQLite config, zero env vars)	✅
19	Docker Containerization (one-command deployment)	✅
20	Agent Implementation (Semantic Kernel plugins, cross-knowledge search)	✅ COMPLETE
21	Ollama Model Management (UI + API + downloads)	✅ VERIFIED
22	MCP Integration (Model Context Protocol servers and clients)	🔄 IN PROGRESS

Latest Sanity Checklist (quick smoke test)
Step	Expectation	Tip
Upload doc	201 Created, new Mongo collection	Check Atlas logs for $vectorSearch
Index create	Search index READY	Log level Debug for AtlasIndexManager
Chunk upsert	_id, vector[], Text in docs	Compass: preview similarity scores
Vector search	Results ordered by score	Try nonsense query (<0.6) → empty
Chat with knowledgeId	Context lines appear in prompt	Temporarily log contextBlock
Provider switch	Replies OK on OpenAI, Gemini, Ollama; Anthropic falls back to non-stream	Anthropic streaming not yet in SK
Conversation resume	Same CID continues context even after refresh	CID is kept in sessionStorage

DTOs
csharp
Copy
Edit
// ChatRequestDto   (client → /api/chat)
public class ChatRequestDto {
    public string? KnowledgeId { get; set; }
    public string  Message { get; set; } = "";
    public double  Temperature { get; set; } = -1;      // -1 = server default
    public bool    StripMarkdown { get; set; } = false;
    public bool    UseExtendedInstructions { get; set; }
    public string? ConversationId { get; set; }         // null → new convo
    public AiProvider Provider { get; set; } = AiProvider.OpenAi;
}

// ChatResponseDto  (server → client)
public class ChatResponseDto {
    public string ConversationId { get; set; } = "";
    public string Reply { get; set; } = "";
}
Usage Example (curl)
bash
Copy
Edit
curl -X POST http://localhost:7040/api/chat \
  -H "Content-Type: application/json" \
  -d '{
        "knowledgeId": "docs-api",
        "message": "How do I delete knowledge?",
        "temperature": 0.7,
        "useExtendedInstructions": true,
        "provider": "Ollama",
        "conversationId": null
      }'

## 🐳 Docker Deployment (Milestone #19)

Complete containerization with multi-stage builds and Docker Hub distribution.

### Quick Start (Production Ready) ✅ VERIFIED WORKING
```bash
# Download and start full stack (AI Knowledge Manager + Qdrant + Ollama)
curl -O https://raw.githubusercontent.com/waynen12/ChatComplete/main/docker-compose.dockerhub.yml
docker-compose -f docker-compose.dockerhub.yml up -d

# Access at http://localhost:8080
# ✅ Model downloads work (gemma3 verified)
# ✅ Complete RAG workflow functional
```

### Available Configurations
- `docker-compose.dockerhub.yml` - Production: Docker Hub images only (recommended)
- `docker-compose.full-stack.yml` - Development: Local build + all services
- `docker-compose.yml` - Standard: Local build + Qdrant only
- `docker-compose.debug.yml` - Debug: Extended logging and health checks

### Architecture
- **Multi-stage build**: Node.js frontend → .NET backend → Alpine runtime
- **Container networking**: Services communicate via container names (qdrant, ollama)  
- **Data persistence**: Named volumes for application data, Qdrant storage, and Ollama models
- **Health monitoring**: TCP-based health checks for all services
- **Security**: Non-root user, minimal attack surface

### Fixed Issues
**Latest (2025-08-17) - VERIFIED WORKING:**
- ✅ **Ollama Docker networking**: Fixed container-to-container communication using service names
- ✅ **Configuration path fix**: Corrected OllamaApiService to read from ChatCompleteSettings:OllamaBaseUrl
- ✅ **Model management**: Ollama model downloads now work in Docker deployments (gemma3 verified)
- ✅ **Real-time progress**: Server-Sent Events working correctly in containerized environment
- ✅ **End-to-end RAG**: Complete workflow verified - upload documents, download Ollama models, chat with local AI

**Previous (2025-01-08):**
- ✅ **Alpine → Debian base image**: Fixed missing `ld-linux-x86-64.so.2` library
- ✅ **Qdrant connection**: Fixed hardcoded localhost → container service names
- ✅ **Port configuration**: REST API (6333) vs gRPC (6334) port clarification
- ✅ **Health checks**: Replaced curl dependency with TCP connection tests
- ✅ **User creation**: Fixed Alpine → Debian user/group commands

### Environment Variables
```bash
# Required for external LLM providers
OPENAI_API_KEY=your_key
ANTHROPIC_API_KEY=your_key  
GEMINI_API_KEY=your_key

# Vector store configuration (handled automatically)
VectorStore__Provider=Qdrant
VectorStore__Qdrant__Host=qdrant
VectorStore__Qdrant__Port=6333
```

### Docker Hub Distribution
- **Image**: `waynen12/ai-knowledge-manager:latest`
- **Multi-platform**: AMD64 + ARM64 support
- **CI/CD**: GitHub Actions auto-build on push/tag
- **Size optimized**: Multi-stage builds, Debian slim base

## 🗃️ SQLite Database Implementation (Milestone #18)

**Zero-Dependency Architecture** - Complete elimination of MongoDB requirement for Qdrant deployments.

### Phase 1 Implementation ✅ COMPLETED
- **SqliteDbContext**: Auto-creating database with full schema initialization
- **Encrypted Configuration**: AES-256 encrypted storage for API keys and sensitive settings  
- **Chat History Persistence**: Complete conversation storage replacing MongoDB
- **Knowledge Metadata**: Document tracking, chunk counts, processing status
- **Smart Configuration**: Dynamic settings with encrypted secure storage

### Database Features
- **Configurable Path**: `"DatabasePath": "/custom/path/database.db"` in appsettings.json
- **Smart Defaults**: 
  - Container: `/app/data/knowledge.db` (volume mounts)
  - Development: `{AppDirectory}/data/knowledge.db` (reliable)
- **Auto-Initialization**: Schema creation, default settings, directory creation
- **Encryption**: Sensitive data protected with PBKDF2 key derivation
- **WAL Mode**: Better concurrency for multi-threaded access

### Zero-Configuration Startup
1. Database file created automatically at startup
2. All tables and indexes initialized  
3. Default model configurations populated
4. API keys stored encrypted when provided
5. Chat conversations persisted locally

**Result**: Complete containerized deployment with Qdrant + SQLite - no external database dependencies required.

## Recent Progress (September 2025)

### Analytics Dashboard Fixes ✅ COMPLETED (2025-09-10)

**Critical Usage Tracking Resolution**:
- 🔧 **Fixed usage metrics recording**: Resolved foreign key constraint failure preventing usage data from being stored in UsageMetrics table
- 🔧 **Conversation ID synchronization**: Fixed conversation ID passing between `SqliteChatService` and `ChatComplete.AskAsync()` by injecting ID into ChatHistory system message
- 🔧 **Database integration working**: Usage tracking now properly records conversations, tokens, response times, and success rates for all providers

**Ollama Provider Analytics Integration**:
- 🔧 **Added OllamaProviderApiService**: Created comprehensive provider service that integrates with local usage tracking database
- 🔧 **Real usage data**: Ollama analytics now show actual request counts, token usage, and model-specific breakdowns from database
- 🔧 **Connection detection**: Ollama now appears as connected provider in analytics dashboard with proper health checks
- 🔧 **Service registration**: Properly registered Ollama provider service in DI container with HttpClient configuration

**Frontend Timeout Implementation**:  
- 🔧 **Request timeout handling**: Added 30-second timeouts with AbortController across all analytics fetch requests
- 🔧 **Exponential backoff retry**: Smart retry logic with 3 attempts, avoiding retries for client errors (4xx)
- 🔧 **CORS fixes**: Resolved SignalR WebSocket connection issues by adding missing headers (x-signalr-user-agent, x-requested-with)
- 🔧 **Error resilience**: Graceful degradation when API requests fail or timeout

**Technical Implementation**:
- **KnowledgeEngine/Chat/SqliteChatService.cs**: Added conversation ID injection to ChatHistory for usage tracking integration
- **Knowledge.Analytics/Services/OllamaProviderApiService.cs**: New service integrating IUsageTrackingService with provider analytics
- **Knowledge.Analytics/Extensions/ServiceCollectionExtensions.cs**: Registered Ollama provider service with HttpClient
- **webclient/src/pages/AnalyticsPage.tsx**: Comprehensive timeout handling and retry logic for all provider requests
- **Knowledge.Api/appsettings.json**: Fixed CORS configuration for SignalR WebSocket connections

**Developer Experience**:
- ✅ Analytics dashboard now shows real conversation and token counts
- ✅ All providers (OpenAI, Anthropic, Google AI, Ollama) properly detected and connected
- ✅ Knowledge base conversation counts accurately reflect actual usage
- ✅ Robust error handling prevents dashboard crashes on network issues
- ✅ Real-time updates via SignalR with proper connection management

**Database Verification**:
```sql
-- Usage metrics now properly recorded
SELECT COUNT(*) FROM UsageMetrics; -- Returns actual usage count, not 0
SELECT Provider, COUNT(*) FROM UsageMetrics GROUP BY Provider; -- Shows per-provider usage
```

### Ollama Model Management Enhancements ✅ COMPLETED (August 2025)

**Progress Bar Fix (2025-08-23)**:
- 🔧 **Fixed progress aggregation**: Resolved issue where Ollama model downloads showed 0% then jumped to 100%
- 🔧 **Multi-layer download tracking**: Implemented proper progress aggregation across Ollama's multiple download layers/digests
- 🔧 **Real-time progress updates**: Progress now updates every 1% for smooth user experience
- 🔧 **Enhanced progress parsing**: Added digest field tracking and improved JSON parsing from Ollama API

**Model Synchronization (2025-08-23)**:
- 🔧 **Auto-sync pre-existing models**: `/api/ollama/models/details` endpoint now automatically discovers and syncs models that were installed outside the Knowledge Manager
- 🔧 **Intelligent model comparison**: Parallel fetching from both Ollama API and SQLite with efficient HashSet comparison
- 🔧 **Status differentiation**: Pre-existing models marked as "Installed" vs downloaded models marked as "Downloaded"
- 🔧 **Error resilience**: Individual model sync failures don't break the entire sync process

**Technical Implementation**:
- **OllamaApiService.cs**: Enhanced `PullModelProgressInternal()` with layer-aware progress aggregation
- **OllamaEndpoints.cs**: Updated `/models/details` endpoint with sync logic and parallel data fetching
- **Docker deployment**: All fixes verified working in containerized environment

**Developer Experience**:
- ✅ Pre-existing Ollama models now visible in Knowledge Manager UI
- ✅ Smooth progress bars during model downloads (no more 0% → 100% jumps)
- ✅ Automatic model discovery eliminates manual database sync needs
- ✅ Complete end-to-end RAG workflow: upload documents → download/sync models → chat with local AI

## Agent Implementation Planning (Milestone #20)

### **Learning Objectives**
- **Agent Architecture**: Hands-on experience with Semantic Kernel plugin development
- **Tool Calling**: LLM orchestration with automatic function calling
- **MCP Preparation**: Design interfaces compatible with Model Context Protocol
- **Cross-Knowledge Search**: Intelligent search across multiple knowledge bases

### **Phase 1: Foundation (Week 1)**
**Semantic Kernel Plugin Integration:**
- Extend existing `ChatComplete.cs` with agent capabilities
- Add optional `UseAgent` parameter to chat requests
- Implement `CrossKnowledgeSearchPlugin` for multi-knowledge-base search
- Maintain backward compatibility with existing chat functionality

**Technical Implementation:**
- `AskWithAgentAsync()` method with `ToolCallBehavior.AutoInvokeKernelFunctions`
- Plugin registration via DI container (`AddScoped<CrossKnowledgeSearchPlugin>()`)
- Enhanced execution settings for multi-provider tool calling (OpenAI, Gemini, Anthropic, Ollama)

### **Phase 2: Integration & Testing (Week 2)**
**API Enhancement:**
- Update chat endpoints to route agent requests
- Tool execution tracking and response enrichment
- Performance testing (target < 2x traditional response time)
- Error handling with graceful degradation

**Test Scenarios:**
1. Cross-knowledge synthesis: "Compare React and .NET deployment approaches"
2. Gap detection: "How do I set up SSL?" (when no SSL docs exist)
3. Multi-knowledge search: "Find Docker deployment information"

### **Future Expansion**
**Additional Plugin Types:**
- **Code Analysis Plugin**: Static analysis, security scanning, quality metrics
- **Documentation Generator**: Extract API docs, create knowledge entries
- **Configuration Helper**: Docker compose analysis, environment management
- **Knowledge Management**: Organization, deduplication, gap analysis

### **MCP Integration Roadmap**
**MCP-Compatible Design:**
- Tool interfaces align with MCP specifications (`name`, `description`, `inputSchema`)
- Future MCP client implementation for external server connections  
- Knowledge Manager MCP server to expose search tools to other applications
- Multi-server orchestration and tool discovery

### **Success Criteria**
- [ ] Agent mode toggleable via API parameter
- [ ] Cross-knowledge search operational across all knowledge bases
- [ ] Tool executions tracked and response quality improved
- [ ] Backward compatibility maintained for existing clients
- [ ] Foundation established for advanced agent features and MCP integration

This agent implementation leverages the existing Semantic Kernel infrastructure while building toward MCP ecosystem integration, providing both immediate utility and future extensibility.

All documentation for this project is located in ./documentation