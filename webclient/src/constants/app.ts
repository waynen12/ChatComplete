/**
 * Application constants
 */

/**
 * Special knowledge collection ID for global/no-knowledge state
 */
export const GLOBAL_KNOWLEDGE_ID = "__global__" as const;

/**
 * AI Provider constants
 */
export const AI_PROVIDERS = {
  OPENAI: "OpenAi",
  GOOGLE: "Google", 
  ANTHROPIC: "Anthropic",
  OLLAMA: "Ollama"
} as const;

export type Provider = typeof AI_PROVIDERS[keyof typeof AI_PROVIDERS];