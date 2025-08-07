import { useEffect, useRef, useState } from "react";
import { useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { motion, AnimatePresence } from "framer-motion";
import type { ChatResponseDto } from "@/types/api";
import clsx from "clsx";
import ReactMarkdown from "react-markdown";


interface Message {
  id: string;
  role: "user" | "assistant";
  content: string;
}

interface KnowledgeItem {
  id: string;
  name: string;
}

type Provider = "OpenAi" | "Google" | "Anthropic" | "Ollama";           // keep as string union

export default function ChatPage() {
  const { id: initialKnowledgeId } = useParams();
  const [collections, setCollections] = useState<KnowledgeItem[]>([]);
  const [collectionId, setCollectionId] = useState<string>(
    initialKnowledgeId ?? "__global__"
  );
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [conversationId, setConversationId] = useState<string | null>(() =>
    sessionStorage.getItem("chat.cid")
  );
  const [provider, setProvider] = useState<Provider>("Ollama");
  const [stripMarkdown, setStripMarkdown] = useState<boolean>(false);
  const [ollamaModel, setOllamaModel] = useState<string>("");
  const [availableOllamaModels, setAvailableOllamaModels] = useState<string[]>([]);
  const [loadingModels, setLoadingModels] = useState<boolean>(false);
  const [sidePanelOpen, setSidePanelOpen] = useState<boolean>(false);
  const scrollRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    (async () => {
      const res = await fetch("/api/knowledge");
      if (!res.ok) return;
      setCollections(await res.json());
    })();
  }, []);

  useEffect(() => {
    scrollRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  // Fetch Ollama models when provider changes to Ollama
  useEffect(() => {
    if (provider === "Ollama") {
      (async () => {
        setLoadingModels(true);
        try {
          const res = await fetch("/api/ollama/models");
          if (res.ok) {
            const models = await res.json() as string[];
            setAvailableOllamaModels(models);
            // Set first model as default if none selected
            if (models.length > 0 && !ollamaModel) {
              setOllamaModel(models[0]);
            }
          } else {
            console.error("Failed to fetch Ollama models:", res.statusText);
            setAvailableOllamaModels([]);
          }
        } catch (error) {
          console.error("Error fetching Ollama models:", error);
          setAvailableOllamaModels([]);
        } finally {
          setLoadingModels(false);
        }
      })();
    } else {
      // Clear Ollama-specific state when switching away from Ollama
      setAvailableOllamaModels([]);
      setOllamaModel("");
    }
  }, [provider, ollamaModel]);

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
    if (collectionId === "__global__") {
      // Could show a toast or other error indication here
      return;
    }
    const userMsg: Message = {
      id: crypto.randomUUID(),
      role: "user",
      content: input.trim(),
    };
    setMessages((m) => [...m, userMsg]);
    setInput("");

    const res = await fetch("/api/chat", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        knowledgeId: collectionId === "__global__" ? null : collectionId,
        message: userMsg.content,
        temperature: 0.8,
        stripMarkdown: stripMarkdown,
        useExtendedInstructions: false,
        provider,
        conversationId,
        ollamaModel: provider === "Ollama" ? ollamaModel : undefined
      }),
    });



    const { reply, conversationId: cid } =
      (await res.json()) as ChatResponseDto;

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
    
  }

    return (
      <section className="h-full flex">
        {/* Side Panel */}
        <AnimatePresence>
          {sidePanelOpen && (
            <motion.div
              initial={{ width: 0, opacity: 0 }}
              animate={{ width: 380, opacity: 1 }}
              exit={{ width: 0, opacity: 0 }}
              transition={{ duration: 0.3, ease: "easeInOut" }}
              className="border-r bg-muted/30 overflow-hidden"
            >
              <div className="p-6 space-y-6 w-96">
                <div className="flex items-center justify-between">
                  <h3 className="text-lg font-semibold">Chat Settings</h3>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setSidePanelOpen(false)}
                    className="h-8 w-8 p-0"
                  >
                    ✕
                  </Button>
                </div>

                {/* Knowledge Selection */}
                <div className="space-y-2">
                  <label className="text-sm font-medium text-foreground">
                    Knowledge Base <span className="text-destructive">*</span>
                  </label>
                  {collections.length > 0 ? (
                    <Select
                      value={collectionId}
                      onValueChange={(v) => {
                        setCollectionId(v);
                        // Reset conversation when switching collections
                        setConversationId(null);
                        sessionStorage.removeItem("chat.cid");
                        // Clear messages for new conversation
                        setMessages([]);
                      }}
                    >
                      <SelectTrigger className="w-full">
                        <span className="truncate">
                          {collectionId === "__global__"
                            ? "Please choose a knowledge item"
                            : collections.find((c) => c.id === collectionId)?.name ?? "Unknown"}
                        </span>
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="__global__" disabled>Please choose a knowledge item</SelectItem>
                        {collections.map((c) => (
                          <SelectItem key={c.id} value={c.id}>
                            {c.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  ) : (
                    <div className="text-sm text-muted-foreground p-2 border rounded">
                      Loading knowledge items...
                    </div>
                  )}
                </div>

                {/* AI Provider Selection */}
                <div className="space-y-2">
                  <label className="text-sm font-medium text-foreground">AI Provider</label>
                  <Select value={provider} onValueChange={(v) => setProvider(v as Provider)}>
                    <SelectTrigger className="w-full">
                      {provider === "OpenAi" ? "OpenAI" :
                       provider === "Google" ? "Gemini" :
                       provider === "Anthropic" ? "Anthropic" :
                       provider === "Ollama" ? "Ollama" : "Unknown"}
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="OpenAi">OpenAI</SelectItem>
                      <SelectItem value="Google">Gemini</SelectItem>
                      <SelectItem value="Anthropic">Anthropic</SelectItem>
                      <SelectItem value="Ollama">Ollama</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                {/* Ollama Model Selection - only shown when Ollama is selected */}
                {provider === "Ollama" && (
                  <div className="space-y-2">
                    <label className="text-sm font-medium text-foreground">
                      Ollama Model
                      {availableOllamaModels.length === 0 && !loadingModels && (
                        <span className="text-destructive ml-1">*</span>
                      )}
                    </label>
                    <Select 
                      value={ollamaModel} 
                      onValueChange={setOllamaModel}
                      disabled={loadingModels || availableOllamaModels.length === 0}
                    >
                      <SelectTrigger className="w-full">
                        {loadingModels ? "Loading models..." : 
                         availableOllamaModels.length === 0 ? "No models available" :
                         ollamaModel || "Select a model"}
                      </SelectTrigger>
                      <SelectContent>
                        {availableOllamaModels.map((model) => (
                          <SelectItem key={model} value={model}>
                            {model}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    {availableOllamaModels.length === 0 && !loadingModels && (
                      <p className="text-xs text-muted-foreground">
                        No Ollama models found. Run 'ollama pull &lt;model&gt;' to install models.
                      </p>
                    )}
                  </div>
                )}

                {/* Markdown Toggle */}
                <div className="space-y-2">
                  <label className="text-sm font-medium text-foreground">Response Format</label>
                  <div className="flex items-center space-x-2">
                    <input
                      type="checkbox"
                      id="strip-markdown"
                      checked={stripMarkdown}
                      onChange={(e) => setStripMarkdown(e.target.checked)}
                      className="w-4 h-4 text-primary bg-background border-border rounded focus:ring-primary focus:ring-2"
                    />
                    <label htmlFor="strip-markdown" className="text-sm text-foreground cursor-pointer">
                      Strip Markdown formatting
                    </label>
                  </div>
                  <p className="text-xs text-muted-foreground">
                    Remove formatting from AI responses for plain text output
                  </p>
                </div>
              </div>
            </motion.div>
          )}
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
                {!sidePanelOpen && collectionId !== "__global__" && (
                  <span className="text-xs text-muted-foreground">
                    • {collections.find((c) => c.id === collectionId)?.name}
                  </span>
                )}
              </Button>
            </div>
            <div className="text-sm text-muted-foreground">
              {provider === "OpenAi" ? "OpenAI" :
               provider === "Google" ? "Gemini" :
               provider === "Anthropic" ? "Anthropic" :
               provider === "Ollama" ? `Ollama${ollamaModel ? ` (${ollamaModel})` : ""}` : "Unknown"}
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
              placeholder="Type your question…"
            />
            <Button
              onClick={sendMessage}
              disabled={input.trim() === "" || collectionId === "__global__"}
            >
              Send
            </Button>
            </div>
          </footer>
        </div>
      </section>
    );
  }
