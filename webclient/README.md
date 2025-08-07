# AI Knowledge Manager - Web Client

A modern React-based web interface for the AI Knowledge Manager platform. Upload technical documentation, chat with AI assistants across multiple providers, and manage your knowledge base through an intuitive UI.

## Features

- **📁 Knowledge Management**: Upload and organize technical documents (PDF, Word, Markdown, Text)
- **🤖 Multi-Provider AI Chat**: Support for OpenAI, Google Gemini, Anthropic Claude, and local Ollama models
- **💬 Persistent Conversations**: Maintain context across chat sessions with conversation history
- **🎨 Modern UI**: Built with React, TypeScript, Tailwind CSS, and shadcn/ui components
- **📝 Markdown Support**: Toggle between formatted and plain text responses
- **⚡ Real-time Experience**: Instant document processing and responsive chat interface

## Tech Stack

- **Frontend**: React 18 + TypeScript + Vite
- **Styling**: Tailwind CSS + shadcn/ui components  
- **Routing**: React Router v6
- **Animations**: Framer Motion
- **Markdown**: ReactMarkdown for rich text rendering
- **State Management**: React hooks (useState, useEffect)

## Getting Started

### Prerequisites

- Node.js 18+ and npm
- AI Knowledge Manager API running (default: http://localhost:7040)

### Installation

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

### Configuration

The development server is configured to proxy API requests to the backend:

```typescript
// vite.config.ts
server: {
  proxy: {
    "/api": {
      target: "http://192.168.50.91:7040", // Update with your API URL
      changeOrigin: true,
      secure: false
    }
  }
}
```

## Usage

### 1. Knowledge Management
- Navigate to `/knowledge` to view uploaded documents
- Click "Upload New Knowledge" to add documents to your knowledge base
- Supported formats: `.pdf`, `.docx`, `.md`, `.txt`

### 2. AI Chat Interface
- Go to `/chat` or select a knowledge item to start chatting
- Choose your preferred AI provider (OpenAI, Gemini, Claude, Ollama)
- Toggle "Strip Markdown" to control response formatting:
  - **Unchecked (default)**: Responses with rich markdown formatting
  - **Checked**: Plain text responses without formatting
- Conversations persist automatically across sessions

### 3. Provider Configuration

Each AI provider requires proper API keys set in the backend:

- **OpenAI**: Requires `OPENAI_API_KEY`
- **Google**: Requires `GOOGLE_API_KEY` 
- **Anthropic**: Requires `ANTHROPIC_API_KEY`
- **Ollama**: Requires local Ollama server running

## API Integration

The web client communicates with the Knowledge Manager API:

```typescript
// Chat request
POST /api/chat
{
  "knowledgeId": "docs-api",
  "message": "How do I delete knowledge?", 
  "temperature": 0.8,
  "stripMarkdown": false,
  "useExtendedInstructions": false,
  "provider": "OpenAi",
  "conversationId": "uuid-or-null"
}

// Knowledge upload
POST /api/knowledge
FormData with files

// List knowledge items
GET /api/knowledge
```

## Project Structure

```
src/
├── components/ui/          # Reusable UI components (shadcn/ui)
├── layouts/               # Page layouts and wrappers
├── pages/                 # Main application pages
│   ├── LandingPage.tsx    # Home page
│   ├── KnowledgeListPage.tsx    # Document management
│   ├── KnowledgeFormPage.tsx    # Upload interface
│   └── ChatPage.tsx       # AI chat interface
├── types/                 # TypeScript type definitions
└── routes.tsx            # Application routing

```

## Development

### Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint

### Key Features Implementation

**Markdown Toggle**: Users can control response formatting via the "Strip Markdown" checkbox. When enabled, the API processes responses through a markdown stripper to return plain text.

**Provider Switching**: The interface supports switching between AI providers mid-conversation, though this starts a new conversation context.

**Responsive Design**: The UI adapts to different screen sizes and provides a mobile-friendly chat experience.

## Contributing

This web client is part of the larger AI Knowledge Manager project. See the main project documentation for contribution guidelines and architecture overview.
