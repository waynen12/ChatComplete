import { useState, useEffect, useRef } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import { ScrollArea } from "@/components/ui/scroll-area";
import { 
  Dialog, 
  DialogContent, 
  DialogHeader, 
  DialogTitle, 
  DialogTrigger 
} from "@/components/ui/dialog";
import { 
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { 
  Download, 
  Trash2, 
  Search, 
  Settings,
  CheckCircle,
  XCircle,
  Clock,
  AlertCircle,
  MoreVertical,
  RefreshCw
} from "lucide-react";
import { cn } from "@/lib/utils";
import { notify } from "@/lib/notify";
import type { OllamaModelDetails, OllamaDownloadStatus } from "@/types/ollama";

interface OllamaModelManagerProps {
  availableModels: string[];
  selectedModel: string;
  onModelSelect: (model: string) => void;
  onModelsRefresh: () => void;
  isLoadingModels: boolean;
}

interface DownloadProgress {
  model: string;
  status: string;
  bytesDownloaded: number;
  totalBytes: number;
  percentComplete: number;
  errorMessage?: string;
  timestamp: string;
}

export function OllamaModelManager({
  selectedModel,
  onModelSelect,
  onModelsRefresh,
  isLoadingModels
}: OllamaModelManagerProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [modelDetails, setModelDetails] = useState<OllamaModelDetails[]>([]);
  const [downloadProgress, setDownloadProgress] = useState<Map<string, DownloadProgress>>(new Map());
  const [activeDownloads, setActiveDownloads] = useState<Set<string>>(new Set());
  const eventSourcesRef = useRef<Map<string, EventSource>>(new Map());

  // Fetch detailed model information when dialog opens
  useEffect(() => {
    if (isOpen) {
      fetchModelDetails();
      fetchActiveDownloads();
    }
    // fetchModelDetails and fetchActiveDownloads are stable functions defined below
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen]);

  // Cleanup event sources on unmount
  useEffect(() => {
    const eventSources = eventSourcesRef.current;
    return () => {
      eventSources.forEach(source => source.close());
      eventSources.clear();
    };
  }, []);

  const fetchModelDetails = async () => {
    try {
      const response = await fetch('/api/ollama/models/details');
      if (response.ok) {
        const details = await response.json();
        setModelDetails(details);
      } else {
        notify.error('Failed to fetch model details');
      }
    } catch {
      notify.error('Error fetching model details');
    }
  };

  const fetchActiveDownloads = async () => {
    try {
      const response = await fetch('/api/ollama/downloads');
      if (response.ok) {
        const downloads = await response.json();
        const activeModels = new Set<string>(downloads.map((d: OllamaDownloadStatus) => d.modelName));
        setActiveDownloads(activeModels);
        
        // Set up SSE streams for active downloads
        downloads.forEach((download: OllamaDownloadStatus) => {
          if (!eventSourcesRef.current.has(download.modelName)) {
            setupProgressStream(download.modelName);
          }
        });
      }
    } catch {
      notify.error('Error fetching active downloads');
    }
  };

  const setupProgressStream = (modelName: string) => {
    if (eventSourcesRef.current.has(modelName)) {
      return; // Already tracking this model
    }

    const eventSource = new EventSource(`/api/ollama/models/download/${encodeURIComponent(modelName)}/progress`);
    eventSourcesRef.current.set(modelName, eventSource);

    eventSource.onmessage = (event) => {
      const data = JSON.parse(event.data);
      
      setDownloadProgress(prev => {
        const newMap = new Map(prev);
        newMap.set(modelName, data);
        return newMap;
      });
    };

    eventSource.addEventListener('progress', (event) => {
      const data = JSON.parse(event.data);
      
      setDownloadProgress(prev => {
        const newMap = new Map(prev);
        newMap.set(modelName, data);
        return newMap;
      });
    });

    eventSource.addEventListener('complete', (event) => {
      const data = JSON.parse(event.data);
      
      setDownloadProgress(prev => {
        const newMap = new Map(prev);
        newMap.set(modelName, data);
        return newMap;
      });

      setActiveDownloads(prev => {
        const newSet = new Set(prev);
        newSet.delete(modelName);
        return newSet;
      });

      // Close the event source
      eventSource.close();
      eventSourcesRef.current.delete(modelName);

      // Refresh model list if download completed successfully
      if (data.status === 'completed') {
        onModelsRefresh();
        fetchModelDetails();
        notify.success(`Model ${modelName} downloaded successfully`);
      } else if (data.status === 'failed') {
        notify.error(`Failed to download ${modelName}: ${data.errorMessage || 'Unknown error'}`);
      }
    });

    eventSource.addEventListener('error', (event) => {
      const customEvent = event as MessageEvent;
      const data = JSON.parse(customEvent.data);
      notify.error(`Download error for ${modelName}: ${data.error}`);
      
      // Clean up
      eventSource.close();
      eventSourcesRef.current.delete(modelName);
      setActiveDownloads(prev => {
        const newSet = new Set(prev);
        newSet.delete(modelName);
        return newSet;
      });
    });

    eventSource.onerror = () => {
      // Connection error, clean up
      eventSource.close();
      eventSourcesRef.current.delete(modelName);
      setActiveDownloads(prev => {
        const newSet = new Set(prev);
        newSet.delete(modelName);
        return newSet;
      });
    };
  };

  const downloadModel = async (modelName: string) => {
    if (activeDownloads.has(modelName)) {
      notify.warning(`${modelName} is already being downloaded`);
      return;
    }

    try {
      const response = await fetch('/api/ollama/models/download', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ modelName })
      });

      if (response.ok) {
        setActiveDownloads(prev => new Set(prev).add(modelName));
        setupProgressStream(modelName);
        notify.success(`Started downloading ${modelName}`);
      } else {
        const error = await response.text();
        notify.error(`Failed to start download: ${error}`);
      }
    } catch (error) {
      notify.error(`Error starting download: ${error}`);
    }
  };

  const deleteModel = async (modelName: string) => {
    try {
      const response = await fetch(`/api/ollama/models/${encodeURIComponent(modelName)}`, {
        method: 'DELETE'
      });

      if (response.ok) {
        onModelsRefresh();
        fetchModelDetails();
        notify.success(`Model ${modelName} deleted successfully`);
        
        // If this was the selected model, clear the selection
        if (selectedModel === modelName) {
          onModelSelect('');
        }
      } else {
        const error = await response.text();
        notify.error(`Failed to delete model: ${error}`);
      }
    } catch (error) {
      notify.error(`Error deleting model: ${error}`);
    }
  };

  const cancelDownload = async (modelName: string) => {
    try {
      const response = await fetch(`/api/ollama/models/download/${encodeURIComponent(modelName)}`, {
        method: 'DELETE'
      });

      if (response.ok) {
        // Close event source
        const eventSource = eventSourcesRef.current.get(modelName);
        if (eventSource) {
          eventSource.close();
          eventSourcesRef.current.delete(modelName);
        }

        setActiveDownloads(prev => {
          const newSet = new Set(prev);
          newSet.delete(modelName);
          return newSet;
        });

        setDownloadProgress(prev => {
          const newMap = new Map(prev);
          newMap.delete(modelName);
          return newMap;
        });

        notify.success(`Download cancelled for ${modelName}`);
      } else {
        notify.error('Failed to cancel download');
      }
    } catch (error) {
      notify.error(`Error cancelling download: ${error}`);
    }
  };

  const filteredModels = modelDetails.filter(model =>
    model.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'completed':
      case 'ready':
        return <CheckCircle className="h-4 w-4 text-green-500" />;
      case 'failed':
      case 'error':
        return <XCircle className="h-4 w-4 text-red-500" />;
      case 'downloading':
      case 'in progress':
        return <Clock className="h-4 w-4 text-blue-500 animate-spin" />;
      default:
        return <AlertCircle className="h-4 w-4 text-yellow-500" />;
    }
  };

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm" className="ml-2">
          <Settings className="h-4 w-4" />
        </Button>
      </DialogTrigger>
      <DialogContent className="max-w-4xl max-h-[80vh]">
        <DialogHeader>
          <DialogTitle className="flex items-center justify-between">
            <span>Ollama Model Manager</span>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                onModelsRefresh();
                fetchModelDetails();
                fetchActiveDownloads();
              }}
              disabled={isLoadingModels}
            >
              <RefreshCw className={`h-4 w-4 ${isLoadingModels ? 'animate-spin' : ''}`} />
            </Button>
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-6">
          {/* Download New Model Section */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold">Download New Model</h3>
            <div className="flex gap-2">
              <div className="flex-1">
                <Label htmlFor="model-search">Model Name</Label>
                <Input
                  id="model-search"
                  placeholder="Enter model name (e.g., llama3.2:3b, mistral:7b)"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' && searchTerm.trim()) {
                      downloadModel(searchTerm.trim());
                      setSearchTerm('');
                    }
                  }}
                />
              </div>
              <Button
                onClick={() => {
                  if (searchTerm.trim()) {
                    downloadModel(searchTerm.trim());
                    setSearchTerm('');
                  }
                }}
                disabled={!searchTerm.trim()}
                className="mt-6"
              >
                <Download className="h-4 w-4 mr-2" />
                Download
              </Button>
            </div>
            <p className="text-sm text-muted-foreground">
              Visit <a href="https://ollama.com/library" target="_blank" rel="noopener noreferrer" className="text-blue-500 hover:underline">
                ollama.com/library
              </a> to browse available models.
            </p>
          </div>

          {/* Active Downloads */}
          {activeDownloads.size > 0 && (
            <div className="space-y-4">
              <h3 className="text-lg font-semibold">Active Downloads</h3>
              <div className="space-y-3">
                {Array.from(activeDownloads).map(modelName => {
                  const progress = downloadProgress.get(modelName);
                  return (
                    <motion.div
                      key={modelName}
                      initial={{ opacity: 0, y: 20 }}
                      animate={{ opacity: 1, y: 0 }}
                      exit={{ opacity: 0, y: -20 }}
                      className="border rounded-lg p-4 space-y-3"
                    >
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          {getStatusIcon(progress?.status || 'downloading')}
                          <span className="font-medium">{modelName}</span>
                          <Badge variant="secondary">{progress?.status || 'Starting'}</Badge>
                        </div>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => cancelDownload(modelName)}
                        >
                          <XCircle className="h-4 w-4" />
                        </Button>
                      </div>
                      
                      {progress && (
                        <div className="space-y-2">
                          <div className="flex justify-between text-sm">
                            <span>{Math.round(progress.percentComplete)}%</span>
                            <span>
                              {formatBytes(progress.bytesDownloaded)} / {formatBytes(progress.totalBytes)}
                            </span>
                          </div>
                          <Progress value={progress.percentComplete} className="h-2" />
                          {progress.errorMessage && (
                            <p className="text-sm text-red-500">{progress.errorMessage}</p>
                          )}
                        </div>
                      )}
                    </motion.div>
                  );
                })}
              </div>
            </div>
          )}

          {/* Installed Models */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold">Installed Models ({filteredModels.length})</h3>
            
            <div className="flex gap-2">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search installed models..."
                  className="pl-10"
                  onChange={(e) => setSearchTerm(e.target.value)}
                />
              </div>
            </div>

            <ScrollArea className="h-[300px]">
              <div className="space-y-2">
                <AnimatePresence>
                  {filteredModels.map(model => (
                    <motion.div
                      key={model.name}
                      initial={{ opacity: 0 }}
                      animate={{ opacity: 1 }}
                      exit={{ opacity: 0 }}
                      className={cn(
                        "border rounded-lg p-4 cursor-pointer transition-colors",
                        selectedModel === model.name
                          ? "border-blue-500 bg-blue-50 dark:bg-blue-950"
                          : "hover:bg-gray-50 dark:hover:bg-gray-800"
                      )}
                      onClick={() => onModelSelect(model.name)}
                    >
                      <div className="flex items-center justify-between">
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            {getStatusIcon(model.status)}
                            <span className="font-medium">{model.name}</span>
                            {selectedModel === model.name && (
                              <Badge variant="default">Selected</Badge>
                            )}
                          </div>
                          
                          <div className="mt-2 grid grid-cols-2 gap-4 text-sm text-muted-foreground">
                            <div>
                              <span className="font-medium">Size:</span> {formatBytes(model.size)}
                            </div>
                            <div>
                              <span className="font-medium">Family:</span> {model.family || 'Unknown'}
                            </div>
                            <div>
                              <span className="font-medium">Parameters:</span> {model.parameterSize || 'Unknown'}
                            </div>
                            <div>
                              <span className="font-medium">Modified:</span>{' '}
                              {model.modifiedAt ? new Date(model.modifiedAt).toLocaleDateString() : 'Unknown'}
                            </div>
                          </div>
                        </div>

                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="sm">
                              <MoreVertical className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem
                              onClick={(e) => {
                                e.stopPropagation();
                                onModelSelect(model.name);
                              }}
                            >
                              Select Model
                            </DropdownMenuItem>
                            <DropdownMenuItem
                              onClick={(e) => {
                                e.stopPropagation();
                                deleteModel(model.name);
                              }}
                              className="text-red-600"
                            >
                              <Trash2 className="h-4 w-4 mr-2" />
                              Delete
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </div>
                    </motion.div>
                  ))}
                </AnimatePresence>
              </div>
            </ScrollArea>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}