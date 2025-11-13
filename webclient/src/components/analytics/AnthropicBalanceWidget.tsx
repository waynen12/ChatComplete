import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { AlertCircle, Activity, Wifi, TrendingUp, Zap } from 'lucide-react';
import AnthropicIcon from '@/components/icons/AnthropicIcon';
import { motion } from 'framer-motion';
import * as signalR from '@microsoft/signalr';

interface AnthropicUsageData {
  provider: string;
  isConnected: boolean;
  lastUpdated: string;
  updateType?: string;
  // Detailed usage and cost data
  totalCost?: number;
  totalRequests?: number;
  totalTokens?: number;
  totalInputTokens?: number;
  totalOutputTokens?: number;
  webSearchRequests?: number;
  uniqueModels?: number;
  hasAdminKey?: boolean;
  billingAccess?: boolean;
  // Model breakdown
  modelBreakdown?: Array<{
    modelName: string;
    requests: number;
    inputTokens: number;
    outputTokens: number;
    cost: number;
  }>;
  // Date range
  startDate?: string;
  endDate?: string;
}

interface AnthropicBalanceWidgetProps {
  className?: string;
}

export const AnthropicBalanceWidget: React.FC<AnthropicBalanceWidgetProps> = ({ className }) => {
  const [usageData, setUsageData] = useState<AnthropicUsageData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [connectionStatus, setConnectionStatus] = useState<'disconnected' | 'connecting' | 'connected'>('disconnected');
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  useEffect(() => {
    const setupSignalRConnection = async () => {
      const hubUrl = `http://192.168.50.91:7040/api/analytics/hub`;
      
      const newConnection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          withCredentials: true,
          timeout: 30000, // 30 second timeout
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            // Exponential backoff: 0, 2, 10, 30 seconds, then every 30 seconds
            if (retryContext.previousRetryCount === 0) {
              return 0;
            } else if (retryContext.previousRetryCount === 1) {
              return 2000;
            } else if (retryContext.previousRetryCount === 2) {
              return 10000;
            } else {
              return 30000;
            }
          }
        })
        .build();

      newConnection.onreconnecting(() => {
        console.log('SignalR: Reconnecting...');
        setConnectionStatus('connecting');
      });

      newConnection.onreconnected(() => {
        console.log('SignalR: Reconnected');
        setConnectionStatus('connected');
      });

      newConnection.onclose(() => {
        console.log('SignalR: Connection closed');
        setConnectionStatus('disconnected');
      });

      // Listen for Anthropic usage updates
      newConnection.on('AnthropicUsageUpdate', (data: AnthropicUsageData) => {
        console.log('Received Anthropic usage update:', data);
        setUsageData(data);
        setIsLoading(false);
        setError(null);
      });

      // Listen for provider errors
      newConnection.on('ProviderError', (provider: string, errorMessage: string) => {
        if (provider === 'Anthropic') {
          console.error('Anthropic provider error:', errorMessage);
          setError(errorMessage);
          setIsLoading(false);
        }
      });

      try {
        // Add connection timeout
        const connectionPromise = newConnection.start();
        const connectionTimeout = new Promise((_, reject) => {
          setTimeout(() => reject(new Error('Connection timeout')), 10000);
        });
        
        await Promise.race([connectionPromise, connectionTimeout]);
        
        console.log('SignalR connection established for Anthropic widget');
        setConnectionStatus('connected');
        setConnection(newConnection);

        // Request initial data with timeout
        const requestPromise = newConnection.invoke('RequestProviderUpdate', 'Anthropic');
        const requestTimeout = new Promise((_, reject) => {
          setTimeout(() => reject(new Error('Request timeout')), 5000);
        });
        
        await Promise.race([requestPromise, requestTimeout]);
      } catch (err) {
        console.error('Failed to start SignalR connection:', err);
        setConnectionStatus('disconnected');
        
        const errorMessage = err instanceof Error 
          ? err.message.includes('timeout') 
            ? 'Connection timeout - Check if backend is running' 
            : 'Backend not running - Start the API server to see live data'
          : 'Unknown connection error';
          
        setError(errorMessage);
        setIsLoading(false);
      }
    };

    setupSignalRConnection();

    // Fallback timeout to stop loading if connection fails
    const timeoutId = setTimeout(() => {
      setIsLoading(false);
      setError('Connection timeout - Backend may not be running');
    }, 10000);

    return () => {
      clearTimeout(timeoutId);
      if (connection) {
        connection.stop();
      }
    };
    // Connection object is intentionally not in dependencies to avoid reconnection loops
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const getConnectionIcon = () => {
    switch (connectionStatus) {
      case 'connected':
        return <Wifi className="h-3 w-3 text-green-500" />;
      case 'connecting':
        return <Activity className="h-3 w-3 text-yellow-500 animate-pulse" />;
      case 'disconnected':
        return <Wifi className="h-3 w-3 text-red-500" />;
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
      maximumFractionDigits: 4
    }).format(amount);
  };

  const formatTokens = (tokens: number) => {
    if (tokens >= 1000000) {
      return `${(tokens / 1000000).toFixed(1)}M`;
    } else if (tokens >= 1000) {
      return `${(tokens / 1000).toFixed(1)}K`;
    }
    return tokens.toString();
  };

  const formatDateRange = (startDate?: string, endDate?: string) => {
    if (!startDate || !endDate) return 'Last 30 days';
    
    const start = new Date(startDate);
    const end = new Date(endDate);
    const days = Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24));
    
    return `Last ${days} day${days !== 1 ? 's' : ''}`;
  };

  if (isLoading) {
    return (
      <Card className={className}>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Anthropic Usage</CardTitle>
          <AnthropicIcon className="h-4 w-4 text-muted-foreground" />
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
          <CardTitle className="text-sm font-medium">Anthropic Usage</CardTitle>
          <AnthropicIcon className="h-4 w-4 text-muted-foreground" />
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
          <CardTitle className="text-sm font-medium">Anthropic Usage</CardTitle>
          <AnthropicIcon className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">No Anthropic usage data available</p>
        </CardContent>
      </Card>
    );
  }

  if (!usageData.isConnected) {
    return (
      <Card className={className}>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Anthropic Usage</CardTitle>
          <AnthropicIcon className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-md">
            <p className="text-sm text-yellow-700">
              ⚠️ API Not Connected - Configure ANTHROPIC_API_KEY
            </p>
            <p className="text-xs text-yellow-600 mt-1">
              Admin API key (sk-ant-admin...) required for detailed usage data
            </p>
          </div>
        </CardContent>
      </Card>
    );
  }

  const topModel = usageData.modelBreakdown && usageData.modelBreakdown.length > 0 
    ? usageData.modelBreakdown.reduce((prev, current) => 
        (current.cost > prev.cost) ? current : prev
      )
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
            <CardTitle className="text-sm font-medium">Anthropic Usage</CardTitle>
            {getConnectionIcon()}
          </div>
          <AnthropicIcon className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {/* Key Metrics */}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1">
                <p className="text-sm font-medium">
                  {usageData.totalCost !== undefined ? formatCurrency(usageData.totalCost) : '$0.00'}
                </p>
                <p className="text-xs text-muted-foreground">Total Cost</p>
              </div>
              <div className="space-y-1">
                <p className="text-sm font-medium">
                  {formatTokens(usageData.totalTokens || 0)}
                </p>
                <p className="text-xs text-muted-foreground">Total Tokens</p>
              </div>
            </div>

            {/* Token Breakdown */}
            {(usageData.totalInputTokens || usageData.totalOutputTokens) && (
              <div className="space-y-2">
                <div className="flex justify-between text-xs">
                  <span>Input: {formatTokens(usageData.totalInputTokens || 0)}</span>
                  <span>Output: {formatTokens(usageData.totalOutputTokens || 0)}</span>
                </div>
                <Progress 
                  value={(usageData.totalOutputTokens || 0) / (usageData.totalTokens || 1) * 100} 
                  className="h-1"
                />
              </div>
            )}

            {/* Additional Stats */}
            <div className="grid grid-cols-2 gap-4 text-xs">
              <div className="flex items-center space-x-1">
                <Zap className="h-3 w-3 text-blue-500" />
                <span>{usageData.totalRequests || 0} requests</span>
              </div>
              {(usageData.webSearchRequests || 0) > 0 && (
                <div className="flex items-center space-x-1">
                  <TrendingUp className="h-3 w-3 text-green-500" />
                  <span>{usageData.webSearchRequests} searches</span>
                </div>
              )}
            </div>

            {/* Top Model */}
            {topModel && (
              <div className="p-2 bg-gray-50 rounded-md">
                <div className="flex justify-between items-center">
                  <span className="text-xs font-medium">{topModel.modelName}</span>
                  <Badge variant="secondary" className="text-xs">
                    {formatCurrency(topModel.cost)}
                  </Badge>
                </div>
                <div className="text-xs text-muted-foreground mt-1">
                  {topModel.requests} requests • {formatTokens(topModel.inputTokens + topModel.outputTokens)} tokens
                </div>
              </div>
            )}

            {/* Admin Key Status */}
            <div className="flex justify-between items-center text-xs">
              <span className="text-muted-foreground">
                {formatDateRange(usageData.startDate, usageData.endDate)}
              </span>
              {usageData.hasAdminKey && (
                <Badge variant="default" className="text-xs">
                  Admin API
                </Badge>
              )}
            </div>

            {/* No Admin Key Message */}
            {!usageData.hasAdminKey && (
              <div className="p-2 bg-blue-50 border border-blue-200 rounded-md">
                <p className="text-xs text-blue-700">
                  🔑 Limited data with regular API key
                </p>
                <p className="text-xs text-blue-600 mt-1">
                  Use Admin API key (sk-ant-admin...) for detailed billing data
                </p>
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </motion.div>
  );
};