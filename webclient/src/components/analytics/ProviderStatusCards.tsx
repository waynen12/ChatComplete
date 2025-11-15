import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { OpenAIIcon, AnthropicIcon, GoogleAIIcon, OllamaIcon, ProviderIcon } from "@/components/icons";

interface ProviderAccount {
  provider: string;
  isConnected: boolean;
  apiKeyConfigured: boolean;
  lastSyncAt?: string;
  balance?: number;
  balanceUnit?: string;
  monthlyUsage: number;
  errorMessage?: string;
}

interface ProviderUsage {
  provider: string;
  totalCost: number;
  period: {
    startDate: string;
    endDate: string;
    days: number;
  };
}

interface ProviderStatusCardsProps {
  accounts: ProviderAccount[];
  usage: ProviderUsage[];
  loading?: boolean;
  onRefresh?: () => void;
  tableView?: boolean;
}

export function ProviderStatusCards({ accounts, usage, loading, onRefresh, tableView = false }: ProviderStatusCardsProps) {
  const [refreshing, setRefreshing] = useState(false);
  const [lastUpdate, setLastUpdate] = useState<Date>(new Date());

  // Auto-refresh every 5 minutes
  useEffect(() => {
    const interval = setInterval(() => {
      setLastUpdate(new Date());
      onRefresh?.();
    }, 5 * 60 * 1000);

    return () => clearInterval(interval);
  }, [onRefresh]);

  const handleManualRefresh = async () => {
    setRefreshing(true);
    setLastUpdate(new Date());
    await onRefresh?.();
    setTimeout(() => setRefreshing(false), 1000);
  };

  const getProviderIcon = (provider: string) => {
    const iconClass = "h-4 w-4";
    switch (provider) {
      case 'OpenAi':
        return <OpenAIIcon className={iconClass} />;
      case 'Anthropic':
        return <AnthropicIcon className={iconClass} />;
      case 'Google':
        return <GoogleAIIcon className={iconClass} />;
      case 'Ollama':
        return <OllamaIcon className={iconClass} />;
      default:
        return <ProviderIcon className={iconClass} />;
    }
  };


  const formatBalance = (balance?: number, unit?: string): string => {
    if (balance === undefined || balance === null) return 'Unknown';
    if (unit === 'USD') return `$${balance.toFixed(2)}`;
    if (unit === 'credits') return `${balance.toLocaleString()} credits`;
    return `${balance} ${unit || ''}`;
  };

  const getUsageForProvider = (provider: string): number => {
    return usage.find(u => u.provider === provider)?.totalCost || 0;
  };

  if (loading) {
    return (
      <div className="space-y-4">
        <div className="flex justify-between items-center">
          <h3 className="text-lg font-semibold">Provider Status</h3>
          <div className="animate-pulse h-8 w-20 bg-gray-200 rounded"></div>
        </div>
        <div className="grid gap-4 md:grid-cols-2">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="h-32 bg-gray-200 rounded animate-pulse"></div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <div>
          <h3 className="text-lg font-semibold">Provider Status</h3>
          <p className="text-sm text-muted-foreground">
            Last updated: {lastUpdate.toLocaleTimeString()} 
            <Button
              variant="ghost"
              size="sm"
              onClick={handleManualRefresh}
              disabled={refreshing}
              className="ml-2 h-6 px-2 text-xs"
            >
              {refreshing ? 'Refreshing...' : 'Refresh'}
            </Button>
          </p>
        </div>
      </div>

      {tableView ? (
        <div className="overflow-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="text-left p-2">Provider</th>
                <th className="text-left p-2">Status</th>
                <th className="text-left p-2">Type</th>
                <th className="text-left p-2">Balance</th>
                <th className="text-left p-2">This Month</th>
                <th className="text-left p-2">Last Sync</th>
              </tr>
            </thead>
            <tbody>
              {['OpenAi', 'Anthropic', 'Google', 'Ollama'].map(provider => {
                const account = accounts.find(a => a.provider === provider);
                const monthlyCost = getUsageForProvider(provider);
                const isConnected = account?.isConnected ?? false;
                const hasError = !!account?.errorMessage;

                return (
                  <tr key={provider} className="border-b">
                    <td className="p-2 font-medium">{provider}</td>
                    <td className="p-2">
                      <div className="flex items-center gap-2">
                        <div className={`w-3 h-3 rounded-full ${
                          hasError ? 'bg-red-500' : 
                          isConnected ? 'bg-green-500' : 
                          'bg-gray-400'
                        }`}></div>
                        <span>{hasError ? 'Error' : isConnected ? 'Active' : 'Inactive'}</span>
                      </div>
                    </td>
                    <td className="p-2">{provider === 'Ollama' ? 'Local Models' : 'Cloud'}</td>
                    <td className="p-2">
                      {provider === 'Ollama' ? 'N/A' : 
                        isConnected ? formatBalance(account?.balance, account?.balanceUnit) : 'Not connected'}
                    </td>
                    <td className="p-2">
                      {provider === 'Ollama' ? (
                        <span className="text-green-600 font-medium">Free</span>
                      ) : (
                        `$${monthlyCost.toFixed(2)}`
                      )}
                    </td>
                    <td className="p-2 text-xs text-muted-foreground">
                      {account?.lastSyncAt ? new Date(account.lastSyncAt).toLocaleDateString() : 'N/A'}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {['OpenAi', 'Anthropic', 'Google', 'Ollama'].map(provider => {
            const account = accounts.find(a => a.provider === provider);
            const monthlyCost = getUsageForProvider(provider);
            const isConnected = account?.isConnected ?? false;
            const hasError = !!account?.errorMessage;

            return (
              <Card key={provider}>
                <CardHeader className="pb-2">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-2">
                      <div className={`w-3 h-3 rounded-full ${
                        hasError ? 'bg-red-500' : 
                        isConnected ? 'bg-green-500' : 
                        'bg-gray-400'
                      }`}></div>
                      <CardTitle className="text-base flex items-center space-x-2">
                        {getProviderIcon(provider)}
                        <span>{provider}</span>
                      </CardTitle>
                    </div>
                    <Badge variant={isConnected ? "default" : "secondary"}>
                      {hasError ? 'Error' : isConnected ? 'Active' : 'Inactive'}
                    </Badge>
                  </div>
                </CardHeader>
                <CardContent className="pt-0">
                  <div className="space-y-2 text-sm">
                    {provider === 'Ollama' ? (
                      <>
                        <div className="flex justify-between">
                          <span className="text-muted-foreground">Type:</span>
                          <span>Local Models</span>
                        </div>
                        <div className="flex justify-between">
                          <span className="text-muted-foreground">Cost:</span>
                          <span className="text-green-600 font-medium">Free</span>
                        </div>
                      </>
                    ) : (
                      <>
                        <div className="flex justify-between">
                          <span className="text-muted-foreground">Balance:</span>
                          <span className={!isConnected ? 'text-muted-foreground' : ''}>
                            {isConnected ? formatBalance(account?.balance, account?.balanceUnit) : 'Not connected'}
                          </span>
                        </div>
                        <div className="flex justify-between">
                          <span className="text-muted-foreground">This month:</span>
                          <span className="font-medium">
                            ${monthlyCost.toFixed(2)}
                          </span>
                        </div>
                      </>
                    )}
                    
                    {hasError && (
                      <div className="text-xs text-red-600 mt-2 p-2 bg-red-50 rounded border">
                        {account?.errorMessage}
                      </div>
                    )}
                    
                    {account?.lastSyncAt && (
                      <div className="flex justify-between text-xs text-muted-foreground">
                        <span>Last sync:</span>
                        <span>{new Date(account.lastSyncAt).toLocaleDateString()}</span>
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}