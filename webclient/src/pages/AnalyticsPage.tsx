import { useState, useEffect, useCallback } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { UsageTrendsChart } from "@/components/analytics/UsageTrendsChart";
import { CostBreakdownChart } from "@/components/analytics/CostBreakdownChart";
import { ProviderStatusCards } from "@/components/analytics/ProviderStatusCards";
import { PerformanceMetrics } from "@/components/analytics/PerformanceMetrics";
import { OpenAIBalanceWidget } from "@/components/analytics/OpenAIBalanceWidget";
import { AnthropicBalanceWidget } from "@/components/analytics/AnthropicBalanceWidget";
import { GoogleAIBalanceWidget } from "@/components/analytics/GoogleAIBalanceWidget";
import { OllamaUsageWidget } from "@/components/analytics/OllamaUsageWidget";

interface ModelUsageStats {
  modelName: string;
  provider: string;
  conversationCount: number;
  totalTokens: number;
  averageTokensPerRequest: number;
  averageResponseTime: number;
  lastUsed: string;
  supportsTools?: boolean;
  successfulRequests: number;
  failedRequests: number;
  successRate: number;
  totalRequests: number;
}

interface KnowledgeUsageStats {
  knowledgeId: string;
  knowledgeName: string;
  documentCount: number;
  chunkCount: number;
  conversationCount: number;
  queryCount: number;
  lastQueried: string;
  createdAt: string;
  vectorStore: string;
  totalFileSize: number;
}

export default function AnalyticsPage() {
  const [modelStats, setModelStats] = useState<ModelUsageStats[]>([]);
  const [knowledgeStats, setKnowledgeStats] = useState<KnowledgeUsageStats[]>([]);
  const [usageTrends, setUsageTrends] = useState<any[]>([]);
  const [costBreakdown, setCostBreakdown] = useState<any[]>([]);
  const [providerAccounts, setProviderAccounts] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [autoRefresh, setAutoRefresh] = useState(true);

  const fetchWithRetry = useCallback(async (url: string, maxRetries = 3): Promise<Response> => {
    let lastError: Error = new Error('Unknown error');
    
    for (let attempt = 0; attempt <= maxRetries; attempt++) {
      try {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 30000);
        
        const response = await fetch(url, { signal: controller.signal });
        clearTimeout(timeoutId);
        
        if (response.ok) {
          return response;
        }
        
        // If it's a client error (4xx), don't retry
        if (response.status >= 400 && response.status < 500) {
          throw new Error(`Client error: ${response.status} ${response.statusText}`);
        }
        
        throw new Error(`Server error: ${response.status} ${response.statusText}`);
      } catch (error) {
        lastError = error instanceof Error ? error : new Error('Unknown error');
        
        // Don't retry on timeout or client errors
        if (lastError.name === 'AbortError' || lastError.message.includes('Client error')) {
          throw lastError;
        }
        
        // If this isn't the last attempt, wait before retrying
        if (attempt < maxRetries) {
          const delay = Math.min(1000 * Math.pow(2, attempt), 10000); // Exponential backoff, max 10s
          console.log(`Request failed, retrying in ${delay}ms... (attempt ${attempt + 1}/${maxRetries})`);
          await new Promise(resolve => setTimeout(resolve, delay));
        }
      }
    }
    
    throw lastError;
  }, []);

  const fetchAnalytics = useCallback(async () => {
    try {
      setLoading(true);

      const [
        modelsResponse, 
        knowledgeResponse, 
        trendsResponse,
        costResponse,
        accountsResponse
      ] = await Promise.all([
        fetchWithRetry('/api/analytics/models'),
        fetchWithRetry('/api/analytics/knowledge-bases'),
        fetchWithRetry('/api/analytics/usage-trends?days=7'),
        fetchWithRetry('/api/analytics/cost-breakdown?days=30'),
        fetchWithRetry('/api/analytics/providers/accounts')
      ]);

      if (modelsResponse.ok) {
        const models = await modelsResponse.json();
        setModelStats(models);
      }

      if (knowledgeResponse.ok) {
        const knowledge = await knowledgeResponse.json();
        setKnowledgeStats(knowledge);
      }

      if (trendsResponse.ok) {
        const trends = await trendsResponse.json();
        setUsageTrends(trends);
      }

      if (costResponse.ok) {
        const cost = await costResponse.json();
        setCostBreakdown(cost);
      }

      if (accountsResponse.ok) {
        const accounts = await accountsResponse.json();
        setProviderAccounts(accounts);
      }
    } catch (error) {
      console.error('Failed to fetch analytics:', error);
      
      // Handle different types of errors
      if (error instanceof Error) {
        if (error.name === 'AbortError') {
          console.error('Analytics request timed out after 30 seconds');
          // Could add user notification here
        } else if (error.message.includes('Failed to fetch')) {
          console.error('Network error - backend may be unavailable');
        } else {
          console.error('Unexpected error:', error.message);
        }
      }
    } finally {
      setLoading(false);
    }
  }, [fetchWithRetry]);

  useEffect(() => {
    fetchAnalytics();
  }, [fetchAnalytics]);

  // Auto-refresh every 30 seconds if enabled
  useEffect(() => {
    if (!autoRefresh) return;
    
    const interval = setInterval(() => {
      fetchAnalytics();
    }, 30000);

    return () => clearInterval(interval);
  }, [fetchAnalytics, autoRefresh]);

  if (loading) {
    return (
      <div className="container mx-auto py-6">
        <div className="animate-pulse space-y-4">
          <div className="h-8 bg-gray-200 rounded w-1/4"></div>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="h-32 bg-gray-200 rounded"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  const totalConversations = modelStats.reduce((sum, model) => sum + model.conversationCount, 0);
  const totalTokens = modelStats.reduce((sum, model) => sum + model.totalTokens, 0);
  const activeProviders = [...new Set(modelStats.map(model => model.provider))].length;
  const totalKnowledgeBases = knowledgeStats.length;

  const formatNumber = (num: number): string => {
    if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M';
    if (num >= 1000) return (num / 1000).toFixed(1) + 'K';
    return num.toString();
  };

  const formatFileSize = (bytes: number): string => {
    const units = ['B', 'KB', 'MB', 'GB'];
    let size = bytes;
    let unitIndex = 0;
    while (size >= 1024 && unitIndex < units.length - 1) {
      size /= 1024;
      unitIndex++;
    }
    return `${size.toFixed(1)} ${units[unitIndex]}`;
  };

  const getProviderColor = (provider: string): string => {
    const colors: Record<string, string> = {
      'OpenAi': 'bg-green-500',
      'Anthropic': 'bg-orange-500', 
      'Google': 'bg-blue-500',
      'Ollama': 'bg-purple-500'
    };
    return colors[provider] || 'bg-gray-500';
  };

  return (
    <div className="container mx-auto py-6 space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Analytics Dashboard</h1>
          <p className="text-muted-foreground">
            Monitor your AI model usage, performance, and knowledge base activity
          </p>
        </div>
        <div className="flex items-center space-x-2">
          <Button
            variant={autoRefresh ? "default" : "outline"}
            size="sm"
            onClick={() => setAutoRefresh(!autoRefresh)}
          >
            {autoRefresh ? '⏸️' : '▶️'} Auto-refresh
          </Button>
          <Button variant="outline" size="sm" onClick={fetchAnalytics}>
            🔄 Refresh
          </Button>
        </div>
      </div>

      {/* Quick Stats */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Conversations</CardTitle>
            <span className="text-2xl">💬</span>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatNumber(totalConversations)}</div>
            <p className="text-xs text-muted-foreground">
              Across all models
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Active Providers</CardTitle>
            <span className="text-2xl">🤖</span>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{activeProviders}</div>
            <p className="text-xs text-muted-foreground">
              AI providers in use
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Tokens</CardTitle>
            <span className="text-2xl">🔢</span>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatNumber(totalTokens)}</div>
            <p className="text-xs text-muted-foreground">
              Tokens processed
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Knowledge Bases</CardTitle>
            <span className="text-2xl">📚</span>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totalKnowledgeBases}</div>
            <p className="text-xs text-muted-foreground">
              Active knowledge bases
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Provider Balance & Usage Widgets - Real-time updates */}
      <div className="space-y-4">
        <Card>
          <CardHeader>
            <CardTitle>Provider Analytics</CardTitle>
            <CardDescription>
              Real-time monitoring of your AI provider accounts with balance, usage, billing data, and local model analytics
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
              <AnthropicBalanceWidget />
              <OpenAIBalanceWidget />
              <GoogleAIBalanceWidget />
              <OllamaUsageWidget />
            </div>
            <p className="text-sm text-muted-foreground mt-4">
              Cloud provider widgets update automatically via WebSocket connections. Configure API keys in settings to enable provider data. Ollama shows local model usage and requires no configuration.
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Provider Status Cards */}
      <ProviderStatusCards 
        accounts={providerAccounts}
        usage={costBreakdown}
        loading={loading}
        onRefresh={fetchAnalytics}
      />

      {/* Charts Section */}
      <div className="grid gap-6 lg:grid-cols-2">
        <UsageTrendsChart data={usageTrends} loading={loading} />
        <CostBreakdownChart data={costBreakdown} loading={loading} />
      </div>

      {/* Performance Metrics */}
      <PerformanceMetrics data={modelStats} loading={loading} />

      {/* Model Performance Table */}
      <Card>
        <CardHeader>
          <CardTitle>Model Performance</CardTitle>
          <CardDescription>
            Usage statistics and performance metrics for all AI models
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {modelStats.length === 0 ? (
              <p className="text-center text-muted-foreground py-8">
                No model usage data available yet. Start some conversations to see analytics.
              </p>
            ) : (
              <div className="space-y-3">
                {modelStats.map((model, index) => (
                  <div key={index} className="flex items-center justify-between p-4 border rounded-lg">
                    <div className="flex items-center space-x-4">
                      <div className={`w-3 h-3 rounded-full ${getProviderColor(model.provider)}`}></div>
                      <div>
                        <div className="flex items-center space-x-2">
                          <span className="font-medium">{model.modelName}</span>
                          <Badge variant="secondary">{model.provider}</Badge>
                          {model.supportsTools && <Badge variant="outline">Tools</Badge>}
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {model.conversationCount} conversations • {formatNumber(model.totalTokens)} tokens
                        </div>
                      </div>
                    </div>
                    <div className="text-right">
                      <div className="text-sm font-medium">
                        {model.successRate.toFixed(1)}% success
                      </div>
                      <div className="text-xs text-muted-foreground">
                        Avg: {(model.averageResponseTime / 1000).toFixed(1)}s
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Knowledge Base Usage */}
      <Card>
        <CardHeader>
          <CardTitle>Knowledge Base Activity</CardTitle>
          <CardDescription>
            Usage patterns and statistics for your knowledge bases
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {knowledgeStats.length === 0 ? (
              <p className="text-center text-muted-foreground py-8">
                No knowledge bases found. Create some knowledge bases to see analytics.
              </p>
            ) : (
              <div className="space-y-3">
                {knowledgeStats.map((kb, index) => (
                  <div key={index} className="flex items-center justify-between p-4 border rounded-lg">
                    <div className="flex items-center space-x-4">
                      <span className="text-2xl">📖</span>
                      <div>
                        <div className="flex items-center space-x-2">
                          <span className="font-medium">{kb.knowledgeName || kb.knowledgeId}</span>
                          <Badge variant="outline">{kb.vectorStore}</Badge>
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {kb.documentCount} docs • {kb.chunkCount} chunks • {formatFileSize(kb.totalFileSize)}
                        </div>
                      </div>
                    </div>
                    <div className="text-right">
                      <div className="text-sm font-medium">
                        {kb.conversationCount} conversations
                      </div>
                      <div className="text-xs text-muted-foreground">
                        {kb.queryCount} queries total
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}