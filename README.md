# AI Knowledge Manager

[![Docker Pulls](https://img.shields.io/docker/pulls/waynen12/ai-knowledge-manager.svg)](https://hub.docker.com/r/waynen12/ai-knowledge-manager)
[![Docker Image Size](https://img.shields.io/docker/image-size/waynen12/ai-knowledge-manager)](https://hub.docker.com/r/waynen12/ai-knowledge-manager)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB)](https://reactjs.org/)

Open-source RAG (Retrieval-Augmented Generation) system for uploading technical documents, vector-indexing them in Qdrant, and chatting over that knowledge with multiple LLM providers (OpenAI, Anthropic, Google Gemini, Ollama).

## 🚀 Quick Start (2 Minutes)

Get a complete AI knowledge management system running with one command:

```bash
# Download Docker Compose file
curl -O https://raw.githubusercontent.com/waynen12/ChatComplete/main/docker-compose.dockerhub.yml

# Start the full stack (Knowledge Manager + Qdrant + Ollama)
docker-compose -f docker-compose.dockerhub.yml up -d

# Access the application
open http://localhost:8080
```

**What you get:**
- ✅ Full-stack RAG system with React UI
- ✅ Qdrant vector database (for embeddings)
- ✅ Ollama (local LLM - no API key needed)
- ✅ Persistent storage (survives restarts)

## 📋 Table of Contents

- [Features](#-features)
- [Architecture](#-architecture)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Usage Examples](#-usage-examples)
- [API Reference](#-api-reference)
- [Development](#-development)
- [Deployment](#-deployment)
- [Troubleshooting](#-troubleshooting)

## ⚡ Features

### Core Capabilities
- **Drag & Drop Upload**: Upload PDFs, DOCX, Markdown, TXT files via web UI
- **Smart Chunking**: Intelligent document splitting with configurable overlap
- **Vector Search**: Semantic similarity search using Qdrant
- **Multi-LLM Support**: Switch between providers per request
  - OpenAI (GPT-4, GPT-4o, GPT-5)
  - Anthropic (Claude Sonnet 4)
  - Google (Gemini 2.5 Flash)
  - Ollama (Local models - gemma3, llama3, etc.)

### Advanced Features
- **Agent Mode**: Cross-knowledge base search with tool calling
- **Persistent Conversations**: Chat history with context retention
- **Analytics Dashboard**: Usage tracking, token counts, provider metrics
- **Ollama Management**: Download and manage local models via UI
- **RESTful API**: Full programmatic access
- **MCP Integration**: Model Context Protocol server (STDIO + HTTP)

### Production Ready
- **SQLite Storage**: Zero-dependency local database
- **Docker Deploy**: One-command containerized deployment
- **Health Monitoring**: Built-in health checks for all services
- **Security**: Non-root containers, API key management

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│              React Frontend (Vite)                      │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  │
│  │   Chat   │ │Knowledge │ │Analytics │ │ Models   │  │
│  │   Page   │ │  Upload  │ │Dashboard │ │Management│  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘  │
└──────────────────────┬──────────────────────────────────┘
                       │ HTTP/REST
┌──────────────────────▼──────────────────────────────────┐
│         ASP.NET 8 Backend (Minimal APIs)                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  Knowledge   │  │     Chat     │  │   Analytics  │  │
│  │  Management  │  │   Complete   │  │   Services   │  │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  │
│         │                  │                  │          │
│  ┌──────▼──────────────────▼──────────────────▼───────┐ │
│  │   Microsoft.Extensions.AI (Agent Framework)        │ │
│  │   AgentFactory + 4 AF Plugins (11 tools)           │ │
│  └─────────────────────┬────────────────────────────── ┘ │
└────────────────────────┼─────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        │                │                │
┌───────▼────────┐ ┌────▼──────┐ ┌───────▼────────┐
│  Qdrant (gRPC) │ │  SQLite   │ │  LLM Providers │
│  Vector Store  │ │  Database │ │  (OpenAI, etc) │
│  Port: 6334    │ │  (Local)  │ │                │
└────────────────┘ └───────────┘ └────────────────┘
```

**Tech Stack:**
- **Frontend**: React 18 + Vite + shadcn/ui (Radix + Tailwind)
- **Backend**: ASP.NET 8 Minimal APIs + Serilog
- **AI Framework**: Microsoft.Extensions.AI (Agent Framework) - *migrated from Semantic Kernel 1.6*
- **Vector DB**: Qdrant (gRPC for performance)
- **Storage**: SQLite (configuration, chat history, analytics)
- **LLM Integration**: OpenAI, Anthropic, Google AI, Ollama

### Agent Framework Architecture

The system uses **Microsoft.Extensions.AI (Agent Framework)** for AI orchestration:

**Core Components:**
- **ChatCompleteAF.cs**: Multi-provider chat with tool calling (950+ lines, 54% smaller than SK version)
- **AgentFactory.cs**: Creates `ChatClient` instances for all 4 providers
- **AgentToolRegistration.cs**: Reflection-based tool discovery and registration
- **4 AF Plugins**: 11 total functions (search, analytics, recommendations, health)

**Key Features:**
- ✅ Direct provider SDK usage (no abstraction overhead)
- ✅ Tool calling support for all providers
- ✅ Streaming chat completion
- ✅ Conversation tracking and analytics
- ✅ Semantic chunking with embeddings (SemanticChunker.NET)

**Migration Details:** See [AF_MIGRATION_STATUS.md](documentation/AF_MIGRATION_STATUS.md) for complete migration documentation (41.5 hours, completed Jan 2026)

## 📦 Installation

### Option 1: Docker (Recommended)

**Prerequisites:**
- Docker 20.10+ and Docker Compose 1.29+
- 4GB RAM minimum (8GB recommended for Ollama)
- 2GB disk space (base image ~541MB + data)

**Full Stack Deployment:**
```bash
# Download compose file
curl -O https://raw.githubusercontent.com/waynen12/ChatComplete/main/docker-compose.dockerhub.yml

# (Optional) Create .env file for API keys
cat > .env << EOF
OPENAI_API_KEY=your_key_here
ANTHROPIC_API_KEY=your_key_here
GEMINI_API_KEY=your_key_here
EOF

# Start all services
docker-compose -f docker-compose.dockerhub.yml up -d

# Check service health
docker-compose -f docker-compose.dockerhub.yml ps
```

**Note**: You can also set API keys inline:
```bash
OPENAI_API_KEY=xxx docker-compose -f docker-compose.dockerhub.yml up -d
```

**Minimal Deployment (Qdrant only):**
```bash
# Use standard compose file (builds from source)
docker-compose up -d
```

### Option 2: Local Development

**Prerequisites:**
- .NET 8 SDK
- Node.js 20+
- Qdrant running locally (port 6334)
- API keys for LLM providers

**Backend Setup:**
```bash
# Clone repository
git clone https://github.com/waynen12/ChatComplete.git
cd ChatComplete

# Set API keys
export OPENAI_API_KEY=your_key
export ANTHROPIC_API_KEY=your_key
export GEMINI_API_KEY=your_key

# Build and run API
dotnet build Knowledge.Api/Knowledge.Api.csproj
dotnet run --project Knowledge.Api/Knowledge.Api.csproj
# API starts on http://localhost:7040
```

**Frontend Setup:**
```bash
# In separate terminal
cd webclient
npm install
npm run dev
# UI starts on http://localhost:5173
```

**Start Qdrant:**
```bash
docker run -d -p 6333:6333 -p 6334:6334 \
  -v qdrant_storage:/qdrant/storage \
  qdrant/qdrant:latest
```

## ⚙️ Configuration

### Environment Variables

#### Required for External LLMs
```bash
OPENAI_API_KEY=sk-...          # OpenAI API key
ANTHROPIC_API_KEY=sk-ant-...   # Anthropic API key
GEMINI_API_KEY=...             # Google Gemini API key
```

#### Container Configuration
```bash
# Vector store (defaults to Qdrant)
ChatCompleteSettings__VectorStore__Provider=Qdrant
ChatCompleteSettings__VectorStore__Qdrant__Host=qdrant  # or localhost
ChatCompleteSettings__VectorStore__Qdrant__Port=6334    # gRPC port

# Ollama integration
ChatCompleteSettings__OllamaBaseUrl=http://ollama:11434  # or http://localhost:11434

# Database path (inside container)
ChatCompleteSettings__DatabasePath=/app/data/knowledge.db
```

### API Keys Setup

1. **OpenAI**: [Get key from OpenAI Platform](https://platform.openai.com/api-keys)
2. **Anthropic**: [Get key from Anthropic Console](https://console.anthropic.com/)
3. **Google Gemini**: [Get key from Google AI Studio](https://makersuite.google.com/app/apikey)
4. **Ollama**: No API key needed (runs locally)

### Configuration Files

**Production**: `Knowledge.Api/appsettings.json`
```json
{
  "ChatCompleteSettings": {
    "DatabasePath": "/opt/knowledge-api/data/knowledge.db",  // OUTSIDE /out for persistence
    "VectorStore": {
      "Provider": "Qdrant",
      "Qdrant": {
        "Host": "localhost",
        "Port": 6334,  // gRPC port for performance
        "UseHttps": false
      }
    }
  }
}
```

## 📚 Usage Examples

### Web UI

1. **Upload Documents**:
   - Navigate to http://localhost:8080
   - Click "Knowledge" → "Upload"
   - Drag PDF/DOCX/MD files or browse
   - Enter Knowledge ID (e.g., "my-docs")
   - Click "Upload"

2. **Chat with Knowledge**:
   - Navigate to "Chat"
   - Select knowledge base from dropdown
   - Choose LLM provider (OpenAI, Anthropic, Gemini, Ollama)
   - Ask questions about your documents

3. **Manage Ollama Models**:
   - Navigate to "Models"
   - Browse available models
   - Click "Download" to pull models locally
   - Progress tracked in real-time

### REST API

#### Upload Documents
```bash
curl -X POST http://localhost:8080/api/knowledge \
  -F "files=@document.pdf" \
  -F "files=@guide.docx" \
  -F "knowledgeId=my-docs"
```

#### Chat with Knowledge Base
```bash
curl -X POST http://localhost:8080/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "knowledgeId": "my-docs",
    "message": "Summarize the main points",
    "provider": "OpenAi",
    "temperature": 0.7,
    "conversationId": null
  }'
```

#### Agent Mode (Cross-Knowledge Search)
```bash
curl -X POST http://localhost:8080/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Compare deployment approaches across all docs",
    "provider": "OpenAi",
    "useAgent": true
  }'
```

#### List Knowledge Bases
```bash
curl http://localhost:8080/api/knowledge
```

#### Delete Knowledge Base
```bash
curl -X DELETE http://localhost:8080/api/knowledge/my-docs
```

## 🔌 API Reference

### Endpoints

#### Knowledge Management
- `POST /api/knowledge` - Upload documents
- `GET /api/knowledge` - List all knowledge bases
- `GET /api/knowledge/{id}` - Get knowledge base details
- `DELETE /api/knowledge/{id}` - Delete knowledge base

#### Chat
- `POST /api/chat` - Send chat message (streaming response)

#### Ollama
- `GET /api/ollama/models` - List available models
- `GET /api/ollama/models/details` - Detailed model info
- `POST /api/ollama/models/pull` - Download model
- `DELETE /api/ollama/models/{name}` - Delete model

#### Analytics
- `GET /api/analytics/providers` - Provider status and metrics
- `GET /api/analytics/knowledge/{id}` - Knowledge base analytics

#### Health
- `GET /api/ping` - Health check
- `GET /api/health` - Detailed health status

### Request/Response Examples

See [Swagger documentation](http://localhost:8080/swagger) when running.

## 🛠️ Development

### Project Structure
```
ChatComplete/
├── Knowledge.Api/           # Main API project
├── KnowledgeEngine/         # Core RAG logic
├── Knowledge.Contracts/     # DTOs and interfaces
├── Knowledge.Analytics/     # Analytics services
├── Knowledge.Data/          # Data access layer (SQLite)
├── Knowledge.Entities/      # Domain models
├── Knowledge.Mcp/           # MCP server implementation
├── webclient/               # React frontend
│   ├── src/
│   │   ├── pages/          # Page components
│   │   ├── components/     # Reusable components
│   │   └── routes.tsx      # Route configuration
│   └── package.json
├── Dockerfile               # Multi-stage build
├── docker-compose.yml       # Local build + Qdrant
└── docker-compose.dockerhub.yml  # Production (Docker Hub)
```

### Build from Source

```bash
# Build backend
dotnet build

# Build frontend
cd webclient && npm run build

# Run tests
dotnet test

# Build Docker image
docker build -t ai-knowledge-manager:local .
```

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test KnowledgeManager.Tests/

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsDirectory=./coverage
```

## 🚀 Deployment

### Docker Hub (Production)

**Automated CI/CD:**
- Every push to `main` triggers GitHub Actions
- Builds multi-platform image (AMD64 + ARM64)
- Pushes to `waynen12/ai-knowledge-manager:latest`

**Manual Deployment:**
```bash
# Pull latest image
docker pull waynen12/ai-knowledge-manager:latest

# Run with Qdrant
docker-compose -f docker-compose.dockerhub.yml up -d
```

### Self-Hosted (Test Machine)

Current production deployment on `192.168.50.203`:

```bash
# API: http://192.168.50.203:7040
# MCP: http://192.168.50.203:5001

# Deployed via GitHub Actions self-hosted runner
# Database: /opt/knowledge-api/data/knowledge.db (outside /out directory)
# Services: knowledge-api.service, knowledge-mcp.service
```

### Environment-Specific Configuration

**Development:**
- Database: `{AppDirectory}/data/knowledge.db`
- Qdrant: `localhost:6334`
- Frontend: `localhost:5173`

**Docker Container:**
- Database: `/app/data/knowledge.db` (volume mounted)
- Qdrant: Service name `qdrant:6334`
- Frontend: Served from `/app/wwwroot`

**Production (Self-Hosted):**
- Database: `/opt/knowledge-api/data/knowledge.db` (persistent)
- Qdrant: `localhost:6334`
- Frontend: Built into API `/wwwroot`

## 🔍 Troubleshooting

### Common Issues

#### 1. Container Won't Start
```bash
# Check logs
docker-compose -f docker-compose.dockerhub.yml logs ai-knowledge-manager

# Common causes:
# - Missing API keys (only needed for external LLMs)
# - Port conflicts (8080, 6333, 6334, 11434)
# - Insufficient memory (need 4GB+)
```

#### 2. Qdrant Connection Errors
```bash
# Verify Qdrant is healthy
docker-compose -f docker-compose.dockerhub.yml ps qdrant

# Check Qdrant logs
docker-compose -f docker-compose.dockerhub.yml logs qdrant

# Test connection
curl http://localhost:6333/health
```

#### 3. Ollama Models Not Downloading
```bash
# Check Ollama service
docker exec -it ollama ollama list

# Download model manually
docker exec -it ollama ollama pull gemma3:12b

# Verify network connectivity
docker-compose -f docker-compose.dockerhub.yml exec ai-knowledge-manager curl http://ollama:11434/api/version
```

#### 4. Database Issues
```bash
# Check database file permissions
docker-compose -f docker-compose.dockerhub.yml exec ai-knowledge-manager ls -la /app/data/

# Verify database location (MUST be outside /out directory for persistence)
# Correct: /app/data/knowledge.db (Docker) or /opt/knowledge-api/data/knowledge.db (self-hosted)
# Wrong: /app/out/data/knowledge.db or /opt/knowledge-api/out/data/knowledge.db
```

#### 5. Port Confusion (Qdrant)
- **Port 6333**: HTTP REST API (for health checks, manual queries)
- **Port 6334**: gRPC API (used by application for performance)
- Application uses **6334** for data operations
- Health checks use **6333** (REST endpoint)

### Health Checks

```bash
# Main application
curl http://localhost:8080/api/ping
# Expected: {"status":"healthy"}

# Qdrant
curl http://localhost:6333/health
# Expected: {"status":"ok"}

# Ollama
curl http://localhost:11434/api/version
# Expected: {"version":"x.y.z"}
```

### Performance Optimization

**For Large Documents:**
```json
{
  "ChatCompleteSettings": {
    "ChunkCharacterLimit": 8192,  // Increase chunk size
    "MaxCodeFenceSize": 20480,    // Handle larger code blocks
    "ChunkOverlap": 80            // More context overlap
  }
}
```

**For Memory Constraints:**
- Use Ollama with smaller models (gemma3:7b instead of 12b)
- Reduce `ChatMaxTurns` to limit context window
- Lower vector dimensions in embedding model

## 📊 Supported File Types

- **Documents**: `.pdf`, `.docx`, `.txt`, `.md`
- **Code**: All programming languages with syntax detection
- **Structured**: `.json`, `.xml`, `.csv` (future)

## 🔒 Security

- ✅ Runs as non-root user (`appuser:appgroup`)
- ✅ API keys managed via environment variables (not committed)
- ✅ SQLite database encrypted for sensitive settings
- ✅ Container isolation with bridge networks
- ✅ CORS configured for specific origins only
- ✅ No hardcoded credentials in code

## 🤝 Contributing

Contributions welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

See [CLAUDE.md](CLAUDE.md) for development guidelines.

## 📝 License

Open source under the MIT License. See [LICENSE](LICENSE) file for details.

## 🔗 Links

- [GitHub Repository](https://github.com/waynen12/ChatComplete)
- [Docker Hub](https://hub.docker.com/r/waynen12/ai-knowledge-manager)
- [Issues & Bug Reports](https://github.com/waynen12/ChatComplete/issues)
- [Project Documentation](https://github.com/waynen12/ChatComplete/blob/main/CLAUDE.md)

## 📈 Project Status

See [CLAUDE.md](CLAUDE.md) for detailed milestone tracking:

- ✅ Milestones #1-22: Core features, MCP integration, Docker deployment
- 🔄 Milestone #23: OAuth 2.1 authentication (in progress)
- 🔄 Milestone #24: MCP client development (in progress)
- 🔄 Milestone #25: UI modernization (in progress)

---

**Built with**: .NET 8, React 18, Semantic Kernel, Qdrant, and ❤️
