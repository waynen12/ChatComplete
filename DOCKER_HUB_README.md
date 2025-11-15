# AI Knowledge Manager

[![Docker Pulls](https://img.shields.io/docker/pulls/waynen12/ai-knowledge-manager.svg)](https://hub.docker.com/r/waynen12/ai-knowledge-manager)
[![Docker Image Size](https://img.shields.io/docker/image-size/waynen12/ai-knowledge-manager)](https://hub.docker.com/r/waynen12/ai-knowledge-manager)

Open-source RAG (Retrieval-Augmented Generation) system for uploading documents, vector-indexing them, and chatting over that knowledge with multiple LLM providers.

## 🚀 Quick Start

Get a complete AI knowledge management system running in under 2 minutes:

```bash
# Download the Docker Compose file
curl -O https://raw.githubusercontent.com/waynen12/ChatComplete/main/docker-compose.dockerhub.yml

# Start the full stack (Knowledge Manager + Qdrant + Ollama)
docker-compose -f docker-compose.dockerhub.yml up -d

# Access the application
open http://localhost:8080
```

## 🏗️ Architecture

- **Frontend**: React + Vite with shadcn/ui components
- **Backend**: ASP.NET 8 Minimal APIs with Semantic Kernel
- **Vector Database**: Qdrant for embeddings storage
- **LLM Support**: OpenAI, Anthropic, Google Gemini, Ollama

## ⚡ Features

- **Drag & Drop Upload**: Upload documents, PDFs, markdown files
- **Smart Chunking**: Intelligent document splitting and embedding
- **Multi-LLM Support**: Switch between OpenAI, Anthropic, Gemini, or Ollama per request
- **Persistent Conversations**: Chat history maintained across sessions
- **Vector Search**: Semantic similarity search through your knowledge base
- **RESTful API**: Full API access for integrations

## 🔧 Configuration

### Environment Variables

Create a `.env` file or set via command line:

```env
# Required for external LLM providers
OPENAI_API_KEY=your_openai_key
ANTHROPIC_API_KEY=your_anthropic_key
GEMINI_API_KEY=your_gemini_key

# Vector store configuration (pre-configured in compose)
ChatCompleteSettings__VectorStore__Provider=Qdrant
ChatCompleteSettings__VectorStore__Qdrant__Host=qdrant
ChatCompleteSettings__VectorStore__Qdrant__Port=6334  # gRPC port

# Ollama configuration (pre-configured in compose)
ChatCompleteSettings__OllamaBaseUrl=http://ollama:11434
```

**Note**: The `docker-compose.dockerhub.yml` file already includes these configurations. You only need to set the API keys.

### API Keys Setup

1. **OpenAI**: Get key from [OpenAI Platform](https://platform.openai.com/api-keys)
2. **Anthropic**: Get key from [Anthropic Console](https://console.anthropic.com/)
3. **Google Gemini**: Get key from [Google AI Studio](https://makersuite.google.com/app/apikey)
4. **Ollama**: No API key needed - runs locally

## 📚 Usage

### Upload Documents
```bash
curl -X POST http://localhost:8080/api/knowledge \
  -F "files=@document.pdf" \
  -F "knowledgeId=my-docs"
```

### Chat with Your Knowledge
```bash
curl -X POST http://localhost:8080/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "knowledgeId": "my-docs",
    "message": "What is this document about?",
    "provider": "OpenAi"
  }'
```

## 🐳 Deployment Options

### Full Stack (Recommended)
Includes Knowledge Manager, Qdrant, and Ollama:
```bash
curl -O https://raw.githubusercontent.com/waynen12/ChatComplete/main/docker-compose.dockerhub.yml
docker-compose -f docker-compose.dockerhub.yml up -d
```

### Minimal Setup
Just the Knowledge Manager (bring your own vector DB):
```bash
docker run -d -p 8080:7040 \
  -e OPENAI_API_KEY=your_key \
  -v ai-knowledge-data:/app/data \
  waynen12/ai-knowledge-manager:latest
```

## 🔍 Health Monitoring

Check service status:
```bash
# Main application
curl http://localhost:8080/api/ping
# Expected: {"status":"healthy"}

# Qdrant vector database (REST API)
curl http://localhost:6333/health
# Expected: {"status":"ok"}

# Ollama LLM service
curl http://localhost:11434/api/version
# Expected: {"version":"..."}

# Check all services via Docker
docker-compose -f docker-compose.dockerhub.yml ps
```

**Port Reference:**
- `8080`: AI Knowledge Manager web UI and API
- `6333`: Qdrant REST API (health checks)
- `6334`: Qdrant gRPC API (data operations - used by app)
- `11434`: Ollama API

## 📊 Supported File Types

- **Documents**: PDF, DOCX, TXT, MD
- **Code**: All major programming languages
- **Structured**: JSON, XML, CSV

## 🔒 Security

- Runs as non-root user
- No sensitive data logged
- API keys managed via environment variables
- Container isolation and network security

## 🏷️ Tags

- `latest` - Latest stable release
- `v1.0.0` - Specific version releases
- `main` - Latest from main branch

## 💾 Data Persistence

**Database Location:**
- Container: `/app/data/knowledge.db` (mounted to Docker volume)
- Self-hosted: `/opt/knowledge-api/data/knowledge.db` (outside `/out` directory)

**Critical**: The database MUST be outside the `/out` directory to survive deployments. The `/out` folder is wiped during updates.

**Volumes:**
- `ai-knowledge-data`: Application database, configuration, chat history
- `qdrant-data`: Vector embeddings storage
- `ollama-data`: Downloaded LLM models

## 📖 Documentation

- [Full Documentation & README](https://github.com/waynen12/ChatComplete)
- [CLAUDE.md - Development Guide](https://github.com/waynen12/ChatComplete/blob/main/CLAUDE.md)
- [Swagger API Docs](http://localhost:8080/swagger) (when running)

## 🤝 Contributing

Visit the [GitHub repository](https://github.com/waynen12/ChatComplete) to contribute, report issues, or request features.

## 📄 License

Open source under the MIT License.