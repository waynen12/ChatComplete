# ChatComplete (AI Knowledge Manager)

ChatComplete is an open-source AI knowledge management and chat application that lets you upload documents, turn them into searchable knowledge, and chat over that knowledge using multiple LLM providers. It includes a web UI for knowledge management, chat, and analytics, and a backend that can use **Qdrant** for vector search and supports provider integrations such as **OpenAI**, **Anthropic**, **Google Gemini**, and **Ollama**.

> The repository contains an existing `README.md` (titled **AI Knowledge Manager**) and Docker tooling to run the full stack.

---

## Features

- **Knowledge bases**
  - Create and manage knowledge collections
  - Upload documents (client-side validation for: `pdf`, `docx`, `md`, `txt`; max **100 MB** per file)
- **Chat over knowledge (RAG)**
  - Chat UI with conversation support (`conversationId`)
  - Select knowledge base to chat against (includes a global knowledge option via `GLOBAL_KNOWLEDGE_ID`)
- **Multi-provider support**
  - Provider selection via UI (`AI_PROVIDERS`)
  - API key support via environment variables (OpenAI, Anthropic, Gemini)
  - Ollama local model selection and model management UI
- **Analytics dashboard**
  - Provider usage/balance widgets (OpenAI, Anthropic, Google AI, Ollama)
  - Cost breakdown charts, usage trends, and performance metrics
  - Real-time updates in several widgets via **SignalR** (`@microsoft/signalr`)
- **Modern UI system**
  - Tailwind CSS styling and a component set built on Radix UI primitives (shadcn-style components)
  - Theme toggle (light/dark) via `ThemeContext`
- **Testing**
  - Unit tests with **Vitest**
  - End-to-end tests with **Playwright**, including API mocking helpers

---

## Tech Stack

### Backend / Services
- **.NET / ASP.NET Core** (implied by `ASPNETCORE_ENVIRONMENT`, port mapping, and extensive `.cs` presence)
- **SignalR** (real-time updates consumed by the web client)
- **Qdrant** (vector store; configured as provider in Docker Compose)
- **Docker / Docker Compose** for running the stack

### Web Client
- **React + TypeScript**
- **React Router** (pages such as `/`, `/knowledge`, `/chat`, `/analytics`)
- **Tailwind CSS**
- **Radix UI** primitives (dialogs, selects, dropdowns, etc.)
- **framer-motion** (animated panels/widgets)
- **recharts** (charts)
- **sonner** (toast notifications)

### Testing
- **Vitest** + Testing Library (`@testing-library/react`)
- **Playwright** (E2E tests + network mocking)

---

## Getting Started

### Prerequisites

For running via Docker (recommended):
- **Docker** and **Docker Compose**

For local web development:
- **Node.js** (required for the `webclient/` workspace)
- A running backend that exposes the `/api/...` endpoints and SignalR hubs expected by the UI

> The repo includes `docker-compose.yml` and a `Dockerfile`, which is the most direct way to run the full system.

---

## Installation & Environment Setup

### 1) Configure environment variables

`docker-compose.yml` expects provider API keys to be available in your environment (or in a `.env` file loaded by Compose):

- `OPENAI_API_KEY`
- `ANTHROPIC_API_KEY`
- `GEMINI_API_KEY`

Example `.env` (at repository root):
```env
OPENAI_API_KEY=...
ANTHROPIC_API_KEY=...
GEMINI_API_KEY=...
```

### 2) Run with Docker Compose

From the repository root:
```bash
docker compose up -d --build
```

By default, the compose file maps:
- **Host**: `http://localhost:8080`
- **Container**: port `7040` (mapped as `8080:7040`)

The compose configuration also:
- Creates a persistent volume `ai-knowledge-data:/app/data`
- Mounts local `./logs` into `/app/data/logs`
- Configures vector store to **Qdrant**:
  - `ChatCompleteSettings__VectorStore__Provider=Qdrant`
  - `ChatCompleteSettings__VectorStore__Qdrant__Host=qdrant`
  - `ChatCompleteSettings__VectorStore__Qdrant__Port=6334` (gRPC)

---

## Usage

### Web UI routes

The React app provides these core pages (from `webclient/src/pages`):

- `/` — Landing page (CTA to manage knowledge)
- `/knowledge` — Knowledge list and management
- `/knowledge/new` — Create a knowledge collection and upload documents
- `/chat` and `/chat/:id` — Chat UI (optionally starting with a given knowledge base id)
- `/analytics` — Provider analytics dashboard
- Any unknown route — 404 page

### Typical workflow

1. Open the app:
   - `http://localhost:8080`
2. Go to **Manage Knowledge**:
   - Create a collection and upload supported documents.
3. Navigate to **Chat**:
   - Open the settings panel and select:
     - Knowledge base/collection
     - Provider (OpenAI / Anthropic / Google / Ollama)
     - Ollama model (if Ollama is the chosen provider)
4. Use **Analytics**:
   - Monitor provider connection status, balances/usage, trends, and performance.

---

## Project Structure Overview

High-level layout (based on detected files):

```text
.
├── docker-compose.yml
├── Dockerfile
├── package.json
├── README.md
├── webclient/
│   └── src/
│       ├── components/
│       │   ├── analytics/
│       │   ├── icons/
│       │   └── ui/
│       ├── context/
│       ├── lib/
│       ├── pages/
│       ├── test/
│       │   ├── e2e/
│       │   │   └── helpers/
│       │   └── setup.ts
│       └── types/
└── .github/
    └── workflows/
```

---

## API Endpoints (used by the web client)

The frontend and tests reference these HTTP endpoints:

| Method | Path | Used for |
|---|---|---|
| `GET` | `/api/knowledge` | List knowledge bases/collections (used by knowledge list page and tests) |
| `POST`/`PUT` | `/api/knowledge` *(inferred)* | Create/upload knowledge collections (UI supports upload flow; exact method depends on backend) |
| `GET` | `/api/ollama/models` | Fetch available Ollama models (`OllamaModelsResponse` is `string[]`) |

### Realtime (SignalR)

Several analytics widgets establish a SignalR connection via `@microsoft/signalr` to receive live updates (exact hub URL is determined by backend and widget code; the UI tracks connection status: `disconnected` → `connecting` → `connected`).

Widgets using SignalR include:
- `OpenAIBalanceWidget`
- `GoogleAIBalanceWidget`
- `AnthropicBalanceWidget`

---

## Key Files

### Root
- **`docker-compose.yml`**
  - Runs the main application container (`ai-knowledge-manager`) and a Qdrant dependency.
  - Sets environment variables for ASP.NET Core and vector store configuration.
- **`Dockerfile`**
  - Defines how the main container image is built.
- **`package.json`**
  - Minimal root Node manifest (dev dependency: `@types/css-modules`), indicating most web dependencies live under `webclient/`.

### Web client (`webclient/src`)
- **Pages**
  - `pages/LandingPage.tsx` — entry page with “Manage Knowledge” CTA.
  - `pages/KnowledgeListPage.tsx` — lists collections with sorting/search and delete confirmation dialogs.
  - `pages/KnowledgeFormPage.tsx` — create/upload flow with strict client-side file validation (extensions + max size).
  - `pages/ChatPage.tsx` — main chat experience; supports `/chat/:id`, settings panel, markdown rendering (`react-markdown`), conversation id handling.
  - `pages/AnalyticsPage.tsx` — analytics dashboard container.
  - `pages/NotFoundPage.tsx` — 404 page.

- **Core components**
  - `components/ChatSettingsPanel.tsx` — slide-out panel for knowledge base, provider, and Ollama model selection.
  - `components/OllamaModelManager.tsx` — UI for searching, downloading, and managing Ollama models (includes progress/status UI patterns).
  - `components/ThemeToggle.tsx` — toggles theme using `ThemeContext`.

- **Analytics components (`components/analytics/*`)**
  - `ProviderStatusCards.tsx` — provider connection/key/balance status cards.
  - `CostBreakdownChart.tsx` — pie chart of costs per provider.
  - `UsageTrendsChart.tsx` — line chart of usage over time (includes table view option).
  - `PerformanceMetrics.tsx` — performance visualization/table with sorting.
  - Provider widgets:
    - `OpenAIBalanceWidget.tsx`
    - `AnthropicBalanceWidget.tsx`
    - `GoogleAIBalanceWidget.tsx`
    - `OllamaUsageWidget.tsx`

- **UI primitives (`components/ui/*`)**
  - shadcn-style wrappers around Radix UI primitives: `button`, `card`, `dialog`, `select`, `dropdown-menu`, `alert-dialog`, etc.
  - Uses `cn()` from `lib/utils.ts` to merge Tailwind class names.

- **Types**
  - `types/api.ts` — shared DTOs used by the UI (`ChatResponseDto`, `KnowledgeSummaryDto`, `KnowledgeItem`, `OllamaModelsResponse`).
  - `types/ollama.ts` — richer Ollama model and download status types used by model management UI.

- **Utilities**
  - `lib/notify.ts` — thin wrapper over `sonner` to standardize toast notifications.
  - `lib/utils.ts` — `cn()` helper using `clsx` + `tailwind-merge`.

### Tests (`webclient/src/test`)
- **Vitest**
  - `test/App.test.tsx` — smoke test rendering the app.
  - `test/setup.ts` — Testing Library DOM matchers.
- **Playwright E2E**
  - `test/e2e/*.spec.ts` — coverage for navigation, chat, analytics, knowledge flows, loading/error handling.
  - `test/e2e/helpers/api-mocks.ts` — route mocking for `/api/knowledge`, analytics endpoints, and other API calls to run tests without a live backend.
  - `test/e2e/README.md` — commands for running Playwright tests (`npm run test:e2e`, UI mode, debug, etc.).

---

## Running Tests

### Unit tests (Vitest)
Run from the web client workspace (where the scripts are defined):
```bash
cd webclient
npm test
```

### E2E tests (Playwright)
Per `webclient/src/test/e2e/README.md`:
```bash
cd webclient
npm run test:e2e
# or
npm run test:e2e:ui
npm run test:e2e:debug
```

---

## Notes on Configuration (Docker)

The included `docker-compose.yml` is configured for production-like container execution:

- `ASPNETCORE_ENVIRONMENT=Production`
- `DOTNET_RUNNING_IN_CONTAINER=true`
- Vector store is set to Qdrant with gRPC port `6334`
- App healthcheck verifies port `7040` inside the container

If you intend to develop the frontend separately, ensure the backend is reachable at the same origin or configure the dev server proxy accordingly (frontend code and tests assume `/api/...` paths).