import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { AlertCircle, DollarSign, Activity, Wifi } from 'lucide-react';
import { motion } from 'framer-motion';
import * as signalR from '@microsoft/signalr';

interface OpenAIBalanceData {
  provider: string;
  balance?: number;
  balanceUnit?: string;
  monthlyUsage: number;
  isConnected: boolean;
  lastUpdated: string;
  updateType?: string;
}

interface OpenAIBalanceWidgetProps {
  className?: string;
}

export const OpenAIBalanceWidget: React.FC<OpenAIBalanceWidgetProps> = ({ className }) => {
  const [balanceData, setBalanceData] = useState<OpenAIBalanceData | null>(null);
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
        newConnection.invoke('RequestProviderData', 'OpenAI');
      });

      newConnection.onclose(() => {
        console.log('SignalR: Connection closed');
        setConnectionStatus('disconnected');
      });

      // Listen for OpenAI balance updates
      newConnection.on('OpenAIBalanceUpdate', (data: OpenAIBalanceData) => {
        console.log('OpenAI Balance Update:', data);
        setBalanceData(data);
        setError(null);
        setIsLoading(false);
      });

      // Listen for OpenAI balance errors
      newConnection.on('OpenAIBalanceError', (errorData: { error: string, timestamp: string }) => {
        console.error('OpenAI Balance Error:', errorData);
        setError(errorData.error);
        setIsLoading(false);
      });

      // Listen for provider data updates (from direct requests)
      newConnection.on('ProviderDataUpdate', (data: { Provider: string, Data: any, Timestamp: string }) => {
        if (data.Provider.toLowerCase() === 'openai') {
          console.log('Provider Data Update (OpenAI):', data);
          setBalanceData({
            provider: data.Provider,
            balance: data.Data.balance,
            balanceUnit: data.Data.balanceUnit,
            monthlyUsage: data.Data.monthlyUsage,
            isConnected: data.Data.isConnected,
            lastUpdated: data.Timestamp,
          });
          setError(null);
          setIsLoading(false);
        }
      });

      // Listen for provider data errors
      newConnection.on('ProviderDataError', (errorData: { Provider: string, Error: string, Timestamp: string }) => {
        if (errorData.Provider.toLowerCase() === 'openai') {
          console.error('Provider Data Error (OpenAI):', errorData);
          setError(errorData.Error);
          setIsLoading(false);
        }
      });

      try {
        setConnectionStatus('connecting');
        await newConnection.start();
        console.log('SignalR: Connected to analytics hub');
        setConnectionStatus('connected');
        
        // Join analytics group and request OpenAI data
        await newConnection.invoke('JoinAnalyticsGroup');
        await newConnection.invoke('RequestProviderData', 'OpenAI');
        
        setConnection(newConnection);
      } catch (error) {
        console.error('SignalR: Connection error:', error);
        setConnectionStatus('disconnected');
        setError('Failed to connect to real-time updates');
        
        // Fallback to REST API
        fetchOpenAIDataFallback();
      }
    };

    const fetchOpenAIDataFallback = async () => {
      try {
        const response = await fetch('/api/analytics/providers/accounts');
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const accounts = await response.json();
        const openAIAccount = accounts.find((acc: any) => 
          acc.provider?.toLowerCase() === 'openai'
        );
        
        if (openAIAccount) {
          setBalanceData({
            provider: 'OpenAI',
            balance: openAIAccount.balance,
            balanceUnit: openAIAccount.balanceUnit || 'USD',
            monthlyUsage: openAIAccount.monthlyUsage || 0,
            isConnected: openAIAccount.isConnected || false,
            lastUpdated: new Date().toISOString(),
          });
          setError(null);
        } else {
          setError('OpenAI account not found or not configured');
        }
      } catch (error) {
        console.error('Failed to fetch OpenAI data:', error);
        setError('Failed to fetch OpenAI balance data');
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

  const getUsagePercentage = () => {
    if (!balanceData?.balance || balanceData.balance === 0) return 0;
    return Math.min((balanceData.monthlyUsage / balanceData.balance) * 100, 100);
  };

  const getStatusColor = () => {
    if (!balanceData?.isConnected) return 'bg-gray-500';
    const usage = getUsagePercentage();
    if (usage > 80) return 'bg-red-500';
    if (usage > 60) return 'bg-yellow-500';
    return 'bg-green-500';
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

  if (isLoading) {
    return (
      <Card className={className}>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">OpenAI Balance</CardTitle>
          <DollarSign className="h-4 w-4 text-muted-foreground" />
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
          <CardTitle className="text-sm font-medium">OpenAI Balance</CardTitle>
          <DollarSign className="h-4 w-4 text-muted-foreground" />
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
          <CardTitle className="text-sm font-medium">OpenAI Balance</CardTitle>
          <DollarSign className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">No OpenAI data available</p>
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
            <CardTitle className="text-sm font-medium">OpenAI Balance</CardTitle>
            {getConnectionIcon()}
          </div>
          <DollarSign className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {/* Current Balance */}
            <div>
              <div className="text-2xl font-bold">
                {formatCurrency(balanceData.balance, balanceData.balanceUnit)}
              </div>
              <div className="flex items-center space-x-2">
                <p className="text-xs text-muted-foreground">
                  Available balance
                </p>
                <Badge 
                  variant={balanceData.isConnected ? "default" : "secondary"}
                  className="text-xs"
                >
                  {balanceData.isConnected ? 'Connected' : 'Disconnected'}
                </Badge>
              </div>
            </div>

            {/* Monthly Usage */}
            <div>
              <div className="flex justify-between items-center mb-1">
                <span className="text-sm text-muted-foreground">Monthly Usage</span>
                <span className="text-sm font-medium">
                  {formatCurrency(balanceData.monthlyUsage, balanceData.balanceUnit)}
                </span>
              </div>
              <Progress 
                value={getUsagePercentage()} 
                className="h-2"
              />
              <p className="text-xs text-muted-foreground mt-1">
                {getUsagePercentage().toFixed(1)}% of available balance
              </p>
            </div>

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