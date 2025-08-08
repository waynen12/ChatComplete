# AI Knowledge Manager

[![Docker Pulls](https://img.shields.io/docker/pulls/waynen12/ai-knowledge-manager.svg)](https://hub.docker.com/r/waynen12/ai-knowledge-manager)
[![Docker Image Size](https://img.shields.io/docker/image-size/waynen12/ai-knowledge-manager)](https://hub.docker.com/r/waynen12/ai-knowledge-manager)

Open-source RAG (Retrieval-Augmented Generation) system for uploading documents, vector-indexing them, and chatting over that knowledge with multiple LLM providers.

## 🚀 Quick Start

Get a complete AI knowledge management system running in under 2 minutes:

```bash
# Download the Docker Compose file
curl -O https://raw.githubusercontent.com/your-username/ChatComplete/main/docker-compose.dockerhub.yml

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

Set these via `.env` file or command line:

```env
# Required for external LLM providers
OPENAI_API_KEY=your_openai_key
ANTHROPIC_API_KEY=your_anthropic_key
GEMINI_API_KEY=your_gemini_key

# Optional: Customize vector store (defaults to Qdrant)
VectorStore__Provider=Qdrant
```

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
curl -O https://raw.githubusercontent.com/your-username/ChatComplete/main/docker-compose.dockerhub.yml
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

# Qdrant vector database
curl http://localhost:6333/health

# Ollama LLM service
curl http://localhost:11434/api/version
```

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

## 📖 Documentation

- [Full Documentation](https://github.com/your-username/ChatComplete)
- [API Reference](https://github.com/your-username/ChatComplete/blob/main/docs/api.md)
- [Deployment Guide](https://github.com/your-username/ChatComplete/blob/main/DOCKER_DEPLOYMENT.md)

## 🤝 Contributing

Visit the [GitHub repository](https://github.com/your-username/ChatComplete) to contribute, report issues, or request features.

## 📄 License

Open source under the MIT License.