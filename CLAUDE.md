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
23	MCP OAuth 2.1 Authentication (secure remote MCP access)	🛠️ TODO
24	MCP Client Development (separate repo, STDIO + HTTP transports)	🔄 IN PROGRESS

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

## MCP OAuth 2.1 Authentication (Milestone #23) 🛠️ TODO

**Secure Remote MCP Access** - Implement OAuth 2.1 authentication per MCP Authorization Specification.

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