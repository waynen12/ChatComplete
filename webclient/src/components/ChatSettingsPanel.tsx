import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger
} from "@/components/ui/select";
import { motion } from "framer-motion";
import { AI_PROVIDERS, GLOBAL_KNOWLEDGE_ID, type Provider } from "@/constants/app";
import { OllamaModelManager } from "@/components/OllamaModelManager";
import type { KnowledgeItem } from "@/types/api";

interface ChatSettingsPanelProps {
  // Panel state
  isOpen: boolean;
  onClose: () => void;
  
  // Knowledge base selection
  collections: KnowledgeItem[];
  selectedCollectionId: string;
  onCollectionChange: (collectionId: string) => void;
  
  // Provider selection
  provider: Provider;
  onProviderChange: (provider: Provider) => void;
  
  // Ollama model selection
  availableOllamaModels: string[];
  selectedOllamaModel: string;
  onOllamaModelChange: (model: string) => void;
  onModelsRefresh: () => void;
  loadingModels: boolean;
  
  // Markdown settings
  stripMarkdown: boolean;
  onStripMarkdownChange: (strip: boolean) => void;
  
  // Agent mode settings
  useAgent: boolean;
  onAgentModeChange: (useAgent: boolean) => void;
}

export function ChatSettingsPanel({
  isOpen,
  onClose,
  collections,
  selectedCollectionId,
  onCollectionChange,
  provider,
  onProviderChange,
  availableOllamaModels,
  selectedOllamaModel,
  onOllamaModelChange,
  onModelsRefresh,
  loadingModels,
  stripMarkdown,
  onStripMarkdownChange,
  useAgent,
  onAgentModeChange,
}: ChatSettingsPanelProps) {
  if (!isOpen) return null;

  return (
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
            onClick={onClose}
            className="h-8 w-8 p-0"
          >
            ✕
          </Button>
        </div>

        {/* Agent Mode Toggle - only shown when no knowledge base is selected */}
        {selectedCollectionId === GLOBAL_KNOWLEDGE_ID && (
          <div className="space-y-3 p-4 bg-blue-50 dark:bg-blue-950/20 border border-blue-200 dark:border-blue-800 rounded-lg">
            <div className="flex items-center space-x-2">
              <span className="text-lg">🤖</span>
              <div>
                <h4 className="text-sm font-semibold text-blue-900 dark:text-blue-100">
                  Agent Testing Mode
                </h4>
                <p className="text-xs text-blue-700 dark:text-blue-300">
                  Test AI agents without selecting a knowledge base
                </p>
              </div>
            </div>
            <div className="flex items-center space-x-2">
              <input
                type="checkbox"
                id="agent-mode"
                checked={useAgent}
                onChange={(e) => onAgentModeChange(e.target.checked)}
                className="w-4 h-4 text-blue-600 bg-background border-blue-300 rounded focus:ring-blue-500 focus:ring-2"
              />
              <label htmlFor="agent-mode" className="text-sm text-blue-900 dark:text-blue-100 cursor-pointer">
                Enable Agent Mode
              </label>
            </div>
            {useAgent && (
              <div className="text-xs text-blue-700 dark:text-blue-300 space-y-1">
                <p><strong>Try asking:</strong></p>
                <ul className="list-disc list-inside space-y-0.5 ml-2">
                  <li>"What are the most popular models?"</li>
                  <li>"Compare gpt-4o and claude-3-sonnet"</li>
                  <li>"Show me performance analysis for gpt-4o"</li>
                  <li>"Which model has the best success rate?"</li>
                </ul>
              </div>
            )}
          </div>
        )}

        {/* Knowledge Selection */}
        <div className="space-y-2">
          <label className="text-sm font-medium text-foreground">
            Knowledge Base {selectedCollectionId === GLOBAL_KNOWLEDGE_ID && !useAgent && (
              <span className="text-destructive">*</span>
            )}
          </label>
          {collections.length > 0 ? (
            <Select
              value={selectedCollectionId}
              onValueChange={onCollectionChange}
            >
              <SelectTrigger className="w-full">
                <span className="truncate">
                  {selectedCollectionId === GLOBAL_KNOWLEDGE_ID
                    ? useAgent ? "Agent Mode (No Knowledge Base)" : "Please choose a knowledge item"
                    : collections.find((c) => c.id === selectedCollectionId)?.name ?? "Unknown"}
                </span>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={GLOBAL_KNOWLEDGE_ID}>
                  {useAgent ? "Agent Mode (No Knowledge Base)" : "Please choose a knowledge item"}
                </SelectItem>
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
          <Select value={provider} onValueChange={(v) => onProviderChange(v as Provider)}>
            <SelectTrigger className="w-full">
              {provider === AI_PROVIDERS.OPENAI ? "OpenAI" :
               provider === AI_PROVIDERS.GOOGLE ? "Gemini" :
               provider === AI_PROVIDERS.ANTHROPIC ? "Anthropic" :
               provider === AI_PROVIDERS.OLLAMA ? "Ollama" : "Unknown"}
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={AI_PROVIDERS.OPENAI}>OpenAI</SelectItem>
              <SelectItem value={AI_PROVIDERS.GOOGLE}>Gemini</SelectItem>
              <SelectItem value={AI_PROVIDERS.ANTHROPIC}>Anthropic</SelectItem>
              <SelectItem value={AI_PROVIDERS.OLLAMA}>Ollama</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Ollama Model Selection - only shown when Ollama is selected */}
        {provider === AI_PROVIDERS.OLLAMA && (
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <label className="text-sm font-medium text-foreground">
                Ollama Model
                {availableOllamaModels.length === 0 && !loadingModels && (
                  <span className="text-destructive ml-1">*</span>
                )}
              </label>
              <OllamaModelManager
                availableModels={availableOllamaModels}
                selectedModel={selectedOllamaModel}
                onModelSelect={onOllamaModelChange}
                onModelsRefresh={onModelsRefresh}
                isLoadingModels={loadingModels}
              />
            </div>
            <Select 
              value={selectedOllamaModel} 
              onValueChange={onOllamaModelChange}
              disabled={loadingModels || availableOllamaModels.length === 0}
            >
              <SelectTrigger className="w-full">
                {loadingModels ? "Loading models..." : 
                 availableOllamaModels.length === 0 ? "No models available" :
                 selectedOllamaModel || "Select a model"}
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
                No models available. Use the model manager to download new models.
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
              onChange={(e) => onStripMarkdownChange(e.target.checked)}
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
  );
}