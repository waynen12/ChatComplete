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
22	MCP Integration (Model Context Protocol servers and clients)	✅ COMPLETE
22.5	MCP Server Deployment (test machine CI/CD integration)	✅ COMPLETE
23	MCP OAuth 2.1 Authentication (secure remote MCP access)	🔄 IN PROGRESS (M2M working, PKCE blocked)
24	MCP Client Development (separate repo, STDIO + HTTP transports)	🔄 IN PROGRESS
25	UI Modernization (accessibility, performance, mobile, color scheme)	🔄 IN PROGRESS (Copilot-driven)

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
- **Multi-stage build**: Node.js frontend → .NET backend → Debian Slim runtime
- **Container networking**: Services communicate via container names (qdrant, ollama)  
- **Data persistence**: Named volumes for application data, Qdrant storage, and Ollama models
- **Health monitoring**: TCP socket checks (no curl dependency)
- **Security**: Non-root user, minimal attack surface, no unnecessary packages

### Docker Image Specifications
- **Final Size**: ~541MB (optimized)
- **Base Image**: mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim
- **Build Stages**: 3 (frontend → backend → runtime)
- **Health Check**: TCP socket (port 7040) - no curl dependency
- **User**: Non-root (appuser:appgroup, UID/GID 1001)
- **Volumes**: /app/data (persistent storage)
- **Port**: 7040 (HTTP)

### Optimization Summary (November 2025)
**Image Size Reduction:**
- Before: ~630MB (standard aspnet:8.0 + curl)
- After: ~541MB (bookworm-slim, no curl)
- Savings: ~90MB (14% reduction)

**Build Improvements:**
- Enhanced .dockerignore (reduced build context)
- Excluded test projects, documentation, .github workflows
- Excluded SQLite databases and temporary files
- Faster builds from smaller context transfer

**Runtime Improvements:**
- No curl dependency (TCP health checks)
- Single-layer user/directory setup
- Minimal runtime packages
- Debian Slim base (vs full Debian)


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

## Recent Progress (November 2025)

### Database Path Fix ✅ COMPLETED (2025-11-14)

**Critical Production Bug Fixed**:
- 🔧 **Database path moved outside `/out` directory**: Changed from `/opt/knowledge-api/out/data/knowledge.db` to `/opt/knowledge-api/data/knowledge.db`
- 🔧 **Survives deployments**: Database no longer deleted during GitHub Actions deployments (which wipe `/out` folder)
- 🔧 **Configuration consistency**: Both Knowledge.Api and Knowledge.Mcp appsettings.json now explicitly configured with same path
- 🔧 **No data loss**: Chat history, knowledge metadata, and configuration now persist across deployments

**Root Cause**:
- Knowledge.Api had `"DatabasePath": null` in appsettings.json
- Fallback logic used `AppContext.BaseDirectory + "data"` → `/opt/knowledge-api/out/data/knowledge.db`
- GitHub Actions workflow runs `rm -rf /opt/knowledge-api/out/*` before each deployment
- Result: Database deleted on every deployment 💥

**Fix Applied**:
- Updated [Knowledge.Api/appsettings.json](Knowledge.Api/appsettings.json#L66): `"DatabasePath": "/opt/knowledge-api/data/knowledge.db"`
- Updated [Knowledge.Mcp/appsettings.json](Knowledge.Mcp/appsettings.json#L18): Same path (was pointing to `/out` directory)
- Both services now explicitly configured (no fallback logic in production)

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

## MCP Integration (Milestone #22) ✅ COMPLETED (October 2025)

**Model Context Protocol Server** - Expose ChatComplete agent functionality via standardized MCP protocol.

### Implementation Complete
- **STDIO Transport**: ✅ Fully functional with Claude Desktop
- **Streamable HTTP Transport**: ✅ Functional with GitHub Copilot and web clients
- **11 MCP Tools**: ✅ Cross-knowledge search, analytics, model recommendations, system health
- **6 MCP Resources**: ✅ Static and parameterized resources for system state exposure
- **Dual Transport Mode**: ✅ `--http` flag for HTTP mode, default STDIO for Claude Desktop

### Technical Implementation

**Project Structure:**
```
Knowledge.Mcp/
├── Tools/                          # 11 MCP tools
│   ├── CrossKnowledgeSearchMcpTool.cs
│   ├── KnowledgeAnalyticsMcpTool.cs
│   ├── ModelRecommendationMcpTool.cs
│   └── SystemHealthMcpTool.cs
├── Resources/                      # 6 MCP resources
│   ├── KnowledgeResourceMethods.cs
│   ├── KnowledgeResourceProvider.cs
│   └── ResourceUriParser.cs
├── Configuration/
│   └── McpServerSettings.cs        # Comprehensive configuration
├── Program.cs                       # Dual-mode server
└── appsettings.json                # All settings configurable
```

**Configuration-Based Architecture:**
- **No hardcoded values**: All ports, URLs, CORS settings in appsettings.json
- **HTTP Transport Settings**: Port, host, session timeout configurable
- **CORS Security**: Configurable allowed origins, credentials, exposed headers
- **OAuth 2.1 Ready**: Configuration structure in place for Milestone #23

**Key Configuration ([appsettings.json](Knowledge.Mcp/appsettings.json#L112-L134)):**
```json
"HttpTransport": {
  "Port": 5001,
  "Host": "localhost",
  "SessionTimeoutMinutes": 30,
  "Cors": {
    "Enabled": true,
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173",
      "https://copilot.github.com"
    ],
    "AllowCredentials": true
  },
  "OAuth": null
}
```

**Critical Fixes:**
1. **CORS Middleware** - Required for browser-based MCP clients (Copilot, MCP Inspector)
2. **Middleware Ordering** - `UseCors()` before `UseRouting()` for preflight requests
3. **Web SDK** - Changed from `Microsoft.NET.Sdk` to `Microsoft.NET.Sdk.Web`
4. **Configuration-Based URLs** - Eliminates hardcoded ports and hosts

### MCP Tools Available

**Cross-Knowledge Search (3 tools):**
- `search_knowledge` - Search specific knowledge base
- `search_all_knowledge` - Search across all knowledge bases
- `compare_knowledge_bases` - Compare content between knowledge bases

**Knowledge Analytics (3 tools):**
- `get_knowledge_base_summary` - Get summary of all knowledge bases
- `get_knowledge_base_health` - Health check for knowledge bases
- `get_storage_optimization` - Storage optimization recommendations

**Model Recommendations (3 tools):**
- `get_popular_models` - Most popular AI models based on usage
- `compare_models` - Compare multiple models side-by-side
- `get_model_performance` - Performance analysis for specific model

**System Health (2 tools):**
- `get_system_health` - Overall system health status
- `check_component_health` - Health of specific component (Qdrant, SQLite, Ollama)

### MCP Resources Available

**Static Resources:**
1. `resource://system/health` - System health status
2. `resource://knowledge/bases` - List of all knowledge bases
3. `resource://analytics/providers` - Provider connection status

**Parameterized Resources:**
1. `resource://knowledge/{knowledgeId}` - Specific knowledge base details
2. `resource://analytics/provider/{providerName}` - Provider-specific analytics
3. `resource://analytics/model/{modelName}` - Model-specific performance

### Usage

**STDIO Mode (Claude Desktop):**
```bash
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj
```

**HTTP Mode (Copilot, Web Clients):**
```bash
dotnet run --project Knowledge.Mcp/Knowledge.Mcp.csproj -- --http
# Server starts on http://localhost:5001 (configurable)
```

**Connect from GitHub Copilot:**
```json
{
  "mcpServers": {
    "knowledge-manager": {
      "url": "http://localhost:5001"
    }
  }
}
```

### Security Considerations

**Development vs Production:**
- Development: `AllowAnyOrigin: false` with specific origins listed
- Production: Must restrict CORS to trusted domains only
- OAuth 2.1: Planned for Milestone #23

**Current Security:**
- ✅ CORS restricted to specific origins by default
- ✅ Credentials support for future OAuth flows
- ✅ Session management via MCP-Session-Id headers
- ⚠️ No authentication yet - suitable for local/trusted networks only

### Known Limitations

1. **SDK Version**: Using `ModelContextProtocol.AspNetCore` v0.4.0-preview.2
   - SessionTimeout configuration not available in this version
   - Will be enabled when SDK is updated

2. **No Authentication**: OAuth 2.1 implementation is Milestone #23
   - Current deployment suitable for localhost/trusted networks only
   - Should not be exposed to public internet without authentication

### Success Metrics
- ✅ Dual transport (STDIO + HTTP) working
- ✅ All 11 tools functional and tested
- ✅ All 6 resources accessible
- ✅ Configuration-based (no hardcoded values)
- ✅ CORS properly configured for web clients
- ✅ Successfully tested with GitHub Copilot
- ✅ Successfully tested with Claude Desktop
- ✅ Exception handling and logging in place

## MCP OAuth 2.1 Authentication (Milestone #23) 🔄 IN PROGRESS (November 2025)

**Secure Remote MCP Access** - OAuth 2.1 authentication for MCP HTTP transport.

### Implementation Status

✅ **Completed (M2M Client Credentials Flow):**
- JWT Bearer authentication with Auth0 integration
- Scope-based authorization (mcp:read, mcp:execute, mcp:admin)
- Token validation via JWKS endpoint
- RFC 9728 OAuth metadata endpoint
- WWW-Authenticate header for 401 responses
- Configurable OAuth (enable/disable via appsettings.json)
- Successfully tested with Postman and MCP Inspector

⚠️ **Blocked (PKCE Authorization Code Flow):**
- Auth0 returning encrypted JWE tokens instead of signed JWT tokens
- Token header shows `{"alg":"dir","enc":"A256GCM"}` (JWE encryption)
- JWT Bearer middleware expects `RS256` with `kid` (standard JWT)
- Blocks GitHub Copilot PKCE integration

🔄 **Next Steps:**
- Test with alternative OAuth providers (Azure AD, AWS Cognito, Keycloak)
- Determine if JWE encryption is Auth0-specific or provider-agnostic issue
- May require JWE decryption support or provider-specific configuration

### Requirements Research Complete

Based on MCP Authorization Specification (March 2025):

**OAuth 2.1 Compliance:**
- MCP servers act as OAuth 2.1 **resource servers only** ✅ (Recommended by Okta Director of Identity Standards)
- Validation of tokens issued by external authorization servers (Auth0, Azure AD, AWS Cognito)
- **PKCE required** on client side (handled by authorization server)
- Token validation via JWKS (JSON Web Key Set) endpoint
- Scope-based authorization (mcp:read, mcp:execute)
- WWW-Authenticate header for 401 responses

**Expert Validation:**
- **Source:** Aaron Parecki (Director of Identity Standards at Okta)
- **Video:** https://www.youtube.com/watch?v=mYKMwZcGynw
- **Recommendation:** Simplified resource server pattern, not full OAuth AS

**Configuration Structure Ready:**
Configuration classes already in place ([McpServerSettings.cs](Knowledge.Mcp/Configuration/McpServerSettings.cs#L277-L334)):
```csharp
public class OAuthSettings
{
    public bool Enabled { get; set; } = false;
    public string? AuthorizationServerUrl { get; set; }
    public string? ResourceIndicator { get; set; }
    public bool RequirePkce { get; set; } = true;  // MUST be true per spec
    public TokenValidationSettings TokenValidation { get; set; }
    public string[] RequiredScopes { get; set; } = { "mcp:read", "mcp:execute" };
}
```

### Implementation Tasks (Simplified - Resource Server Only)

**Week 1: JWT Bearer Authentication Setup**
- [ ] Add Microsoft.AspNetCore.Authentication.JwtBearer NuGet package
- [ ] Configure JWT Bearer authentication middleware
- [ ] Set up Auth0 as primary identity provider
- [ ] Configure JWKS endpoint for signature validation
- [ ] Implement WWW-Authenticate header responses (401)

**Week 2: Scope-Based Authorization**
- [ ] Define authorization policies for MCP scopes
  - `mcp:read` - Read-only operations (resources, health checks)
  - `mcp:execute` - Tool execution (search, analytics)
- [ ] Apply [Authorize] attributes to MCP endpoints
- [ ] Test scope enforcement on all tools/resources
- [ ] Add logging for authorization failures

**Week 3: Testing & Documentation**
- [ ] Set up free Auth0 tenant for testing
- [ ] Configure API and scopes in Auth0
- [ ] Test with real Auth0 tokens
- [ ] Create client integration guide (how to get tokens)
- [ ] Document token format and claims
- [ ] Performance testing with token validation

**Implementation Effort:** 2-3 weeks (vs 3-6 months for full OAuth AS)

### Security Architecture (Resource Server Pattern)

**Simplified Token Flow:**
```
┌─────────────┐                      ┌─────────────────────┐
│             │  1. Get token        │   Auth0/Azure AD    │
│  MCP Client │─────────────────────>│  (with PKCE)        │
│             │                      └─────────────────────┘
└─────────────┘                               │
      │                                       │ 2. Token
      │                                       │    (JWT)
      │<──────────────────────────────────────┘
      │
      │ 3. Authorization: Bearer <token>
      v
┌─────────────────────┐
│   Knowledge.Mcp     │
│  (Resource Server)  │
│  - Validate token   │
│  - Check scopes     │
│  - Serve tools      │
└─────────────────────┘
```

**Responsibilities:**
- **Authorization Server (Auth0):** User authentication, token issuance, MFA, SSO
- **MCP Server (Knowledge.Mcp):** Token validation, scope checking, business logic
- **MCP Client:** OAuth flow, token refresh, include token in requests

**Scope Design:**
- `mcp:read` - Access to read-only resources and health checks
- `mcp:execute` - Execute tools (search, analytics, recommendations)
- `mcp:admin` - Administrative operations (future: manage knowledge bases)

**Token Validation:**
1. Extract token from `Authorization: Bearer <token>` header
2. Validate signature using JWKS from Auth0
3. Validate issuer, audience, expiration
4. Extract and verify scopes
5. Allow/deny request based on scopes

### Testing Strategy
- Unit tests for token validation logic
- Integration tests with test authorization server
- End-to-end tests with real OAuth providers
- Security testing (invalid tokens, expired tokens, scope violations)

### Documentation Needed
- OAuth provider setup guides (Auth0, Azure AD, AWS Cognito)
- Client configuration examples
- Scope permission matrix
- Troubleshooting guide for common OAuth issues

## MCP Server Deployment to Test Machine ✅ COMPLETED (November 2025)

**Production Deployment** - MCP server deployed to test machine (192.168.50.203 - Mint Linux) via GitHub Actions CI/CD pipeline.

### Deployment Architecture

**Services on Test Machine:**
- **Knowledge.Api** (Port 7040) - Main API server at `/opt/knowledge-api/out`
- **Knowledge.Mcp** (Port 5001) - MCP server at `/opt/knowledge-mcp/out`
- **Qdrant** (Port 6333) - Vector database
- **Shared Database**: `/opt/knowledge-api/data/knowledge.db` (SQLite - OUTSIDE `/out` directory)

Both services share the same SQLite database for configuration, chat history, and knowledge metadata.

**CRITICAL**: The database path is `/opt/knowledge-api/data/knowledge.db` (NOT `/opt/knowledge-api/out/data/knowledge.db`). This ensures the database survives GitHub Actions deployments which delete the `/out` directory.

### GitHub Actions Workflow Integration

**Workflow File:** `.github/workflows/deploy-self.yml`

**MCP Deployment Steps:**
1. Clean MCP publish folder: `rm -rf /opt/knowledge-mcp/out/*`
2. Publish MCP server: `dotnet publish -c Release -r linux-x64 --self-contained true -o /opt/knowledge-mcp/out`
3. Restart MCP service: `sudo -n systemctl restart knowledge-mcp.service`
4. Verify health: `curl http://localhost:5001/health`

**Key Requirements:**
- Self-hosted GitHub Actions runner on test machine
- Passwordless sudo access for systemctl commands
- Systemd service file configured correctly

### Systemd Service Configuration

**File:** `/etc/systemd/system/knowledge-mcp.service`

```ini
[Unit]
Description=Knowledge Manager MCP Server
After=network.target knowledge-api.service
Requires=knowledge-api.service

[Service]
Type=simple  # CRITICAL: Must be 'simple', not 'notify'
User=chatapi
WorkingDirectory=/opt/knowledge-mcp/out
ExecStart=/opt/knowledge-mcp/out/Knowledge.Mcp --http
Restart=on-failure
RestartSec=10
KillMode=process

Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="ASPNETCORE_URLS=http://+:5001"
Environment="DOTNET_RUNNING_IN_CONTAINER=false"

[Install]
WantedBy=multi-user.target
```

**Critical Configuration Notes:**
- `Type=simple` is REQUIRED (not `Type=notify`) - app doesn't send systemd notifications
- `After=knowledge-api.service` ensures API starts first
- `Requires=knowledge-api.service` creates dependency relationship
- User `chatapi` must have permissions to shared database

### Sudoers Configuration

**File:** `/etc/sudoers.d/chatapi` (mode 0440)

```bash
# Allow GitHub Actions runner to manage services without password
chatapi ALL=(root) NOPASSWD: /usr/bin/systemctl restart knowledge-api.service, \
                              /usr/bin/systemctl --no-pager status knowledge-api.service, \
                              /usr/bin/systemctl restart knowledge-mcp.service, \
                              /usr/bin/systemctl --no-pager status knowledge-mcp.service, \
                              /usr/bin/journalctl
```

**Critical Requirements:**
- File permissions MUST be `0440`: `sudo chmod 0440 /etc/sudoers.d/chatapi`
- Commands must match EXACTLY including `--no-pager` flag
- Use absolute paths: `/usr/bin/systemctl`, not `systemctl`
- Validate syntax: `sudo visudo -c`

**Common Pitfalls:**
- ❌ Wrong permissions (755, 644) - file silently ignored
- ❌ Missing `--no-pager` in sudoers but used in workflow - permission denied
- ❌ `Type=notify` in service file - causes infinite timeout on restart
- ❌ Relative command paths - won't match absolute paths in sudoers

### MCP Configuration for Test Machine

**File:** `Knowledge.Mcp/appsettings.json`

**Critical Settings:**
```json
{
  "ChatCompleteSettings": {
    "DatabasePath": "/opt/knowledge-api/out/data/knowledge.db"  // Shared with API
  },
  "McpServerSettings": {
    "HttpTransport": {
      "Port": 5001,
      "Host": "localhost",
      "Cors": {
        "AllowedOrigins": [
          "http://localhost:3000",
          "http://localhost:5173",
          "http://192.168.50.203",        // Test machine IP
          "http://192.168.50.203:7040",   // Test machine API port
          "https://copilot.github.com"
        ]
      }
    }
  }
}
```

**Database Path Configuration:**
- **Production Path**: `/opt/knowledge-api/data/knowledge.db` (OUTSIDE `/out` directory)
- **Critical**: Database MUST be outside `/opt/knowledge-api/out/` to survive deployments
- **Both Services**: API and MCP configured to use same database path in appsettings.json
- **Why `/data`**: The `/out` folder is deleted during GitHub Actions deployments
- **Shared Database**: Both services access same SQLite file for configuration, chat history, and knowledge metadata

### Health Endpoints

**MCP Health Check:**
```bash
curl http://localhost:5001/health
# Returns: {"status":"healthy","service":"knowledge-mcp","timestamp":"...","version":"1.0.0"}
```

**API Health Check:**
```bash
curl http://localhost:7040/api/health
# Returns: {"status":"healthy","timestamp":"...","checks":{...}}
```

### Deployment Verification

**Manual Verification Steps:**
1. Check API service: `sudo systemctl status knowledge-api.service`
2. Check MCP service: `sudo systemctl status knowledge-mcp.service`
3. Test API health: `curl http://localhost:7040/api/health`
4. Test MCP health: `curl http://localhost:5001/health`
5. Check logs: `sudo journalctl -u knowledge-mcp.service -n 50`
6. Verify database: `ls -la /opt/knowledge-api/out/data/knowledge.db`

**GitHub Actions Verification:**
1. Trigger workflow via push to main or manual dispatch
2. Monitor workflow: GitHub Actions → "Build & Deploy – self-hosted"
3. Verify "Restart MCP service" step succeeds
4. Verify "Verify MCP health" step passes

### Troubleshooting Guide

**Issue: "sudo: a password is required"**
- **Cause:** Sudoers configuration incorrect or file permissions wrong
- **Fix:**
  1. Check file permissions: `ls -la /etc/sudoers.d/chatapi` (must be `-r--r-----`)
  2. Fix permissions: `sudo chmod 0440 /etc/sudoers.d/chatapi`
  3. Validate syntax: `sudo visudo -c`
  4. Test manually: `sudo -u chatapi sudo -n systemctl restart knowledge-mcp.service`

**Issue: "Job for knowledge-mcp.service failed because a timeout was exceeded"**
- **Cause:** Service configured with `Type=notify` but app doesn't send notification
- **Fix:** Change `Type=notify` to `Type=simple` in service file
- **Verify:** Service should start immediately without hanging

**Issue: "Orphaned collections" warning in MCP logs**
- **Cause:** MCP server pointing to wrong database (collections in Qdrant but not in SQLite)
- **Fix:** Update `DatabasePath` in MCP appsettings.json to match API database path
- **Verify:** Both services should show same knowledge bases

**Issue: MCP health check fails**
- **Cause:** Service not running or port conflict
- **Fix:**
  1. Check service: `sudo systemctl status knowledge-mcp.service`
  2. Check port: `sudo ss -tlnp | grep 5001`
  3. Check logs: `sudo journalctl -u knowledge-mcp.service -n 50`

### Success Metrics
- ✅ MCP server deploys automatically via GitHub Actions
- ✅ Both API and MCP services running on test machine
- ✅ Health endpoints responding correctly
- ✅ Shared database working (no orphaned collections)
- ✅ CORS configured for external clients
- ✅ Systemd service auto-restarts on failure

### Documentation Created
- ✅ `documentation/SUDOERS_SERVICE_USER_GUIDE.md` - Complete sudoers configuration guide
- ✅ Workflow inline documentation with prerequisite steps
- ✅ Service file configuration examples
- ✅ Troubleshooting guide with common issues

### Next Steps
1. ✅ **Deployment complete** - MCP server operational on test machine
2. 🔄 **Monitor production usage** - Verify stability and performance
3. 🛠️ **Phase 2:** OAuth 2.1 authentication (Milestone #23)
4. 🛠️ **Phase 3:** Docker integration (when needed)

## MCP Client Development (Milestone #24) 🔄 IN PROGRESS

**Separate Repository** - Standalone C# MCP client for connecting to Knowledge Manager server.

### Repository Information

**Location:** `/home/wayne/repos/McpClient` (separate git repository)

**Purpose:**
- Connect to Knowledge Manager MCP server (and other MCP servers)
- Provide CLI interface for MCP tool execution
- Demonstrate MCP client implementation patterns
- Enable integration testing of Knowledge Manager

### Current Status

**Phase 1: STDIO Transport** (In Progress)
- ✅ Repository created and initialized
- ✅ Solution and project setup (.NET 9)
- ✅ NuGet packages installed:
  - ModelContextProtocol v0.4.0-preview.2
  - Microsoft.Extensions.AI v9.9.1
  - Microsoft.Extensions.AI.OpenAI v9.9.1-preview.1
  - Microsoft.Extensions.AI.Ollama v9.7.0-preview.1
- ✅ Basic STDIO connection working
- ✅ Tool discovery functional
- ✅ OpenAI integration (function calling)
- ⏳ Clean architecture refactoring
- ⏳ Service layer implementation
- ⏳ Interactive CLI with Spectre.Console
- ⏳ Unit and integration tests

**Phase 2: HTTP Transport** (TODO)
- [ ] HTTP client transport
- [ ] SSE (Server-Sent Events) handling
- [ ] Reconnection logic
- [ ] Session management
- [ ] Synchronized with Knowledge.Mcp HTTP transport

### Architecture

```
McpClient Repository (/home/wayne/repos/McpClient)
├── McpClient_cs/
│   ├── Transports/
│   │   ├── ITransport.cs           (Interface)
│   │   ├── StdioTransport.cs       (Phase 1)
│   │   └── HttpSseTransport.cs     (Phase 2)
│   ├── Services/
│   │   ├── McpClientService.cs     (Main service)
│   │   ├── DiscoveryService.cs     (Tools/resources)
│   │   └── ExecutionService.cs     (Tool execution)
│   ├── Models/
│   │   ├── ClientConfiguration.cs
│   │   └── McpServerConfig.cs
│   └── Program.cs                  (CLI entry point)
└── tests/
    ├── McpClient.Tests/            (Unit tests)
    └── McpClient.IntegrationTests/ (Integration tests)
```

### Why Separate Repository?

**Benefits:**
1. ✅ **Clean Separation** - Different lifecycle from Knowledge Manager
2. ✅ **Reusability** - Can connect to any MCP server, not just Knowledge Manager
3. ✅ **Independent Versioning** - Client and server can evolve independently
4. ✅ **Distribution** - Standalone CLI tool, potential NuGet package
5. ✅ **Testing** - Validate Knowledge Manager MCP server implementation

**Coordination with Knowledge Manager:**
- Phase 1 (STDIO): No coordination needed - server already supports STDIO
- Phase 2 (HTTP): Requires Knowledge Manager HTTP transport (Milestone #22 - completed)

### Configuration Example

**appsettings.json:**
```json
{
  "McpServers": {
    "knowledge-manager-stdio": {
      "transport": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/home/wayne/repos/ChatComplete/Knowledge.Mcp/Knowledge.Mcp.csproj"
      ]
    },
    "knowledge-manager-http": {
      "transport": "http",
      "baseUrl": "http://localhost:5001"
    }
  }
}
```

### Testing Strategy

**Unit Tests:**
- Transport implementations
- Service layer logic
- Message serialization/deserialization

**Integration Tests:**
- Connect to Knowledge Manager STDIO server
- Discover all 11 tools
- Execute tools with various parameters
- Read all 6 resources
- Verify response formats

**Cross-Transport Tests:**
- Same functionality via STDIO and HTTP
- Performance comparison
- Failure scenarios and reconnection

### Implementation Timeline

| Phase | Focus | Duration | Status |
|-------|-------|----------|--------|
| **Phase 1** | STDIO Transport | 3 weeks | 🔄 In Progress |
| Week 1 | Transport interface, STDIO impl | 5 days | ⏳ |
| Week 2 | Service layer, CLI | 5 days | 🛠️ TODO |
| Week 3 | Testing, documentation | 5 days | 🛠️ TODO |
| **Phase 2** | HTTP Transport | 3 weeks | 🛠️ TODO |
| Week 4 | HTTP transport client-side | 5 days | 🛠️ TODO |
| Week 5 | SSE handling, reconnection | 5 days | 🛠️ TODO |
| Week 6 | Integration tests, polish | 5 days | 🛠️ TODO |

### Success Criteria

**Phase 1 Complete:**
- [ ] Clean architecture with ITransport interface
- [ ] StdioTransport fully functional
- [ ] Can connect to Knowledge Manager via STDIO
- [ ] Discovers all 11 tools correctly
- [ ] Executes tools successfully
- [ ] Discovers all 6 resources
- [ ] Reads resource content
- [ ] Interactive CLI with Spectre.Console
- [ ] Unit tests >80% coverage
- [ ] Integration tests passing

**Phase 2 Complete:**
- [ ] HttpSseTransport implemented
- [ ] SSE streaming working
- [ ] Reconnection logic robust
- [ ] Both transports configurable
- [ ] Performance acceptable (<100ms overhead)
- [ ] Cross-transport tests passing
- [ ] Documentation complete

### Keeping in Sync with Knowledge Manager

**Server Changes Requiring Client Updates:**
1. **New tools added** - Update tool discovery tests
2. **New resources added** - Update resource reading tests
3. **Protocol version changes** - Update initialization handshake
4. **HTTP transport changes** - Update HttpSseTransport (Phase 2)
5. **Authentication added** - Update to support OAuth 2.1 (Milestone #23)

**Process:**
1. Knowledge Manager MCP server changes are made first
2. MCP Client repository is updated to match
3. Integration tests verify compatibility
4. Both repositories tagged with matching versions

### Related Documentation

**In ChatComplete Repository:**
- [MCP_CLIENT_SEPARATE_REPO.md](documentation/MCP_CLIENT_SEPARATE_REPO.md) - Architecture overview
- [MCP_CLIENT_IMPLEMENTATION_PLAN.md](documentation/MCP_CLIENT_IMPLEMENTATION_PLAN.md) - Detailed design
- [MCP_CLIENT_TESTING_GUIDE.md](documentation/MCP_CLIENT_TESTING_GUIDE.md) - Testing strategy

**In McpClient Repository:**
- `IMPLEMENTATION_PLAN.md` - Week-by-week development plan
- `MILESTONE_REVIEW.md` - Progress tracking
- `PHASE_CLARIFICATION.md` - Phase-specific details

All documentation for this project is located in ./documentation

---

## UI Modernization (Milestone #25) 🔄 IN PROGRESS (November 2025)

**GitHub Copilot Cloud Development** - Comprehensive UI/UX improvements driven by GitHub Copilot in separate branch.

### Branch Strategy

**UI Development Branch:** `copilot/review-ui-in-webclient`
- Dedicated branch for UI improvements
- GitHub Copilot Cloud performs autonomous UI development
- Human developer focuses on backend work
- Branches merge after review and testing

### Copilot Guidance

**Instructions File:** `.github/copilot-instructions.md` (660 lines)
- Comprehensive guide for GitHub Copilot Cloud
- Clear boundaries: UI work only, no backend changes
- Week-by-week implementation plan
- Testing requirements and PR standards
- Design system and accessibility guidelines

### Current UI State (Baseline)

**Metrics:**
- Bundle size: 1.15 MB (target: < 500 KB)
- ESLint errors: 0 (clean ✅)
- Overall UI score: 7/10
- Accessibility score: 4/10 (needs work)
- Performance score: 6/10 (needs optimization)

**Review Documentation:**
- `documentation/UI_REVIEW.md` (647 lines) - Detailed analysis
- `documentation/UI_IMPROVEMENTS_ACTION_PLAN.md` (1,015 lines) - Implementation guide
- `documentation/REVIEW_SUMMARY.md` (225 lines) - Executive summary

### Implementation Priorities

**Week 1: Critical Fixes (🔴 High Priority)**

**1. Color Scheme Overhaul (Do First):**
- Replace all blue-shaded colors with minimalist light palette
- New primary: `oklch(0.86 0.01 262.85)` (light lavender-gray)
- Darken button text for contrast
- Ensure WCAG 2.1 AA compliance (4.5:1 contrast)
- Test in both light and dark modes

**2. Accessibility Basics:**
- Add ARIA labels to all interactive elements
- Implement keyboard navigation
- Add skip navigation link
- Fix form label associations

**3. Performance Optimization:**
- Implement code splitting (React.lazy)
- Reduce bundle size by 50%
- Add React.memo to expensive renders
- Optimize asset loading

**4. Mobile Responsiveness:**
- Implement hamburger menu
- Fix responsive breakpoints
- Ensure 44px minimum touch targets

**Week 2: High Priority UX**
- Message actions (copy, edit, delete)
- Conversation search/filter
- Inline form validation
- Helpful empty states

**Week 3: Medium Priority Polish**
- Conversation management sidebar
- Keyboard shortcuts (Cmd+K search, etc.)
- Settings organization

**Week 4: Testing & Polish**
- Cross-browser testing
- Accessibility audit
- Performance verification
- Documentation updates

### Key UI Changes

**Target Improvements:**
1. **Bundle Size:** 1.15 MB → < 500 KB (reduce by 57%)
2. **Accessibility:** 4/10 → 10/10 (WCAG 2.1 AA compliance)
3. **Performance:** 6/10 → 9/10 (Lighthouse score 90+)
4. **Mobile UX:** Incomplete → Full feature parity
5. **Color Scheme:** Blue-heavy → Minimalist light palette

**New Components to Create:**
- `<MessageActions />` - Copy/edit/delete buttons
- `<ConversationList />` - Searchable sidebar
- `<EmptyState />` - Reusable empty states
- `<MobileMenu />` - Hamburger navigation
- `<SkipNav />` - Accessibility skip link
- `<KeyboardShortcuts />` - Shortcuts modal

### Testing Requirements

**Before Merging to Main:**
- ✅ Desktop browsers (Chrome, Firefox, Safari)
- ✅ Mobile responsive (375px, 768px, 1024px)
- ✅ Dark mode compatibility
- ✅ Keyboard navigation
- ✅ Screen reader testing
- ✅ Bundle size < 500 KB
- ✅ Build succeeds without errors
- ✅ 0 ESLint errors maintained

### Success Criteria
- [ ] Bundle size reduced to < 500 KB
- [ ] WCAG 2.1 AA compliance achieved
- [ ] Lighthouse score 90+ across all metrics
- [ ] Full mobile feature parity
- [ ] Minimalist color scheme implemented
- [ ] All 35+ improvements from action plan completed
- [ ] Zero regression in existing functionality

### Collaboration Model

**Human Developer (Backend):**
- Focus on backend features and APIs
- Review UI PRs from Copilot
- Merge approved changes to main

**GitHub Copilot Cloud (UI):**
- Autonomous UI development in `copilot/review-ui-in-webclient`
- Follow `.github/copilot-instructions.md` guidance
- Submit PRs with screenshots and testing checklists
- Document backend dependencies if needed

### Documentation Created
- ✅ `.github/copilot-instructions.md` - Copilot guidance (660 lines)
- ✅ `documentation/UI_REVIEW.md` - Current state analysis
- ✅ `documentation/UI_IMPROVEMENTS_ACTION_PLAN.md` - Implementation guide
- ✅ `documentation/REVIEW_SUMMARY.md` - Executive summary

### Timeline
- **Estimated Effort:** 76 hours (~4 weeks with Copilot)
- **Week 1 Focus:** Color scheme, accessibility, performance
- **Week 2 Focus:** Chat UX, forms, empty states
- **Week 3 Focus:** Conversation management, shortcuts
- **Week 4 Focus:** Testing, audit, polish

### Next Steps
1. 🔄 **Copilot begins Week 1 work** - Color scheme overhaul
2. 🔄 **Human monitors progress** - Review PRs as submitted
3. 🛠️ **Iterative improvements** - Address feedback and refine
4. 🛠️ **Final merge** - After all testing passes

---

## Current System Status (November 2025)

### Production Deployments
- ✅ **Test Machine (192.168.50.203):** API + MCP server running
- ✅ **Docker Hub:** `waynen12/ai-knowledge-manager:latest` available
- ✅ **Local Dev:** React UI connected to test machine API

### Active Work Streams
1. **UI Modernization** - GitHub Copilot Cloud (branch: `copilot/review-ui-in-webclient`)
2. **MCP OAuth 2.1** - Blocked on PKCE/JWE issue
3. **MCP Client** - STDIO transport complete, HTTP transport TODO

### Critical Paths Resolved
- ✅ Sudoers configuration for CI/CD deployment
- ✅ Systemd service configuration (Type=simple)
- ✅ Database path sharing between API and MCP
- ✅ GitHub Actions MCP deployment integration

### Known Issues
1. **OAuth PKCE Flow** - Auth0 returns JWE tokens (blocked)
2. **MCP Client Phase 2** - HTTP transport not yet implemented
3. **UI Bundle Size** - 1.15 MB, needs optimization

### Bootstrap Instructions for New Chat
1. Read this CLAUDE.md file completely
2. Check current branch: `git status`
3. Review open PRs on GitHub (especially from `copilot/review-ui-in-webclient`)
4. Check test machine status: `curl http://192.168.50.203:7040/api/health`
5. Review recent commits: `git log --oneline -20`
6. Check for active work: Review TODO items in code or issues