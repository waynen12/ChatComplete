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
// add others as you need them
