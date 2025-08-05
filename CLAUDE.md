You are joining AI Knowledge Manager, an open-source stack for uploading technical docs, vector-indexing them in MongoDB Atlas, and chatting over that knowledge with multiple LLM providers (OpenAI, Gemini, Ollama, Anthropic).
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
9	Unit / integration tests (incl. kernel-selection)	🔄
10	Guard super-large code fences	🛠️
11	UI MD toggle & README note	🛠️
12	Build+Deploy job split (CI refactor)	🛠️
13	Refactor AtlasIndexManager for DI	🛠️
14	Upgrade SK → 1.6 & migrate to Mongo Vector Store	✅
15	Provider menu + Anthropic fallback	✅
16	README & Swagger examples for 4 providers	✅
17	Qdrant Vector Store parallel implementation	🛠️

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

Now for further information on the project 
 can you read through the 
 PROJECT_SUMARY, 
 QDRANT_IMPLEMENTATION_PLAN, 
 AGENT_IMPLEMENTATION_PLAN   
 Do not perform any further tasks just yet.    
You are now primed with full context.
