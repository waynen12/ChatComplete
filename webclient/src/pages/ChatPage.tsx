import { useEffect, useRef, useState } from "react";
import { useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { AnimatePresence, motion } from "framer-motion";
import type { ChatResponseDto, OllamaModelsResponse, KnowledgeItem } from "@/types/api";
import { GLOBAL_KNOWLEDGE_ID, AI_PROVIDERS, type Provider } from "@/constants/app";
import { notify } from "@/lib/notify";
import { ChatSettingsPanel } from "@/components/ChatSettingsPanel";
import clsx from "clsx";
import ReactMarkdown from "react-markdown";


interface Message {
  id: string;
  role: "user" | "assistant";
  content: string;
}



export default function ChatPage() {
  const { id: initialKnowledgeId } = useParams();
  const [collections, setCollections] = useState<KnowledgeItem[]>([]);
  const [collectionId, setCollectionId] = useState<string>(
    initialKnowledgeId ?? GLOBAL_KNOWLEDGE_ID
  );
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [conversationId, setConversationId] = useState<string | null>(() =>
    sessionStorage.getItem("chat.cid")
  );
  const [provider, setProvider] = useState<Provider>(AI_PROVIDERS.OLLAMA);
  const [stripMarkdown, setStripMarkdown] = useState<boolean>(false);
  const [ollamaModel, setOllamaModel] = useState<string>("");
  const [availableOllamaModels, setAvailableOllamaModels] = useState<string[]>([]);
  const [loadingModels, setLoadingModels] = useState<boolean>(false);
  const [sidePanelOpen, setSidePanelOpen] = useState<boolean>(false);
  const [useAgent, setUseAgent] = useState<boolean>(false);
  const scrollRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    (async () => {
      try {
        const res = await fetch("/api/knowledge");
        if (!res.ok) {
          throw new Error(`Failed to fetch knowledge bases: ${res.status} ${res.statusText}`);
        }
        setCollections(await res.json());
      } catch (error) {
        notify.error("Failed to load knowledge bases. Please refresh the page.");
      }
    })();
  }, []);

  useEffect(() => {
    scrollRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  // Function to fetch Ollama models
  const fetchOllamaModels = async () => {
    if (provider !== AI_PROVIDERS.OLLAMA) return;
    
    setLoadingModels(true);
    try {
      const res = await fetch("/api/ollama/models");
      if (res.ok) {
        const models = await res.json() as OllamaModelsResponse;
        setAvailableOllamaModels(models);
        // Set first model as default if none selected
        if (models.length > 0 && !ollamaModel) {
          setOllamaModel(models[0]);
        }
      } else {
        notify.error(`Failed to fetch Ollama models: ${res.statusText}`);
        setAvailableOllamaModels([]);
      }
    } catch (error) {
      notify.error("Error connecting to Ollama. Please ensure Ollama is running.");
      setAvailableOllamaModels([]);
    } finally {
      setLoadingModels(false);
    }
  };

  // Fetch Ollama models when provider changes to Ollama
  useEffect(() => {
    if (provider === AI_PROVIDERS.OLLAMA) {
      fetchOllamaModels();
    } else {
      // Clear Ollama-specific state when switching away from Ollama
      setAvailableOllamaModels([]);
      setOllamaModel("");
    }
  }, [provider]);

  // Handle navigation from knowledge list page - start fresh conversation
  useEffect(() => {
    if (initialKnowledgeId) {
      setConversationId(null);
      sessionStorage.removeItem("chat.cid");
      setMessages([]);
    }
  }, [initialKnowledgeId]);

  async function sendMessage() {
    if (!input.trim()) return;
    if (collectionId === GLOBAL_KNOWLEDGE_ID && !useAgent) {
      notify.error("Please select a knowledge base or enable Agent Mode to start chatting");
      return;
    }

    const userMsg: Message = {
      id: crypto.randomUUID(),
      role: "user",
      content: input.trim(),
    };
    setMessages((m) => [...m, userMsg]);
    setInput("");

    try {
      const res = await fetch("/api/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          knowledgeId: (collectionId === GLOBAL_KNOWLEDGE_ID && useAgent) ? null : collectionId,
          message: userMsg.content,
          temperature: 0.8,
          stripMarkdown: stripMarkdown,
          useExtendedInstructions: false,
          provider,
          conversationId,
          ollamaModel: provider === AI_PROVIDERS.OLLAMA ? ollamaModel : undefined,
          useAgent: collectionId === GLOBAL_KNOWLEDGE_ID ? useAgent : false
        }),
      });

      if (!res.ok) {
        throw new Error(`Chat request failed: ${res.status} ${res.statusText}`);
      }

      const { reply, conversationId: cid } = (await res.json()) as ChatResponseDto;

      setMessages((m) => [
        ...m,
        {
          id: crypto.randomUUID(),
          role: "assistant",
          content: reply,
        },
      ]);

      // first turn → persist cid
      if (!conversationId && cid) {
        setConversationId(cid);
        sessionStorage.setItem("chat.cid", cid);
      }
    } catch (error) {
      notify.error(error instanceof Error ? error.message : "Failed to send message. Please try again.");
      
      // Remove the user message that failed to get a response
      setMessages((m) => m.filter(msg => msg.id !== userMsg.id));
      
      // Restore the input
      setInput(userMsg.content);
    }
  }

  // Callback functions for ChatSettingsPanel
  const handleCollectionChange = (newCollectionId: string) => {
    setCollectionId(newCollectionId);
    // Reset conversation when switching collections
    setConversationId(null);
    sessionStorage.removeItem("chat.cid");
    // Clear messages for new conversation
    setMessages([]);
    // Disable agent mode when selecting a knowledge base
    if (newCollectionId !== GLOBAL_KNOWLEDGE_ID) {
      setUseAgent(false);
    }
  };

  const handleProviderChange = (newProvider: Provider) => {
    setProvider(newProvider);
  };

  const handleOllamaModelChange = (newModel: string) => {
    setOllamaModel(newModel);
  };

  const handleStripMarkdownChange = (strip: boolean) => {
    setStripMarkdown(strip);
  };

  const handleAgentModeChange = (agent: boolean) => {
    setUseAgent(agent);
    // Reset conversation when toggling agent mode
    if (collectionId === GLOBAL_KNOWLEDGE_ID) {
      setConversationId(null);
      sessionStorage.removeItem("chat.cid");
      setMessages([]);
    }
  };

  return (
      <section className="h-full flex">
        {/* Side Panel */}
        <AnimatePresence>
          <ChatSettingsPanel
            isOpen={sidePanelOpen}
            onClose={() => setSidePanelOpen(false)}
            collections={collections}
            selectedCollectionId={collectionId}
            onCollectionChange={handleCollectionChange}
            provider={provider}
            onProviderChange={handleProviderChange}
            availableOllamaModels={availableOllamaModels}
            selectedOllamaModel={ollamaModel}
            onOllamaModelChange={handleOllamaModelChange}
            onModelsRefresh={fetchOllamaModels}
            loadingModels={loadingModels}
            stripMarkdown={stripMarkdown}
            onStripMarkdownChange={handleStripMarkdownChange}
            useAgent={useAgent}
            onAgentModeChange={handleAgentModeChange}
          />
        </AnimatePresence>

        {/* Main Chat Area */}
        <div className="flex-1 grid grid-rows-[auto_1fr_auto]">
          {/* Top bar with settings toggle */}
          <header className="border-b p-4 flex items-center justify-between">
            <div className="flex items-center gap-4">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setSidePanelOpen(!sidePanelOpen)}
                className="flex items-center gap-2"
              >
                ⚙️ Settings
                {!sidePanelOpen && (
                  <span className="text-xs text-muted-foreground">
                    {collectionId === GLOBAL_KNOWLEDGE_ID 
                      ? useAgent 
                        ? "• 🤖 Agent Mode"
                        : "• No knowledge base"
                      : `• ${collections.find((c) => c.id === collectionId)?.name}`
                    }
                  </span>
                )}
              </Button>
            </div>
            <div className="text-sm text-muted-foreground">
              {provider === AI_PROVIDERS.OPENAI ? "OpenAI" :
               provider === AI_PROVIDERS.GOOGLE ? "Gemini" :
               provider === AI_PROVIDERS.ANTHROPIC ? "Anthropic" :
               provider === AI_PROVIDERS.OLLAMA ? `Ollama${ollamaModel ? ` (${ollamaModel})` : ""}` : "Unknown"}
            </div>
          </header>

          {/* Messages */}
          <div className="overflow-y-auto p-6 bg-muted/30 flex justify-center">
            <div className="w-full max-w-4xl space-y-4">
              <AnimatePresence initial={false}>
                {messages.map((m) => (
                  <motion.div
                    key={m.id}
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    className={clsx(
                      "max-w-[85%] rounded-2xl px-4 py-3",
                      m.role === "user"
                        ? "bg-primary text-primary-foreground ml-auto"
                        : "bg-card text-card-foreground shadow-sm border"
                    )}
                  >
                    {m.role === "assistant"
                      ? <ReactMarkdown>{m.content}</ReactMarkdown>
                      : m.content}
                  </motion.div>
                ))}
              </AnimatePresence>
              <div ref={scrollRef} />
            </div>
          </div>

          {/* Input */}
          <footer className="p-4 border-t bg-background">
            <div className="max-w-4xl mx-auto flex gap-2">
            <Textarea
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && sendMessage()}
              placeholder={
                collectionId === GLOBAL_KNOWLEDGE_ID && useAgent
                  ? "Ask about model recommendations, performance analysis, or comparisons..."
                  : "Type your question…"
              }
            />
            <Button
              onClick={sendMessage}
              disabled={input.trim() === "" || (collectionId === GLOBAL_KNOWLEDGE_ID && !useAgent)}
            >
              Send
            </Button>
            </div>
          </footer>
        </div>
      </section>
    );
  }
