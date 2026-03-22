# ChatComplete

ChatComplete is an AI knowledge management and chat platform. It allows users to upload technical documents, convert them into a searchable knowledge base using vector embeddings, and interact with the content via chat powered by multiple large language model (LLM) providers including OpenAI, Anthropic, Google Gemini, and Ollama.

## Features

- **Document Upload & Knowledge Base**: Upload documents (PDF, DOCX, Markdown, TXT), organize them into collections, and browse/manage your knowledge bases.
- **Instant Chat with Knowledge**: Query your documents with questions, using state-of-the-art LLMs to retrieve and generate answers based on your private data.
- **Provider Agnostic**: Seamless LLM switching among OpenAI, Anthropic, Google Gemini, and local Ollama models.
- **Chat Analytics**: Rich analytics dashboard for token usage, model/provider breakdowns, cost analysis, and performance trends.
- **Ollama Model Management**: Visual management of local LLM models (download, monitor, and select Ollama models from the UI).
- **Status & Health Reporting**: Live status for each connected provider and the vector database.
- **Modern UI**: Clean, responsive interface built with React (TypeScript), Tailwind CSS, and modern component libraries.
- **End-to-End Tested**: Robust E2E test suite with Playwright for reliability.
- **Dockerized Deployment**: One-command launch for the full stack (application, vector DB, models) using Docker Compose.

## Tech Stack

- **Frontend**: React + TypeScript (with Vite), Tailwind CSS, shadcn/ui, framer-motion, recharts, lucide-react
- **Backend**: .NET 8 (C#) — not included in the `webclient` directory but targeted by the `ai-knowledge-manager` service
- **Vector DB**: Qdrant (integrated via Docker Compose)
- **LLM Providers**: OpenAI, Anthropic, Google Gemini (API keys required), Ollama (local inference, no key needed)
- **Containerization**: Docker, Docker Compose
- **Testing**: Playwright (E2E), Vitest (component/unit)
- **Utilities**: ESLint, Tailwind CLI, GitHub Actions (CI/CD)

## Getting Started

### Prerequisites

- **Docker** and **Docker Compose** (for full-stack, all-in-one setup)
- Node.js and npm (only needed for UI development or running tests locally)
- API keys for OpenAI, Anthropic, and Google Gemini (not required for Ollama/local models)
- (Optional for development): .NET 8 SDK for backend code

### Installation & Setup

**To run the complete stack via Docker:**

1. Clone the repository:

    ```bash
    git clone https://github.com/your-org/ChatComplete.git
    cd ChatComplete
    ```

2. Copy or create an `.env` file with your API keys:

    ```env
    OPENAI_API_KEY=sk-...
    ANTHROPIC_API_KEY=sk-ant-...
    GEMINI_API_KEY=...
    ```

3. Launch the stack:

    ```bash
    docker-compose up -d
    ```

   This runs:
   - The main ChatComplete application (frontend + backend)
   - Qdrant vector database
   - Ollama LLM server (local/private models)

4. Access the app at: [http://localhost:8080](http://localhost:8080)

**For local frontend development:**

```bash
cd webclient
npm install
npm run dev
```

### Environment Variables

Configure LLM provider keys and other settings via `.env` or Docker Compose environment. See `docker-compose.yml` for supported variables:
- `OPENAI_API_KEY`
- `ANTHROPIC_API_KEY`
- `GEMINI_API_KEY`
- Vector store provider settings

---

## Usage

- **Landing Page** (`/`): Intro and navigation.
- **Manage Knowledge** (`/knowledge`): List, create, and delete knowledge collections.
- **Add Knowledge** (`/knowledge/new`): Upload documents to a collection.
- **Chat** (`/chat`): Converse with your AI using the uploaded documents.
- **Analytics** (`/analytics`): View usage, provider breakdowns, and cost analysis.

### Typical Workflow

1. Visit `/knowledge` to create a new collection and upload documents.
2. Go to `/chat` to ask questions about your documents.
3. Use the settings panel to select your preferred LLM provider or Ollama model.
4. Monitor usage and cost analytics in `/analytics`.

---

## Project Structure

```
ChatComplete/
├── docker-compose.yml
├── Dockerfile
├── README.md
└── webclient/
    ├── src/
    │   ├── components/
    │   │   ├── analytics/
    │   │   │   ├── AnthropicBalanceWidget.tsx
    │   │   │   ├── CostBreakdownChart.tsx
    │   │   │   ├── GoogleAIBalanceWidget.tsx
    │   │   │   ├── OllamaUsageWidget.tsx
    │   │   │   ├── OpenAIBalanceWidget.tsx
    │   │   │   ├── PerformanceMetrics.tsx
    │   │   │   ├── ProviderStatusCards.tsx
    │   │   │   └── UsageTrendsChart.tsx
    │   │   ├── icons/
    │   │   │   ├── AnthropicIcon.tsx
    │   │   │   ├── ConversationIcon.tsx
    │   │   │   ├── GoogleAIIcon.tsx
    │   │   │   ├── KnowledgeIcon.tsx
    │   │   │   ├── OllamaIcon.tsx
    │   │   │   ├── OpenAIIcon.tsx
    │   │   │   ├── PerformanceIcon.tsx
    │   │   │   ├── ProviderIcon.tsx
    │   │   │   ├── StarIcon.tsx
    │   │   │   └── TokenIcon.tsx
    │   │   ├── ui/
    │   │   │   ├── alert-dialog.tsx
    │   │   │   ├── badge.tsx
    │   │   │   ├── button.tsx
    │   │   │   ├── card.tsx
    │   │   │   ├── dialog.tsx
    │   │   │   ├── dropdown-menu.tsx
    │   │   │   ├── input.tsx
    │   │   │   ├── label.tsx
    │   │   │   ├── progress.tsx
    │   │   │   ├── scroll-area.tsx
    │   │   │   ├── select.tsx
    │   │   │   ├── sonner.tsx
    │   │   │   └── textarea.tsx
    │   │   ├── ChatSettingsPanel.tsx
    │   │   ├── IconPreview.tsx
    │   │   ├── OllamaModelManager.tsx
    │   │   └── ThemeToggle.tsx
    │   ├── lib/
    │   │   ├── notify.ts
    │   │   └── utils.ts
    │   ├── pages/
    │   │   ├── AnalyticsPage.tsx
    │   │   ├── ChatPage.tsx
    │   │   ├── KnowledgeFormPage.tsx
    │   │   ├── KnowledgeListPage.tsx
    │   │   ├── LandingPage.tsx
    │   │   └── NotFoundPage.tsx
    │   ├── test/
    │   │   ├── e2e/ ...
    │   │   ├── App.test.tsx
    │   │   └── setup.ts
    │   ├── types/
    │   │   ├── api.ts
    │   │   └── ollama.ts
    │   └── ...
    ├── package.json
    └── ...
```

---

## API Endpoints

Below are the key REST API endpoints as inferred from UI code and type definitions (`webclient/src/types/api.ts`):

| Method | Path                               | Description                                             |
|--------|------------------------------------|---------------------------------------------------------|
| GET    | `/api/knowledge`                   | List all knowledge collections.                         |
| POST   | `/api/knowledge`                   | Create a new knowledge collection and upload documents. |
| DELETE | `/api/knowledge/:id`               | Delete a knowledge collection.                          |
| GET    | `/api/ollama/models`               | List available Ollama models on the local server.       |
| POST   | `/api/chat`                        | Send a chat question and receive an answer.             |
| GET    | `/api/analytics`                   | Retrieve analytics and usage data for dashboard.        |

Additional endpoints exist for provider status, document upload, download progress, health checks, and more, but these are the core routes visible to the frontend.

---

## Key Files and What They Do

- **docker-compose.yml**: Defines the multi-container stack — main app, Qdrant vector DB, Ollama, environment, and volumes.
- **Dockerfile**: Builds the application container image.
- **webclient/package.json**: NPM dependencies and scripts for the frontend.
- **webclient/src/pages/ChatPage.tsx**: Core page for chatting with your knowledge base, managing conversation context, provider/model selection, and live streaming of assistant replies.
- **webclient/src/pages/KnowledgeListPage.tsx**: Lists all knowledge collections, allowing sorting, searching, and deleting of collections.
- **webclient/src/pages/KnowledgeFormPage.tsx**: Form for uploading documents and creating a new knowledge base collection, including client-side validation.
- **webclient/src/pages/AnalyticsPage.tsx**: Analytics dashboard with widgets for token usage, request count, model/provider breakdowns, and cost graphs.
- **webclient/src/components/ChatSettingsPanel.tsx**: Slide-out panel for configuring chat context — select provider, knowledge base, or Ollama model.
- **webclient/src/components/OllamaModelManager.tsx**: UI for viewing, downloading, and managing Ollama LLM models.
- **webclient/src/components/analytics/**: Contains modular analytic widgets and charts for provider balances, cost breakdowns, performance, requests, etc.
- **webclient/src/components/icons/**: Custom SVG icon components for all providers (OpenAI, Anthropic, Google AI, Ollama), tokens, performance, etc.
- **webclient/src/components/ui/**: Common UI primitives (card, button, badge, dialog, dropdown, progress bar, etc.), largely based on shadcn/ui and Radix UI.
- **webclient/src/types/api.ts, ollama.ts**: API data shapes and type definitions shared across the UI.
- **webclient/src/lib/notify.ts**: Wraps toast notifications for user feedback.
- **webclient/src/test/e2e/**: End-to-end Playwright test suite, mocks, and helpers with coverage for all user flows.
- **webclient/src/test/App.test.tsx**: Entry unit/component test for App root.

---

## Notes

- Ollama mode and model management do not require external API keys and run locally for privacy.
- All chat, knowledge uploading, and analytics are handled via REST API endpoints provided by the backend.
- The frontend is fully portable and can be run standalone for development, or packaged in the main Docker container for production.

---

**Need help or want to contribute?**  
Please refer to issue tracker or project discussions (if available) for support. For API details and backend code, see the main repo sections outside `webclient/`.

---
**Enjoy fast, private, multi-provider RAG with ChatComplete!**