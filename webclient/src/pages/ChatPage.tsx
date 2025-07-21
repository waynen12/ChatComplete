import { useEffect, useRef, useState } from "react";
import { useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
    initialKnowledgeId ?? ""
  );
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [conversationId, setConversationId] = useState<string | null>(() =>
    sessionStorage.getItem("chat.cid")
  );
  const [provider, setProvider] = useState<Provider>("OpenAi");
  const scrollRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    (async () => {
      const res = await fetch("/api/chat");
      if (!res.ok) return;
      setCollections(await res.json());
    })();
  }, []);

  useEffect(() => {
    scrollRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  async function sendMessage() {
    if (!input.trim()) return;
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
        knowledgeId: collectionId || null,   // ""  → null
        message: userMsg.content,
        temperature: 0.8,
        stripMarkdown: false,
        useExtendedInstructions: false,
        provider,
        conversationId
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
      <section className="h-full grid grid-rows-[auto_1fr_auto]">
        {/* Top bar */}
        <header className="border-b p-4 flex gap-4 items-center">
          {collections.length > 0 ? (
            <Select
              value={collectionId}
              onValueChange={(v) => setCollectionId(v)}   // v is always a string
            >
              <SelectTrigger className="w-64">
                {collectionId
                  ? collections.find((c) => c.id === collectionId)?.name ?? "Unknown"
                  : "🌐 Global chat"}
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">🌐 Global chat</SelectItem>
                {collections.map((c) => (
                  <SelectItem key={c.id} value={c.id}>
                    {c.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          ) : (
            <span className="text-sm text-muted-foreground">🌐 Global chat</span>
          )}
          {/* ▼ provider picker – small, after the knowledge dropdown */}
          <Select value={provider} onValueChange={(v) => setProvider(v as Provider)}>
            <SelectTrigger className="w-40" />
            <SelectContent>
              <SelectItem value="OpenAi">OpenAI</SelectItem>
              <SelectItem value="Google">Gemini</SelectItem>
              <SelectItem value="Anthropic">Anthropic</SelectItem>
              <SelectItem value="Ollama">Ollama</SelectItem>
            </SelectContent>
          </Select>
        </header>

        {/* Messages */}
        <div className="overflow-y-auto p-6 bg-slate-50 flex justify-center">
          <div className="w-full max-w-2xl space-y-2">
            <AnimatePresence initial={false}>
              {messages.map((m) => (
                <motion.div
                  className={clsx(
                    // limit bubble width
                    "max-w-[90%] sm:max-w-xs md:max-w-md lg:max-w-lg",
                    "rounded-2xl px-4 py-2",
                    m.role === "user"
                      ? "bg-primary text-primary-foreground ml-auto"
                      : "bg-white shadow"
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
        <footer className="px-70  border-t flex gap-2">
          <Textarea
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && sendMessage()}
            placeholder="Type your question…"
          />
          <Button
            onClick={sendMessage}
            disabled={input.trim() === ""}        // ← disables on empty/whitespace
          >
            Send
          </Button>
        </footer>
      </section>
    );
  }
