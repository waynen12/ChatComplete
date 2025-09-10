import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { AlertCircle, Activity, Server, HardDrive, Clock, Zap, Download } from 'lucide-react';
import { motion } from 'framer-motion';

interface OllamaUsageData {
  provider: string;
  isConnected: boolean;
  totalModels: number;
  totalDiskSpaceBytes: number;
  totalRequests: number;
  totalTokens: number;
  averageResponseTimeMs: number;
  successRate: number;
  toolEnabledModels: number;
  lastUpdated: string;
  topModels: Array<{
    modelName: string;
    requests: number;
    totalTokens: number;
    averageResponseTimeMs: number;
    sizeBytes: number;
    supportsTools: boolean;
    lastUsed: string;
  }>;
  recentDownloads: {
    pendingDownloads: number;
    completedToday: number;
    failedToday: number;
    recentDownloads: Array<{
      modelName: string;
      status: string;
      totalBytes: number;
      startedAt: string;
      completedAt?: string;
      errorMessage?: string;
    }>;
  };
  periodStart: string;
  periodEnd: string;
}

interface OllamaUsageWidgetProps {
  className?: string;
}

export const OllamaUsageWidget: React.FC<OllamaUsageWidgetProps> = ({ className }) => {
  const [usageData, setUsageData] = useState<OllamaUsageData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchOllamaUsage();
    // Refresh every 30 seconds
    const interval = setInterval(fetchOllamaUsage, 30000);
    return () => clearInterval(interval);
  }, []);

  const fetchOllamaUsage = async () => {
    try {
      const response = await fetch('/api/analytics/ollama/usage?days=30', {
        signal: AbortSignal.timeout(15000) // 15 second timeout
      });
      
      if (response.ok) {
        const data = await response.json();
        setUsageData(data);
        setError(null);
      } else {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
    } catch (err) {
      console.error('Failed to fetch Ollama usage data:', err);
      
      if (err instanceof Error) {
        if (err.name === 'AbortError') {
          setError('Request timeout - Check if Ollama is running');
        } else if (err.message.includes('Failed to fetch')) {
          setError('Backend not available - Start the API server');
        } else {
          setError('Failed to load Ollama data');
        }
      } else {
        setError('Unknown error occurred');
      }
    } finally {
      setIsLoading(false);
    }
  };

  const formatBytes = (bytes: number) => {
    const units = ['B', 'KB', 'MB', 'GB', 'TB'];
    let size = bytes;
    let unitIndex = 0;
    
    while (size >= 1024 && unitIndex < units.length - 1) {
      size /= 1024;
      unitIndex++;
    }
    
    return `${size.toFixed(1)} ${units[unitIndex]}`;
  };

  const formatNumber = (num: number) => {
    if (num >= 1000000) return `${(num / 1000000).toFixed(1)}M`;
    if (num >= 1000) return `${(num / 1000).toFixed(1)}K`;
    return num.toString();
  };

  const formatResponseTime = (ms: number) => {
    if (ms >= 1000) return `${(ms / 1000).toFixed(1)}s`;
    return `${Math.round(ms)}ms`;
  };

  if (isLoading) {
    return (
      <Card className={className}>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Ollama Usage</CardTitle>
          <Server className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="animate-pulse">
            <div className="h-8 bg-gray-200 rounded mb-2"></div>
            <div className="h-4 bg-gray-200 rounded w-2/3"></div>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className={className}>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Ollama Usage</CardTitle>
          <Server className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="flex items-center space-x-2 p-3 bg-red-50 border border-red-200 rounded-md">
            <AlertCircle className="h-4 w-4 text-red-500" />
            <span className="text-sm text-red-700">{error}</span>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!usageData) {
    return (
      <Card className={className}>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Ollama Usage</CardTitle>
          <Server className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">No Ollama usage data available</p>
        </CardContent>
      </Card>
    );
  }

  if (!usageData.isConnected) {
    return (
      <Card className={className}>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Ollama Usage</CardTitle>
          <Server className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-md">
            <p className="text-sm text-yellow-700">
              ⚠️ Ollama Not Available
            </p>
            <p className="text-xs text-yellow-600 mt-1">
              No models installed or Ollama not running
            </p>
          </div>
        </CardContent>
      </Card>
    );
  }

  const topModel = usageData.topModels && usageData.topModels.length > 0 
    ? usageData.topModels[0] 
    : null;

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
    >
      <Card className={className}>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <div className="flex items-center space-x-2">
            <CardTitle className="text-sm font-medium">Ollama Usage</CardTitle>
            <Activity className="h-3 w-3 text-green-500" />
          </div>
          <Server className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {/* Key Metrics */}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1">
                <p className="text-sm font-medium">
                  {usageData.totalModels}
                </p>
                <p className="text-xs text-muted-foreground">Models Installed</p>
              </div>
              <div className="space-y-1">
                <p className="text-sm font-medium">
                  {formatBytes(usageData.totalDiskSpaceBytes)}
                </p>
                <p className="text-xs text-muted-foreground">Disk Usage</p>
              </div>
            </div>

            {/* Usage Stats */}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1">
                <p className="text-sm font-medium">
                  {formatNumber(usageData.totalRequests)}
                </p>
                <p className="text-xs text-muted-foreground">Requests</p>
              </div>
              <div className="space-y-1">
                <p className="text-sm font-medium">
                  {formatNumber(usageData.totalTokens)}
                </p>
                <p className="text-xs text-muted-foreground">Tokens</p>
              </div>
            </div>

            {/* Performance */}
            <div className="space-y-2">
              <div className="flex justify-between text-xs">
                <span>Avg Response: {formatResponseTime(usageData.averageResponseTimeMs)}</span>
                <span>Success: {(usageData.successRate * 100).toFixed(1)}%</span>
              </div>
              <Progress 
                value={usageData.successRate * 100} 
                className="h-1"
              />
            </div>

            {/* Tool Support */}
            <div className="flex items-center justify-between text-xs">
              <div className="flex items-center space-x-1">
                <Zap className="h-3 w-3 text-blue-500" />
                <span>{usageData.toolEnabledModels} tool-enabled</span>
              </div>
              {usageData.recentDownloads.pendingDownloads > 0 && (
                <div className="flex items-center space-x-1">
                  <Download className="h-3 w-3 text-orange-500" />
                  <span>{usageData.recentDownloads.pendingDownloads} downloading</span>
                </div>
              )}
            </div>

            {/* Top Model */}
            {topModel && (
              <div className="p-2 bg-gray-50 rounded-md">
                <div className="flex justify-between items-center">
                  <span className="text-xs font-medium">{topModel.modelName}</span>
                  <div className="flex space-x-1">
                    {topModel.supportsTools && (
                      <Badge variant="secondary" className="text-xs">
                        Tools
                      </Badge>
                    )}
                  </div>
                </div>
                <div className="text-xs text-muted-foreground mt-1 flex justify-between">
                  <span>{topModel.requests} requests</span>
                  <span>{formatBytes(topModel.sizeBytes)}</span>
                </div>
              </div>
            )}

            {/* Download Activity */}
            {(usageData.recentDownloads.completedToday > 0 || usageData.recentDownloads.failedToday > 0) && (
              <div className="p-2 bg-blue-50 rounded-md">
                <div className="text-xs font-medium text-blue-700 mb-1">Today's Downloads</div>
                <div className="flex justify-between text-xs text-blue-600">
                  <span>✓ {usageData.recentDownloads.completedToday} completed</span>
                  {usageData.recentDownloads.failedToday > 0 && (
                    <span>✗ {usageData.recentDownloads.failedToday} failed</span>
                  )}
                </div>
              </div>
            )}

            {/* Local Status */}
            <div className="flex justify-between items-center text-xs">
              <span className="text-muted-foreground">
                Last 30 days
              </span>
              <Badge variant="default" className="text-xs">
                Local
              </Badge>
            </div>
          </div>
        </CardContent>
      </Card>
    </motion.div>
  );
};