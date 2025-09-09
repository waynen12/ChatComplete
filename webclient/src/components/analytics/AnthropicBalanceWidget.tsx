import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';

import { AlertCircle, Activity, Wifi, DollarSign } from 'lucide-react';
import AnthropicIcon from '@/components/icons/AnthropicIcon';
import { motion } from 'framer-motion';
import * as signalR from '@microsoft/signalr';

interface AnthropicBalanceData {
  provider: string;
  balance?: number;
  balanceUnit?: string;
  monthlyUsage: number;
  isConnected: boolean;
  lastUpdated: string;
  updateType?: string;
  // Anthropic-specific fields
  totalCost?: number;
  totalRequests?: number;
  totalTokens?: number;
  hasAdminKey?: boolean;
  billingAccess?: boolean;
}

interface AnthropicBalanceWidgetProps {
  className?: string;
}

export const AnthropicBalanceWidget: React.FC<AnthropicBalanceWidgetProps> = ({ className }) => {
  const [balanceData, setBalanceData] = useState<AnthropicBalanceData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [connectionStatus, setConnectionStatus] = useState<'disconnected' | 'connecting' | 'connected'>('disconnected');
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  useEffect(() => {
    const setupSignalRConnection = async () => {
      const hubUrl = `${window.location.origin}/api/analytics/hub`;
      
      const newConnection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          withCredentials: true,
        })
        .withAutomaticReconnect()
        .build();

      newConnection.onreconnecting(() => {
        console.log('SignalR: Reconnecting...');
        setConnectionStatus('connecting');
      });

      newConnection.onreconnected(() => {
        console.log('SignalR: Reconnected');
        setConnectionStatus('connected');
        // Re-join analytics group and request initial data
        newConnection.invoke('JoinAnalyticsGroup');
        newConnection.invoke('RequestProviderData', 'Anthropic');
      });

      newConnection.onclose(() => {
        console.log('SignalR: Connection closed');
        setConnectionStatus('disconnected');
      });

      // Listen for provider data updates (from direct requests)
      newConnection.on('ProviderDataUpdate', (data: { Provider: string, Data: any, Timestamp: string }) => {
        if (data.Provider.toLowerCase().includes('anthropic')) {
          console.log('Provider Data Update (Anthropic):', data);
          setBalanceData({
            provider: data.Provider,
            balance: data.Data.balance,
            balanceUnit: data.Data.balanceUnit,
            monthlyUsage: data.Data.monthlyUsage,
            isConnected: data.Data.isConnected,
            lastUpdated: data.Timestamp,
            totalCost: data.Data.totalCost,
            totalRequests: data.Data.totalRequests,
            totalTokens: data.Data.totalTokens,
            hasAdminKey: data.Data.hasAdminKey,
            billingAccess: data.Data.billingAccess,
          });
          setError(null);
          setIsLoading(false);
        }
      });

      // Listen for provider data errors
      newConnection.on('ProviderDataError', (errorData: { Provider: string, Error: string, Timestamp: string }) => {
        if (errorData.Provider.toLowerCase().includes('anthropic')) {
          console.error('Provider Data Error (Anthropic):', errorData);
          setError(errorData.Error);
          setIsLoading(false);
        }
      });

      try {
        setConnectionStatus('connecting');
        await newConnection.start();
        console.log('SignalR: Connected to analytics hub');
        setConnectionStatus('connected');
        
        // Join analytics group and request Anthropic data
        await newConnection.invoke('JoinAnalyticsGroup');
        await newConnection.invoke('RequestProviderData', 'Anthropic');
        
        setConnection(newConnection);
      } catch (error) {
        console.error('SignalR: Connection error:', error);
        setConnectionStatus('disconnected');
        setError('Failed to connect to real-time updates');
        
        // Fallback to REST API
        fetchAnthropicDataFallback();
      }
    };

    const fetchAnthropicDataFallback = async () => {
      try {
        const response = await fetch('/api/analytics/providers/accounts');
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const accounts = await response.json();
        const anthropicAccount = accounts.find((acc: any) => 
          acc.provider?.toLowerCase().includes('anthropic')
        );
        
        if (anthropicAccount) {
          setBalanceData({
            provider: 'Anthropic',
            balance: anthropicAccount.balance,
            balanceUnit: anthropicAccount.balanceUnit || 'USD',
            monthlyUsage: anthropicAccount.monthlyUsage || 0,
            isConnected: anthropicAccount.isConnected || false,
            lastUpdated: new Date().toISOString(),
            hasAdminKey: anthropicAccount.additionalInfo?.admin_key,
            billingAccess: anthropicAccount.additionalInfo?.billing_access,
          });
          setError(null);
        } else {
          setError('Anthropic account not found or not configured');
        }
      } catch (error) {
        console.error('Failed to fetch Anthropic data:', error);
        setError('Failed to fetch Anthropic balance data');
      } finally {
        setIsLoading(false);
      }
    };

    setupSignalRConnection();

    return () => {
      if (connection) {
        connection.stop();
      }
    };
  }, []);

  const formatCurrency = (amount?: number, unit: string = 'USD') => {
    if (amount === undefined || amount === null) return 'N/A';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: unit,
      minimumFractionDigits: 2,
    }).format(amount);
  };

  const formatNumber = (num?: number) => {
    if (num === undefined || num === null) return '0';
    return num.toLocaleString();
  };

  const getConnectionIcon = () => {
    switch (connectionStatus) {
      case 'connected':
        return <Wifi className="h-3 w-3 text-green-500" />;
      case 'connecting':
        return <Activity className="h-3 w-3 text-yellow-500 animate-pulse" />;
      default:
        return <AlertCircle className="h-3 w-3 text-red-500" />;
    }
  };

  const getStatusColor = () => {
    if (!balanceData?.isConnected) return 'bg-gray-500';
    if (balanceData?.billingAccess) return 'bg-green-500';
    return 'bg-blue-500'; // Connected but no billing access
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
            <div className="h-4 bg-gray-300 rounded w-3/4 mb-2"></div>
            <div className="h-3 bg-gray-300 rounded w-1/2"></div>
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
            <p className="text-sm text-red-700">{error}</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!balanceData) {
    return (
      <Card className={className}>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Anthropic Usage</CardTitle>
          <AnthropicIcon className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">No Anthropic data available</p>
        </CardContent>
      </Card>
    );
  }

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
          <div className="space-y-3">
            {/* Connection Status */}
            <div className="flex items-center space-x-2">
              <Badge 
                variant={balanceData.isConnected ? "default" : "secondary"}
                className="text-xs"
              >
                {balanceData.isConnected ? 'Connected' : 'Disconnected'}
              </Badge>
              {balanceData.hasAdminKey && (
                <Badge variant="outline" className="text-xs">
                  Admin Key
                </Badge>
              )}
            </div>

            {/* Usage Data - Only show if we have billing access */}
            {balanceData.billingAccess && balanceData.hasAdminKey ? (
              <div className="space-y-3">
                {/* Total Cost */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <DollarSign className="h-4 w-4 text-green-600" />
                    <span className="text-sm font-medium">Total Cost</span>
                  </div>
                  <span className="text-lg font-bold">
                    {formatCurrency(balanceData.totalCost, balanceData.balanceUnit)}
                  </span>
                </div>

                {/* Usage Stats */}
                <div className="grid grid-cols-2 gap-4">
                  <div className="text-center">
                    <div className="text-lg font-semibold">{formatNumber(balanceData.totalRequests)}</div>
                    <div className="text-xs text-muted-foreground">Requests</div>
                  </div>
                  <div className="text-center">
                    <div className="text-lg font-semibold">{formatNumber(balanceData.totalTokens)}</div>
                    <div className="text-xs text-muted-foreground">Tokens</div>
                  </div>
                </div>

                {/* Full Billing Access Message */}
                <div className="p-3 bg-green-50 border border-green-200 rounded-md">
                  <p className="text-sm text-green-700">
                    ✅ Full Billing API Access
                  </p>
                  <p className="text-xs text-green-600 mt-1">
                    Admin API key provides complete usage and cost data via Anthropic's Usage & Cost API.
                  </p>
                </div>
              </div>
            ) : (
              /* No Billing Access Message */
              <div className="space-y-3">
                <div className="text-2xl font-bold text-muted-foreground">
                  Not Available
                </div>
                
                <div className="p-3 bg-blue-50 border border-blue-200 rounded-md">
                  <p className="text-sm text-blue-700">
                    ✅ API Connected - Billing data requires Admin API key
                  </p>
                  <p className="text-xs text-blue-600 mt-1">
                    Regular API keys can't access billing data. You need an Admin API key (starts with{' '}
                    <code className="bg-blue-100 px-1 rounded text-xs">sk-ant-admin</code>) from the{' '}
                    <a href="https://console.anthropic.com" target="_blank" rel="noopener noreferrer" 
                       className="underline hover:text-blue-800">
                      Anthropic Console
                    </a>.
                  </p>
                </div>
                
                <div className="p-3 bg-amber-50 border border-amber-200 rounded-md">
                  <p className="text-sm text-amber-700">
                    💡 How to get Admin API access:
                  </p>
                  <ul className="text-xs text-amber-600 mt-1 list-disc list-inside space-y-1">
                    <li>Log into the Anthropic Console</li>
                    <li>Navigate to API Keys section</li>
                    <li>Generate an Admin API key (organization admin role required)</li>
                    <li>Admin keys enable access to Usage & Cost API endpoints</li>
                  </ul>
                </div>
              </div>
            )}

            {/* Last Updated */}
            <div className="flex items-center justify-between">
              <p className="text-xs text-muted-foreground">
                Last updated: {new Date(balanceData.lastUpdated).toLocaleTimeString()}
              </p>
              <div className={`w-2 h-2 rounded-full ${getStatusColor()}`}></div>
            </div>
          </div>
        </CardContent>
      </Card>
    </motion.div>
  );
};