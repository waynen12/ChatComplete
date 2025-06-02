import { useEffect, useRef, useState } from "react";
import { useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger } from "@/components/ui/select";
import { motion, AnimatePresence } from "framer-motion";
import clsx from "clsx";

interface Message {
  id: string;
  role: "user" | "assistant";
  content: string;
}

interface KnowledgeItem {
  id: string;
  name: string;
}

export default function ChatPage() {
  const { id: initialKnowledgeId } = useParams();
  const [collections, setCollections] = useState<KnowledgeItem[]>([]);
  const [collectionId, setCollectionId] = useState<string>(
  initialKnowledgeId ?? ""
  );
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const scrollRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    (async () => {
      const res = await fetch("/api/knowledge");
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
      }),
    });

    const assistantText = await res.text();
    setMessages((m) => [
      ...m,
      {
        id: crypto.randomUUID(),
        role: "assistant",
        content: assistantText,
      },
    ]);
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
      </header>

      {/* Messages */}
     <div className="overflow-y-auto p-6 bg-slate-50 flex justify-center">
       <div className="w-full max-w-2xl space-y-2">
         <AnimatePresence initial={false}>
            {messages.map((m) => (
              <motion.div
                key={m.id}
                initial={{ opacity: 0, scale: 0.95, y: 4 }}
                animate={{ opacity: 1, scale: 1, y: 0 }}
                transition={{ duration: 0.12 }}
                className={`max-w-prose rounded-2xl px-4 py-2 ${
                  m.role === "user"
                    ? "bg-primary text-primary-foreground ml-auto"
                    : "bg-white shadow"
                }`}
              >
                {m.content}
              </motion.div>
            ))}
         </AnimatePresence>
         <div ref={scrollRef} />
       </div>
      </div>

      {/* Input */}
      <footer className="p-4 border-t flex gap-2">
        <Input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && sendMessage()}
          placeholder="Type your question…"
        />
        <Button onClick={sendMessage}>Send</Button>
      </footer>
    </section>
  );
}
