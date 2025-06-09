// src/types/api.ts
export interface ChatResponseDto {
  id: string | null;
  reply: string;
}

export interface KnowledgeSummaryDto {
  id: string;
  name: string;
  documentCount: number;
}
// add others as you need them
