import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { AlertCircle, Activity, Wifi } from 'lucide-react';
import GoogleAIIcon from '@/components/icons/GoogleAIIcon';
import { motion } from 'framer-motion';
import * as signalR from '@microsoft/signalr';

interface GoogleAIBalanceData {
  provider: string;
  balance?: number;
  balanceUnit?: string;
  monthlyUsage: number;
  isConnected: boolean;
  lastUpdated: string;
  updateType?: string;
}

interface GoogleAIBalanceWidgetProps {
  className?: string;
}

export const GoogleAIBalanceWidget: React.FC<GoogleAIBalanceWidgetProps> = ({ className }) => {
  const [balanceData, setBalanceData] = useState<GoogleAIBalanceData | null>(null);
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
        newConnection.invoke('RequestProviderData', 'Google AI');
      });

      newConnection.onclose(() => {
        console.log('SignalR: Connection closed');
        setConnectionStatus('disconnected');
      });

      // Listen for provider data updates (from direct requests)
      newConnection.on('ProviderDataUpdate', (data: { Provider: string; Data: { balance: number; balanceUnit: string; monthlyUsage: number; isConnected: boolean }; Timestamp: string }) => {
        if (data.Provider.toLowerCase().includes('google')) {
          console.log('Provider Data Update (Google AI):', data);
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
        if (errorData.Provider.toLowerCase().includes('google')) {
          console.error('Provider Data Error (Google AI):', errorData);
          setError(errorData.Error);
          setIsLoading(false);
        }
      });

      try {
        setConnectionStatus('connecting');
        await newConnection.start();
        console.log('SignalR: Connected to analytics hub');
        setConnectionStatus('connected');
        
        // Join analytics group and request Google AI data
        await newConnection.invoke('JoinAnalyticsGroup');
        await newConnection.invoke('RequestProviderData', 'Google AI');
        
        setConnection(newConnection);
      } catch (error) {
        console.error('SignalR: Connection error:', error);
        setConnectionStatus('disconnected');
        setError('Failed to connect to real-time updates');
        
        // Fallback to REST API
        fetchGoogleAIDataFallback();
      }
    };

    const fetchGoogleAIDataFallback = async () => {
      try {
        const response = await fetch('/api/analytics/providers/accounts');
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const accounts = await response.json();
        const googleAIAccount = accounts.find((acc: { provider?: string }) => 
          acc.provider?.toLowerCase().includes('google')
        );
        
        if (googleAIAccount) {
          setBalanceData({
            provider: 'Google AI',
            balance: googleAIAccount.balance,
            balanceUnit: googleAIAccount.balanceUnit || 'USD',
            monthlyUsage: googleAIAccount.monthlyUsage || 0,
            isConnected: googleAIAccount.isConnected || false,
            lastUpdated: new Date().toISOString(),
          });
          setError(null);
        } else {
          setError('Google AI account not found or not configured');
        }
      } catch (error) {
        console.error('Failed to fetch Google AI data:', error);
        setError('Failed to fetch Google AI balance data');
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
    // Connection object is intentionally not in dependencies to avoid reconnection loops
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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
          <CardTitle className="text-sm font-medium">Google AI Balance</CardTitle>
          <GoogleAIIcon className="h-4 w-4 text-muted-foreground" />
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
          <CardTitle className="text-sm font-medium">Google AI Balance</CardTitle>
          <GoogleAIIcon className="h-4 w-4 text-muted-foreground" />
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
          <CardTitle className="text-sm font-medium">Google AI Balance</CardTitle>
          <GoogleAIIcon className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">No Google AI data available</p>
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
            <CardTitle className="text-sm font-medium">Google AI Balance</CardTitle>
            {getConnectionIcon()}
          </div>
          <GoogleAIIcon className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {/* Current Balance */}
            <div>
              <div className="text-2xl font-bold">
                Not Available
              </div>
              <div className="flex items-center space-x-2">
                <p className="text-xs text-muted-foreground">
                  Billing data requires Cloud Console access
                </p>
                <Badge 
                  variant={balanceData.isConnected ? "default" : "secondary"}
                  className="text-xs"
                >
                  {balanceData.isConnected ? 'Connected' : 'Disconnected'}
                </Badge>
              </div>
            </div>

            {/* Cloud Console Message */}
            <div className="p-3 bg-muted border border-border rounded-md">
              <p className="text-sm text-foreground">
                ✅ API Connected - Billing data not available via API
              </p>
              <p className="text-xs text-muted-foreground mt-1">
                Google AI billing requires{' '}
                <a href="https://cloud.google.com/billing/docs" target="_blank" rel="noopener noreferrer" 
                   className="underline hover:text-primary">
                  Cloud Console access
                </a>{' '}
                and Cloud Billing API setup. Visit the{' '}
                <a href="https://console.cloud.google.com/billing" target="_blank" rel="noopener noreferrer" 
                   className="underline hover:text-primary">
                  Google Cloud Console
                </a>{' '}
                to view billing information.
              </p>
            </div>

            {/* Last Updated */}
            <div className="flex items-center justify-between">
              <p className="text-xs text-muted-foreground">
                Last updated: {new Date(balanceData.lastUpdated).toLocaleTimeString()}
              </p>
              <div className={`w-2 h-2 rounded-full ${balanceData.isConnected ? 'bg-primary' : 'bg-muted-foreground'}`}></div>
            </div>
          </div>
        </CardContent>
      </Card>
    </motion.div>
  );
};