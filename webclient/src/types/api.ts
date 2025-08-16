// src/types/api.ts
export interface ChatResponseDto {
  reply: string;
  conversationId: string;
}

export interface KnowledgeSummaryDto {
  id: string;
  name: string;
  documentCount: number;
}

/**
 * Response from /api/ollama/models endpoint
 */
export type OllamaModelsResponse = string[];

/**
 * Knowledge base item structure
 */
export interface KnowledgeItem {
  id: string;
  name: string;
}

// add others as you need them
