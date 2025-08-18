export interface OllamaModelDetails {
  name: string;
  displayName?: string;
  size: number;
  family?: string;
  parameterSize?: string;
  quantizationLevel?: string;
  format?: string;
  template?: string;
  parameters?: string;
  modifiedAt?: string;
  installedAt: string;
  lastUsedAt?: string;
  isAvailable: boolean;
  status: string;
}

export interface OllamaDownloadStatus {
  modelName: string;
  status: string;
  bytesDownloaded: number;
  totalBytes: number;
  percentComplete: number;
  errorMessage?: string;
  startedAt: string;
  completedAt?: string;
}

export interface DownloadProgressEvent {
  model: string;
  status: string;
  bytesDownloaded: number;
  totalBytes: number;
  percentComplete: number;
  errorMessage?: string;
  timestamp: string;
}

export interface DownloadCompleteEvent {
  model: string;
  status: 'completed' | 'failed' | 'cancelled' | 'not_found';
  finalProgress?: number;
  errorMessage?: string;
  message?: string;
}

export interface DownloadErrorEvent {
  model: string;
  error: string;
}