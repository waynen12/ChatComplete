# ChatComplete

ChatComplete is an open-source RAG (Retrieval-Augmented Generation) system that enables you to upload technical documents, index them in a vector database, and interact with them through conversational AI powered by multiple LLM providers.

## Features

- **Multi-Provider AI Support**: Seamlessly integrate with OpenAI, Anthropic Claude, Google Gemini, and Ollama (local models)
- **Document Knowledge Base**: Upload and manage PDF, DOCX, Markdown, and text documents
- **Vector Search**: Automatic document chunking and embedding using Qdrant vector database
- **Real-time Analytics**: Monitor API usage, costs, token consumption, and performance metrics across all providers
- **Interactive Chat Interface**: Conversation-based document querying with context-aware responses
- **Ollama Model Management**: Download, manage, and switch between local Ollama models directly from the UI
- **Real-time Updates**: SignalR-powered live updates for provider balances and usage statistics
- **Dark Mode Support**: Full theme toggle with persistent preferences

## Tech Stack

### Backend
- **.NET 8** (C#) - ASP.NET Core Web API
- **Qdrant** - Vector database for embeddings and semantic search
- **SignalR** - Real-time communication for live analytics

### Frontend
- **React 18** with **TypeScript**
- **Vite** - Fast development and build tooling
- **Tailwind CSS** - Utility-first styling
- **shadcn/ui** - High-quality React component library
- **Framer Motion** - Smooth animations
- **Recharts** - Data visualization for analytics
- **React Router** - Client-side routing
- **React Markdown** - Markdown rendering in chat

### Infrastructure
- **Docker** & **Docker Compose** - Containerization and orchestration
- **Ollama** - Local LLM inference (optional)

### Testing
- **Vitest** - Unit testing for React components
- **Playwright** - End-to-end testing
- **Testing Library** - React component testing utilities

## Getting Started

### Prerequisites

- **Docker** and **Docker Compose** (recommended)
- OR **.NET 8 SDK** + **Node.js 18+** (for local development)
- **API Keys** (at least one):
  - OpenAI API key
  - Anthropic API key
  - Google Gemini API key
  - OR use Ollama for local inference (no API key needed)

### Installation

#### Option 1: Docker Compose (Recommended)

1. **Clone the repository**:
```bash
git clone https://github.com/waynen12/ChatComplete.git
cd ChatComplete
```

2. **Set up environment variables**:
```bash
# Create .env file in project root
cat > .env << EOF
OPENAI_API_KEY=your_openai_key_here
ANTHROPIC_API_KEY=your_anthropic_key_here
GEMINI_API_KEY=your_gemini_key_here
EOF
```

3. **Start all services**:
```bash
docker-compose up -d
```

This starts:
- AI Knowledge Manager (port 8080)
- Qdrant vector database (port 6333 for HTTP, 6334 for gRPC)
- Ollama (port 11434) - optional, comment out if not needed

4. **Access the application**:
```
http://localhost:8080
```

#### Option 2: Quick Start (Pre-built Image)

Use the pre-built Docker image from Docker Hub:

```bash
# Download the Docker Hub compose file
curl -O https://raw.githubusercontent.com/waynen12/ChatComplete/main/docker-compose.dockerhub.yml

# Start with your API keys
OPENAI_API_KEY=your_key docker-compose -f docker-compose.dockerhub.yml up -d

# Access at http://localhost:8080
```

#### Option 3: Local Development

1. **Backend Setup**:
```bash
cd ChatComplete  # .NET project directory
dotnet restore
dotnet run
# Backend runs on http://localhost:7040
```

2. **Frontend Setup**:
```bash
cd webclient
npm install
npm run dev
# Frontend runs on http://localhost:5173
```

3. **Start Qdrant** (required for vector search):
```bash
docker run -p 6333:6333 -p 6334:6334 \
  -v qdrant_storage:/qdrant/storage \
  qdrant/qdrant
```

### Environment Configuration

The application uses these environment variables:

```bash
# AI Provider API Keys
OPENAI_API_KEY=sk-...
ANTHROPIC_API_KEY=sk-ant-...
GEMINI_API_KEY=AIza...

# Vector Store Configuration (defaults shown)
ChatCompleteSettings__VectorStore__Provider=Qdrant
ChatCompleteSettings__VectorStore__Qdrant__Host=qdrant
ChatCompleteSettings__VectorStore__Qdrant__Port=6334

# ASP.NET Core Settings
ASPNETCORE_ENVIRONMENT=Production
DOTNET_RUNNING_IN_CONTAINER=true
```

## Usage

### 1. Create a Knowledge Base

1. Navigate to **Knowledge** from the main menu
2. Click **Create New Knowledge Base**
3. Enter a name for your collection
4. Upload documents (PDF, DOCX, MD, TXT supported, up to 100MB per file)
5. Wait for document processing and embedding

### 2. Chat with Your Documents

1. Navigate to **Chat**
2. Select a knowledge base from the settings panel (⚙️ icon)
3. Choose your preferred AI provider (OpenAI, Anthropic, Google, Ollama)
4. For Ollama: select which local model to use
5. Start asking questions about your documents

### 3. Monitor Analytics

1. Navigate to **Analytics** to view:
   - **Provider Status Cards**: Real-time connection status and balances
   - **Cost Breakdown**: Spending distribution across providers
   - **Usage Trends**: Request volume and token consumption over time
   - **Performance Metrics**: Response times and success rates
   - **Provider-Specific Widgets**: 
     - OpenAI & Google balances
     - Anthropic usage details
     - Ollama model statistics and disk usage

### 4. Manage Ollama Models

From the Chat page settings panel:
- **Search & Download**: Browse and pull models from Ollama library
- **View Details**: See model size, parameters, last used date
- **Delete Models**: Free up disk space
- **Monitor Downloads**: Real-time progress tracking

## Project Structure

```
ChatComplete/
├── ChatComplete/                # .NET Backend
│   ├── Controllers/            # API endpoints
│   ├── Services/               # Business logic
│   │   ├── Chat/              # Chat service implementations
│   │   ├── Knowledge/         # Document processing
│   │   └── Providers/         # AI provider integrations
│   ├── Hubs/                  # SignalR hubs for real-time updates
│   ├── Models/                # Data models and DTOs
│   └── Program.cs             # Application entry point
│
├── webclient/                  # React Frontend
│   ├── src/
│   │   ├── components/        # React components
│   │   │   ├── analytics/    # Analytics widgets
│   │   │   ├── icons/        # Provider icons
│   │   │   └── ui/           # shadcn/ui components
│   │   ├── pages/            # Route pages
│   │   ├── types/            # TypeScript type definitions
│   │   ├── lib/              # Utilities and helpers
│   │   └── test/             # Test files
│   │       └── e2e/          # Playwright E2E tests
│   ├── public/               # Static assets
│   └── package.json          # Frontend dependencies
│
├── docker-compose.yml         # Local development compose
├── docker-compose.dockerhub.yml  # Pre-built image compose
├── Dockerfile                 # Multi-stage build
└── README.md
```

## API Endpoints

### Knowledge Management

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/knowledge` | List all knowledge bases |
| `POST` | `/api/knowledge` | Create new knowledge base with documents |
| `DELETE` | `/api/knowledge/{id}` | Delete a knowledge base |

### Chat

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/chat` | Send a chat message and get AI response |
| `DELETE` | `/api/chat/conversations/{id}` | Clear conversation history |

### Analytics

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/analytics/usage` | Get usage statistics and trends |
| `GET` | `/api/analytics/cost-breakdown` | Get cost distribution by provider |
| `GET` | `/api/analytics/performance` | Get model performance metrics |

### Provider Status

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/providers` | Get all provider connection statuses |
| `GET` | `/api/providers/openai/balance` | OpenAI account balance |
| `GET` | `/api/providers/anthropic/usage` | Anthropic usage details |
| `GET` | `/api/providers/google/balance` | Google AI balance |
| `GET` | `/api/providers/ollama/usage` | Ollama model statistics |

### Ollama Management

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/ollama/models` | List available Ollama models |
| `GET` | `/api/ollama/models/details` | Get detailed model information |
| `POST` | `/api/ollama/models/download` | Download/pull a new model |
| `DELETE` | `/api/ollama/models/{name}` | Delete an Ollama model |
| `GET` | `/api/ollama/downloads` | Get active download statuses |

### SignalR Hubs

- `/hubs/analytics` - Real-time analytics updates for provider balances and usage

## Key Files

| File | Purpose |
|------|---------|
| `docker-compose.yml` | Multi-container orchestration for local development |
| `Dockerfile` | Multi-stage build: frontend assets + .NET runtime |
| `ChatComplete/Program.cs` | ASP.NET Core configuration, dependency injection, middleware |
| `ChatComplete/Services/Chat/ChatService.cs` | Main chat orchestration and provider selection |
| `ChatComplete/Services/Knowledge/KnowledgeService.cs` | Document ingestion, chunking, embedding pipeline |
| `ChatComplete/Hubs/AnalyticsHub.cs` | SignalR hub for live analytics broadcasting |
| `webclient/src/App.tsx` | React app root with routing configuration |
| `webclient/src/pages/ChatPage.tsx` | Main chat interface with message history |
| `webclient/src/pages/AnalyticsPage.tsx` | Analytics dashboard with real-time updates |
| `webclient/src/pages/KnowledgeListPage.tsx` | Knowledge base management interface |
| `webclient/src/components/OllamaModelManager.tsx` | Ollama model download and management UI |
| `webclient/src/components/analytics/*` | Provider-specific analytics widgets |
| `webclient/tailwind.config.ts` | Tailwind CSS theme configuration |
| `webclient/vitest.config.ts` | Vitest unit test configuration |
| `webclient/playwright.config.ts` | Playwright E2E test configuration |

---

**Built with ❤️ using .NET 8, React, and open-source AI**