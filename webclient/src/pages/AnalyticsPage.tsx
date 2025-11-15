import { useState, useEffect, useCallback } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { UsageTrendsChart } from "@/components/analytics/UsageTrendsChart";
import { ProviderStatusCards } from "@/components/analytics/ProviderStatusCards";
import { PerformanceMetrics } from "@/components/analytics/PerformanceMetrics";
import { OllamaUsageWidget } from "@/components/analytics/OllamaUsageWidget";
import {
  ConversationIcon,
  ProviderIcon,
  TokenIcon,
  KnowledgeIcon,
  StarIcon,
  OpenAIIcon,
  AnthropicIcon,
  GoogleAIIcon,
  OllamaIcon
} from "@/components/icons";
import { Maximize2, Minimize2, Table, ArrowUpDown, ArrowUp, ArrowDown } from "lucide-react";
import { Responsive, WidthProvider } from "react-grid-layout";
import type { Layout } from "react-grid-layout";
import "react-grid-layout/css/styles.css";
import "react-resizable/css/styles.css";

const ResponsiveGridLayout = WidthProvider(Responsive);

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

interface UsageTrendData {
  date: string;
  totalRequests: number;
  successfulRequests: number;
  totalTokens: number;
  uniqueConversations: number;
}

interface ProviderAccountData {
  provider: string;
  isConnected: boolean;
  apiKeyConfigured: boolean;
  lastSyncAt?: string;
  balance?: number;
  balanceUnit?: string;
  monthlyUsage: number;
  errorMessage?: string;
}

interface CostBreakdownData {
  provider: string;
  totalCost: number;
  period: {
    startDate: string;
    endDate: string;
    days: number;
  };
}

// Default layouts for KPI cards (6 cards in 2 rows)
const defaultKpiLayout: Layout[] = [
  { i: "kpi-0", x: 0, y: 0, w: 2, h: 1, minW: 2, minH: 1 },
  { i: "kpi-1", x: 2, y: 0, w: 2, h: 1, minW: 2, minH: 1 },
  { i: "kpi-2", x: 4, y: 0, w: 2, h: 1, minW: 2, minH: 1 },
  { i: "kpi-3", x: 0, y: 1, w: 2, h: 1, minW: 2, minH: 1 },
  { i: "kpi-4", x: 2, y: 1, w: 2, h: 1, minW: 2, minH: 1 },
  { i: "kpi-5", x: 4, y: 1, w: 2, h: 1, minW: 2, minH: 1 },
];

// Default layouts for main widgets - ensures no overlaps with proper spacing
// Heights adjusted: provider-analytics +20%, others +5%
const defaultWidgetLayout: Layout[] = [
  { i: "provider-analytics", x: 0, y: 0, w: 12, h: 5, minW: 6, minH: 2 },  // 4 -> 5 (20% increase)
  { i: "provider-status", x: 0, y: 5, w: 12, h: 6, minW: 6, minH: 2 },     // 5 -> 6 (5% increase, rounded)
  { i: "usage-trends", x: 0, y: 11, w: 12, h: 6, minW: 6, minH: 3 },       // 5 -> 6 (5% increase, rounded)
  { i: "performance-metrics", x: 0, y: 17, w: 12, h: 6, minW: 6, minH: 3 }, // 5 -> 6 (5% increase, rounded)
  { i: "model-performance", x: 0, y: 23, w: 12, h: 7, minW: 6, minH: 3 },  // 6 -> 7 (5% increase, rounded)
  { i: "knowledge-activity", x: 0, y: 30, w: 12, h: 7, minW: 6, minH: 3 }, // 6 -> 7 (5% increase, rounded)
];

// Sortable table header component
interface SortableHeaderProps {
  column: string;
  label: string;
  currentSort?: { column: string; direction: 'asc' | 'desc' };
  onSort: (column: string) => void;
  className?: string;
}

function SortableHeader({ column, label, currentSort, onSort, className = "text-left p-2" }: SortableHeaderProps) {
  const isActive = currentSort?.column === column;
  const direction = currentSort?.direction;

  return (
    <th className={className}>
      <button
        onClick={() => onSort(column)}
        className="flex items-center gap-1 hover:text-primary transition-colors w-full"
      >
        <span>{label}</span>
        {isActive ? (
          direction === 'asc' ? <ArrowUp className="h-3 w-3" /> : <ArrowDown className="h-3 w-3" />
        ) : (
          <ArrowUpDown className="h-3 w-3 opacity-50" />
        )}
      </button>
    </th>
  );
}

export default function AnalyticsPage() {
  const [modelStats, setModelStats] = useState<ModelUsageStats[]>([]);
  const [knowledgeStats, setKnowledgeStats] = useState<KnowledgeUsageStats[]>([]);
  const [usageTrends, setUsageTrends] = useState<UsageTrendData[]>([]);
  const [costBreakdown, setCostBreakdown] = useState<CostBreakdownData[]>([]);
  const [providerAccounts, setProviderAccounts] = useState<ProviderAccountData[]>([]);
  const [loading, setLoading] = useState(true);
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [maximizedWidget, setMaximizedWidget] = useState<string | null>(null);
  
  // Layout state for KPIs and widgets
  const [kpiLayout, setKpiLayout] = useState<Layout[]>(() => {
    const saved = localStorage.getItem("analytics-kpi-layout");
    return saved ? JSON.parse(saved) : defaultKpiLayout;
  });
  
  const [widgetLayout, setWidgetLayout] = useState<Layout[]>(() => {
    const saved = localStorage.getItem("analytics-widget-layout");
    return saved ? JSON.parse(saved) : defaultWidgetLayout;
  });

  // Handle layout changes and persist to localStorage
  const handleKpiLayoutChange = useCallback((newLayout: Layout[]) => {
    setKpiLayout(newLayout);
    localStorage.setItem("analytics-kpi-layout", JSON.stringify(newLayout));
  }, []);

  const handleWidgetLayoutChange = useCallback((newLayout: Layout[]) => {
    setWidgetLayout(newLayout);
    localStorage.setItem("analytics-widget-layout", JSON.stringify(newLayout));
  }, []);

  // Reset layouts to default
  const handleResetLayout = useCallback(() => {
    setKpiLayout(defaultKpiLayout);
    setWidgetLayout(defaultWidgetLayout);
    localStorage.removeItem("analytics-kpi-layout");
    localStorage.removeItem("analytics-widget-layout");
    setMaximizedWidget(null);
  }, []);

  // Toggle maximize widget
  const handleToggleMaximize = useCallback((widgetId: string) => {
    setMaximizedWidget(prev => prev === widgetId ? null : widgetId);
  }, []);

  // Table view toggle state for each widget
  const [tableViewEnabled, setTableViewEnabled] = useState<Record<string, boolean>>({});

  // Sorting state for tables: { widgetId: { column: string, direction: 'asc' | 'desc' } }
  const [tableSorting, setTableSorting] = useState<Record<string, { column: string; direction: 'asc' | 'desc' }>>({});

  // Toggle table view for a specific widget
  const handleToggleTableView = useCallback((widgetId: string) => {
    setTableViewEnabled(prev => ({
      ...prev,
      [widgetId]: !prev[widgetId]
    }));
  }, []);

  // Handle column sort
  const handleSort = useCallback((widgetId: string, column: string) => {
    setTableSorting(prev => {
      const current = prev[widgetId];
      if (current?.column === column) {
        // Toggle direction
        return {
          ...prev,
          [widgetId]: { column, direction: current.direction === 'asc' ? 'desc' : 'asc' }
        };
      }
      // New column, default to ascending
      return {
        ...prev,
        [widgetId]: { column, direction: 'asc' }
      };
    });
  }, []);

  // Sort array based on column and direction
  const sortData = useCallback(<T extends Record<string, any>>(
    data: T[],
    widgetId: string,
    columnKey: string
  ): T[] => {
    const sorting = tableSorting[widgetId];
    if (!sorting || sorting.column !== columnKey) {
      return data;
    }

    return [...data].sort((a, b) => {
      const aVal = a[sorting.column];
      const bVal = b[sorting.column];
      
      // Handle different data types
      if (typeof aVal === 'string' && typeof bVal === 'string') {
        return sorting.direction === 'asc' 
          ? aVal.localeCompare(bVal)
          : bVal.localeCompare(aVal);
      }
      
      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return sorting.direction === 'asc' ? aVal - bVal : bVal - aVal;
      }
      
      return 0;
    });
  }, [tableSorting]);

  // Handle drag over for widget swapping
  const handleWidgetDrop = useCallback((layout: Layout[]) => {
    // react-grid-layout handles the drop, we just need to update state
    setWidgetLayout(layout);
    localStorage.setItem("analytics-widget-layout", JSON.stringify(layout));
  }, []);

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
    }, 600000);

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

  // Find most popular model (highest conversation count)
  const mostPopularModel = modelStats.reduce((prev, current) => 
    (current.conversationCount > prev.conversationCount) ? current : prev
  , { modelName: 'None', conversationCount: 0, provider: '' });

  // Find most popular knowledge base (highest conversation count)
  const mostPopularKnowledgeBase = knowledgeStats.reduce((prev, current) => 
    (current.conversationCount > prev.conversationCount) ? current : prev
  , { knowledgeName: 'None', knowledgeId: 'none', conversationCount: 0 });

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

  // KPI cards data
  const kpiCards = [
    {
      id: "kpi-0",
      title: "Total Conversations",
      icon: <ConversationIcon className="h-6 w-6 text-muted-foreground" />,
      value: formatNumber(totalConversations),
      description: "Across all models"
    },
    {
      id: "kpi-1",
      title: "Active Providers",
      icon: <ProviderIcon className="h-6 w-6 text-muted-foreground" />,
      value: activeProviders,
      description: "AI providers in use"
    },
    {
      id: "kpi-2",
      title: "Total Tokens",
      icon: <TokenIcon className="h-6 w-6 text-muted-foreground" />,
      value: formatNumber(totalTokens),
      description: "Tokens processed"
    },
    {
      id: "kpi-3",
      title: "Knowledge Bases",
      icon: <KnowledgeIcon className="h-6 w-6 text-muted-foreground" />,
      value: totalKnowledgeBases,
      description: "Active knowledge bases"
    },
    {
      id: "kpi-4",
      title: "Most Popular Model",
      icon: <StarIcon className="h-6 w-6 text-muted-foreground" />,
      value: mostPopularModel.modelName,
      description: `${mostPopularModel.conversationCount} conversations`,
      isText: true
    },
    {
      id: "kpi-5",
      title: "Most Popular KB",
      icon: <KnowledgeIcon className="h-6 w-6 text-muted-foreground" />,
      value: mostPopularKnowledgeBase.knowledgeName || mostPopularKnowledgeBase.knowledgeId,
      description: `${mostPopularKnowledgeBase.conversationCount} conversations`,
      isText: true
    }
  ];

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
            variant="secondary"
            size="sm"
            onClick={handleResetLayout}
            title="Reset dashboard layout to default"
          >
            Reset Layout
          </Button>
          <Button
            variant={autoRefresh ? "default" : "outline"}
            size="sm"
            onClick={() => setAutoRefresh(!autoRefresh)}
          >
            {autoRefresh ? 'Pause' : 'Start'} Auto-refresh
          </Button>
          <Button variant="outline" size="sm" onClick={fetchAnalytics}>
            Refresh
          </Button>
        </div>
      </div>

      {/* Quick Stats - Draggable KPI Cards */}
      <div className="relative">
        <div className="text-xs text-muted-foreground mb-2 flex items-center gap-2">
          <span className="inline-block w-3 h-3 bg-muted rounded"></span>
          <span>Drag KPI cards to reorder</span>
        </div>
        <ResponsiveGridLayout
          className="layout"
          layouts={{ lg: kpiLayout }}
          breakpoints={{ lg: 1200, md: 996, sm: 768, xs: 480, xxs: 0 }}
          cols={{ lg: 6, md: 4, sm: 2, xs: 2, xxs: 1 }}
          rowHeight={115}
          onLayoutChange={handleKpiLayoutChange}
          isDraggable={true}
          isResizable={false}
          compactType="horizontal"
          preventCollision={true}
        >
          {kpiCards.map((kpi) => (
            <div key={kpi.id} className="cursor-move">
              <Card className="h-full hover:shadow-md transition-shadow">
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">{kpi.title}</CardTitle>
                  {kpi.icon}
                </CardHeader>
                <CardContent>
                  <div className={kpi.isText ? "text-sm font-bold truncate" : "text-2xl font-bold"} title={typeof kpi.value === 'string' ? kpi.value : undefined}>
                    {kpi.value}
                  </div>
                  <p className="text-xs text-muted-foreground">
                    {kpi.description}
                  </p>
                </CardContent>
              </Card>
            </div>
          ))}
        </ResponsiveGridLayout>
      </div>

      {/* Main Widgets - Draggable and Resizable */}
      <div className="relative">
        <div className="text-xs text-muted-foreground mb-2 flex items-center gap-2">
          <span className="inline-block w-3 h-3 bg-muted rounded"></span>
          <span>Drag to move widgets, drag corners to resize</span>
        </div>
        {maximizedWidget ? (
          // Maximized widget view
          <div className="fixed inset-0 z-50 bg-background p-6 overflow-auto">
            <div className="h-full flex flex-col">
              {maximizedWidget === "provider-analytics" && (
                <Card className="h-full flex flex-col">
                  <CardHeader className="flex-shrink-0">
                    <div className="flex items-center justify-between">
                      <div>
                        <CardTitle>Provider Analytics</CardTitle>
                        <CardDescription>
                          Real-time monitoring of your AI provider accounts with balance, usage, billing data, and local model analytics
                        </CardDescription>
                      </div>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleToggleMaximize("provider-analytics")}
                        className="ml-2 h-8 w-8"
                      >
                        <Minimize2 className="h-5 w-5" />
                      </Button>
                    </div>
                  </CardHeader>
                  <CardContent className="flex-1 overflow-auto">
                    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
                      <OllamaUsageWidget />
                    </div>
                    <p className="text-sm text-muted-foreground mt-4">
                      Ollama shows local model usage and requires no configuration. Cloud provider widgets can be added by configuring API keys in settings.
                    </p>
                  </CardContent>
                </Card>
              )}
              {maximizedWidget === "provider-status" && (
                <div className="h-full flex flex-col">
                  <div className="flex items-center justify-end mb-4 gap-2">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleTableView("provider-status")}
                      className="h-9 px-3"
                    >
                      <Table className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleMaximize("provider-status")}
                      className="h-9 px-3"
                    >
                      <Minimize2 className="h-4 w-4" />
                    </Button>
                  </div>
                  <div className="flex-1 overflow-auto">
                    <ProviderStatusCards 
                      accounts={providerAccounts}
                      usage={costBreakdown}
                      loading={loading}
                      onRefresh={fetchAnalytics}
                      tableView={tableViewEnabled["provider-status"]}
                    />
                  </div>
                </div>
              )}
              {maximizedWidget === "usage-trends" && (
                <div className="h-full flex flex-col">
                  <div className="flex items-center justify-end mb-4 gap-2">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleTableView("usage-trends")}
                      className="h-9 px-3"
                    >
                      <Table className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleMaximize("usage-trends")}
                      className="h-9 px-3"
                    >
                      <Minimize2 className="h-4 w-4" />
                    </Button>
                  </div>
                  <div className="flex-1 overflow-auto">
                    <UsageTrendsChart data={usageTrends} loading={loading} tableView={tableViewEnabled["usage-trends"]} />
                  </div>
                </div>
              )}
              {maximizedWidget === "performance-metrics" && (
                <div className="h-full flex flex-col">
                  <div className="flex items-center justify-end mb-4 gap-2">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleTableView("performance-metrics")}
                      className="h-9 px-3"
                    >
                      <Table className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleMaximize("performance-metrics")}
                      className="h-9 px-3"
                    >
                      <Minimize2 className="h-4 w-4" />
                    </Button>
                  </div>
                  <div className="flex-1 overflow-auto">
                    <PerformanceMetrics data={modelStats} loading={loading} tableView={tableViewEnabled["performance-metrics"]} />
                  </div>
                </div>
              )}
              {maximizedWidget === "model-performance" && (
                <Card className="h-full flex flex-col">
                  <CardHeader className="flex-shrink-0">
                    <div className="flex items-center justify-between">
                      <div>
                        <CardTitle>Model Performance</CardTitle>
                        <CardDescription>
                          Usage statistics and performance metrics for all AI models
                        </CardDescription>
                      </div>
                      <div className="flex gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleToggleTableView("model-performance")}
                          className="flex-shrink-0 h-9 px-3"
                          title="Toggle table view"
                        >
                          <Table className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleToggleMaximize("model-performance")}
                          className="ml-2 h-9 px-3"
                        >
                          <Minimize2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  </CardHeader>
                  <CardContent className="flex-1 overflow-auto">
                    <div className="space-y-4">
                      {modelStats.length === 0 ? (
                        <p className="text-center text-muted-foreground py-8">
                          No model usage data available yet. Start some conversations to see analytics.
                        </p>
                      ) : tableViewEnabled["model-performance"] ? (
                        <div className="overflow-auto">
                          <table className="w-full text-sm">
                            <thead>
                              <tr className="border-b">
                                <SortableHeader
                                  column="modelName"
                                  label="Model"
                                  currentSort={tableSorting["model-performance"]}
                                  onSort={(col) => handleSort("model-performance", col)}
                                />
                                <SortableHeader
                                  column="provider"
                                  label="Provider"
                                  currentSort={tableSorting["model-performance"]}
                                  onSort={(col) => handleSort("model-performance", col)}
                                />
                                <SortableHeader
                                  column="conversationCount"
                                  label="Conversations"
                                  currentSort={tableSorting["model-performance"]}
                                  onSort={(col) => handleSort("model-performance", col)}
                                />
                                <SortableHeader
                                  column="totalTokens"
                                  label="Tokens"
                                  currentSort={tableSorting["model-performance"]}
                                  onSort={(col) => handleSort("model-performance", col)}
                                />
                                <SortableHeader
                                  column="successRate"
                                  label="Success Rate"
                                  currentSort={tableSorting["model-performance"]}
                                  onSort={(col) => handleSort("model-performance", col)}
                                />
                                <SortableHeader
                                  column="averageResponseTime"
                                  label="Avg Response"
                                  currentSort={tableSorting["model-performance"]}
                                  onSort={(col) => handleSort("model-performance", col)}
                                />
                                <th className="text-left p-2">Tools</th>
                              </tr>
                            </thead>
                            <tbody>
                              {sortData(modelStats, "model-performance", tableSorting["model-performance"]?.column || "modelName").map((model, index) => (
                                <tr key={index} className="border-b hover:bg-muted/50 transition-colors">
                                  <td className="p-2 font-medium">{model.modelName}</td>
                                  <td className="p-2">{model.provider}</td>
                                  <td className="p-2">{model.conversationCount}</td>
                                  <td className="p-2">{formatNumber(model.totalTokens)}</td>
                                  <td className="p-2">{model.successRate.toFixed(1)}%</td>
                                  <td className="p-2">{(model.averageResponseTime / 1000).toFixed(1)}s</td>
                                  <td className="p-2">{model.supportsTools ? "Yes" : "No"}</td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      ) : (
                        <div className="space-y-3">
                          {modelStats.map((model, index) => (
                            <div key={index} className="flex items-center justify-between p-4 border rounded-lg">
                              <div className="flex items-center space-x-4">
                                {getProviderIcon(model.provider)}
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
              )}
              {maximizedWidget === "knowledge-activity" && (
                <Card className="h-full flex flex-col">
                  <CardHeader className="flex-shrink-0">
                    <div className="flex items-center justify-between">
                      <div>
                        <CardTitle>Knowledge Base Activity</CardTitle>
                        <CardDescription>
                          Usage patterns and statistics for your knowledge bases
                        </CardDescription>
                      </div>
                      <div className="flex gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleToggleTableView("knowledge-activity")}
                          className="flex-shrink-0 h-9 px-3"
                          title="Toggle table view"
                        >
                          <Table className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleToggleMaximize("knowledge-activity")}
                          className="ml-2 h-9 px-3"
                        >
                          <Minimize2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  </CardHeader>
                  <CardContent className="flex-1 overflow-auto">
                    <div className="space-y-4">
                      {knowledgeStats.length === 0 ? (
                        <p className="text-center text-muted-foreground py-8">
                          No knowledge bases found. Create some knowledge bases to see analytics.
                        </p>
                      ) : tableViewEnabled["knowledge-activity"] ? (
                        <div className="overflow-auto">
                          <table className="w-full text-sm">
                            <thead>
                              <tr className="border-b">
                                <SortableHeader
                                  column="knowledgeName"
                                  label="Knowledge Base"
                                  currentSort={tableSorting["knowledge-activity"]}
                                  onSort={(col) => handleSort("knowledge-activity", col)}
                                />
                                <SortableHeader
                                  column="vectorStore"
                                  label="Vector Store"
                                  currentSort={tableSorting["knowledge-activity"]}
                                  onSort={(col) => handleSort("knowledge-activity", col)}
                                />
                                <SortableHeader
                                  column="documentCount"
                                  label="Documents"
                                  currentSort={tableSorting["knowledge-activity"]}
                                  onSort={(col) => handleSort("knowledge-activity", col)}
                                />
                                <SortableHeader
                                  column="chunkCount"
                                  label="Chunks"
                                  currentSort={tableSorting["knowledge-activity"]}
                                  onSort={(col) => handleSort("knowledge-activity", col)}
                                />
                                <SortableHeader
                                  column="totalFileSize"
                                  label="Size"
                                  currentSort={tableSorting["knowledge-activity"]}
                                  onSort={(col) => handleSort("knowledge-activity", col)}
                                />
                                <SortableHeader
                                  column="conversationCount"
                                  label="Conversations"
                                  currentSort={tableSorting["knowledge-activity"]}
                                  onSort={(col) => handleSort("knowledge-activity", col)}
                                />
                                <SortableHeader
                                  column="queryCount"
                                  label="Queries"
                                  currentSort={tableSorting["knowledge-activity"]}
                                  onSort={(col) => handleSort("knowledge-activity", col)}
                                />
                              </tr>
                            </thead>
                            <tbody>
                              {sortData(knowledgeStats, "knowledge-activity", tableSorting["knowledge-activity"]?.column || "knowledgeName").map((kb, index) => (
                                <tr key={index} className="border-b hover:bg-muted/50 transition-colors">
                                  <td className="p-2 font-medium">{kb.knowledgeName || kb.knowledgeId}</td>
                                  <td className="p-2">{kb.vectorStore}</td>
                                  <td className="p-2">{kb.documentCount}</td>
                                  <td className="p-2">{kb.chunkCount}</td>
                                  <td className="p-2">{formatFileSize(kb.totalFileSize)}</td>
                                  <td className="p-2">{kb.conversationCount}</td>
                                  <td className="p-2">{kb.queryCount}</td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      ) : (
                        <div className="space-y-3">
                          {knowledgeStats.map((kb, index) => (
                            <div key={index} className="flex items-center justify-between p-4 border rounded-lg">
                              <div className="flex items-center space-x-4">
                                <KnowledgeIcon className="h-6 w-6 text-muted-foreground" />
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
              )}
            </div>
          </div>
        ) : (
          // Normal grid view
          <ResponsiveGridLayout
            className="layout"
            layouts={{ lg: widgetLayout }}
          breakpoints={{ lg: 1200, md: 996, sm: 768, xs: 480, xxs: 0 }}
          cols={{ lg: 12, md: 10, sm: 6, xs: 4, xxs: 2 }}
          rowHeight={80}
          onLayoutChange={handleWidgetLayoutChange}
          onDrop={handleWidgetDrop}
          isDraggable={true}
          isResizable={true}
          compactType={null}
          preventCollision={false}
          allowOverlap={false}
        >
          {/* Provider Balance & Usage Widgets */}
          <div key="provider-analytics" className="cursor-move">
            <Card className="h-full">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <CardTitle>Provider Analytics</CardTitle>
                    <CardDescription>
                      Real-time monitoring of your AI provider accounts with balance, usage, billing data, and local model analytics
                    </CardDescription>
                  </div>
                  <div className="flex gap-1">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleTableView("provider-analytics")}
                      className="flex-shrink-0 h-9 px-3"
                      title="Toggle table view"
                    >
                      <Table className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleMaximize("provider-analytics")}
                      className="flex-shrink-0 h-9 px-3"
                      title="Maximize widget"
                    >
                      <Maximize2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                {tableViewEnabled["provider-analytics"] ? (
                  <div className="overflow-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b">
                          <th className="text-left p-2">Provider</th>
                          <th className="text-left p-2">Status</th>
                          <th className="text-left p-2">Balance</th>
                          <th className="text-left p-2">Monthly Usage</th>
                        </tr>
                      </thead>
                      <tbody>
                        {providerAccounts.map((account, idx) => (
                          <tr key={idx} className="border-b">
                            <td className="p-2">{account.provider}</td>
                            <td className="p-2">{account.isConnected ? "Connected" : "Disconnected"}</td>
                            <td className="p-2">{account.balance ? `${account.balance} ${account.balanceUnit || ""}` : "N/A"}</td>
                            <td className="p-2">${account.monthlyUsage.toFixed(2)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <>
                    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
                      <OllamaUsageWidget />
                    </div>
                    <p className="text-sm text-muted-foreground mt-4">
                      Ollama shows local model usage and requires no configuration. Cloud provider widgets can be added by configuring API keys in settings.
                    </p>
                  </>
                )}
              </CardContent>
            </Card>
          </div>

          {/* Provider Status Cards */}
          <div key="provider-status" className="cursor-move">
            <div className="h-full relative">
              <div className="flex gap-1 absolute top-2 right-2 z-10">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleToggleTableView("provider-status")}
                  className="h-9 px-3"
                  title="Toggle table view"
                >
                  <Table className="h-4 w-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleToggleMaximize("provider-status")}
                  className="h-9 px-3"
                  title="Maximize widget"
                >
                  <Maximize2 className="h-4 w-4" />
                </Button>
              </div>
              <ProviderStatusCards 
                accounts={providerAccounts}
                usage={costBreakdown}
                loading={loading}
                onRefresh={fetchAnalytics}
                tableView={tableViewEnabled["provider-status"]}
              />
            </div>
          </div>

          {/* Charts Section */}
          <div key="usage-trends" className="cursor-move">
            <div className="h-full relative">
              <div className="flex gap-1 absolute top-2 right-2 z-10">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleToggleTableView("usage-trends")}
                  className="h-9 px-3"
                  title="Toggle table view"
                >
                  <Table className="h-4 w-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleToggleMaximize("usage-trends")}
                  className="h-9 px-3"
                  title="Maximize widget"
                >
                  <Maximize2 className="h-4 w-4" />
                </Button>
              </div>
              <UsageTrendsChart data={usageTrends} loading={loading} tableView={tableViewEnabled["usage-trends"]} />
            </div>
          </div>

          {/* Performance Metrics */}
          <div key="performance-metrics" className="cursor-move">
            <div className="h-full relative">
              <div className="flex gap-1 absolute top-2 right-2 z-10">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleToggleTableView("performance-metrics")}
                  className="h-9 px-3"
                  title="Toggle table view"
                >
                  <Table className="h-4 w-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleToggleMaximize("performance-metrics")}
                  className="h-9 px-3"
                  title="Maximize widget"
                >
                  <Maximize2 className="h-4 w-4" />
                </Button>
              </div>
              <PerformanceMetrics data={modelStats} loading={loading} tableView={tableViewEnabled["performance-metrics"]} />
            </div>
          </div>

          {/* Model Performance Table */}
          <div key="model-performance" className="cursor-move">
            <Card className="h-full">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <CardTitle>Model Performance</CardTitle>
                    <CardDescription>
                      Usage statistics and performance metrics for all AI models
                    </CardDescription>
                  </div>
                  <div className="flex gap-1">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleTableView("model-performance")}
                      className="flex-shrink-0 h-9 px-3"
                      title="Toggle table view"
                    >
                      <Table className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleMaximize("model-performance")}
                      className="ml-2 flex-shrink-0 h-9 px-3"
                      title="Maximize widget"
                    >
                      <Maximize2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {modelStats.length === 0 ? (
                    <p className="text-center text-muted-foreground py-8">
                      No model usage data available yet. Start some conversations to see analytics.
                    </p>
                  ) : tableViewEnabled["model-performance"] ? (
                    <div className="overflow-auto">
                      <table className="w-full text-sm">
                        <thead>
                          <tr className="border-b">
                            <SortableHeader
                              column="modelName"
                              label="Model"
                              currentSort={tableSorting["model-performance"]}
                              onSort={(col) => handleSort("model-performance", col)}
                            />
                            <SortableHeader
                              column="provider"
                              label="Provider"
                              currentSort={tableSorting["model-performance"]}
                              onSort={(col) => handleSort("model-performance", col)}
                            />
                            <SortableHeader
                              column="conversationCount"
                              label="Conversations"
                              currentSort={tableSorting["model-performance"]}
                              onSort={(col) => handleSort("model-performance", col)}
                            />
                            <SortableHeader
                              column="totalTokens"
                              label="Tokens"
                              currentSort={tableSorting["model-performance"]}
                              onSort={(col) => handleSort("model-performance", col)}
                            />
                            <SortableHeader
                              column="successRate"
                              label="Success Rate"
                              currentSort={tableSorting["model-performance"]}
                              onSort={(col) => handleSort("model-performance", col)}
                            />
                            <SortableHeader
                              column="averageResponseTime"
                              label="Avg Response"
                              currentSort={tableSorting["model-performance"]}
                              onSort={(col) => handleSort("model-performance", col)}
                            />
                            <th className="text-left p-2">Tools</th>
                          </tr>
                        </thead>
                        <tbody>
                          {sortData(modelStats, "model-performance", tableSorting["model-performance"]?.column || "modelName").map((model, index) => (
                            <tr key={index} className="border-b hover:bg-muted/50 transition-colors">
                              <td className="p-2 font-medium">{model.modelName}</td>
                              <td className="p-2">{model.provider}</td>
                              <td className="p-2">{model.conversationCount}</td>
                              <td className="p-2">{formatNumber(model.totalTokens)}</td>
                              <td className="p-2">{model.successRate.toFixed(1)}%</td>
                              <td className="p-2">{(model.averageResponseTime / 1000).toFixed(1)}s</td>
                              <td className="p-2">{model.supportsTools ? "Yes" : "No"}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <div className="space-y-3">
                      {modelStats.map((model, index) => (
                        <div key={index} className="flex items-center justify-between p-4 border rounded-lg">
                          <div className="flex items-center space-x-4">
                            {getProviderIcon(model.provider)}
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
          </div>

          {/* Knowledge Base Usage */}
          <div key="knowledge-activity" className="cursor-move">
            <Card className="h-full">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <CardTitle>Knowledge Base Activity</CardTitle>
                    <CardDescription>
                      Usage patterns and statistics for your knowledge bases
                    </CardDescription>
                  </div>
                  <div className="flex gap-1">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleTableView("knowledge-activity")}
                      className="flex-shrink-0 h-9 px-3"
                      title="Toggle table view"
                    >
                      <Table className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleMaximize("knowledge-activity")}
                      className="ml-2 flex-shrink-0 h-9 px-3"
                      title="Maximize widget"
                    >
                      <Maximize2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {knowledgeStats.length === 0 ? (
                    <p className="text-center text-muted-foreground py-8">
                      No knowledge bases found. Create some knowledge bases to see analytics.
                    </p>
                  ) : tableViewEnabled["knowledge-activity"] ? (
                    <div className="overflow-auto">
                      <table className="w-full text-sm">
                        <thead>
                          <tr className="border-b">
                            <SortableHeader
                              column="knowledgeName"
                              label="Knowledge Base"
                              currentSort={tableSorting["knowledge-activity"]}
                              onSort={(col) => handleSort("knowledge-activity", col)}
                            />
                            <SortableHeader
                              column="vectorStore"
                              label="Vector Store"
                              currentSort={tableSorting["knowledge-activity"]}
                              onSort={(col) => handleSort("knowledge-activity", col)}
                            />
                            <SortableHeader
                              column="documentCount"
                              label="Documents"
                              currentSort={tableSorting["knowledge-activity"]}
                              onSort={(col) => handleSort("knowledge-activity", col)}
                            />
                            <SortableHeader
                              column="chunkCount"
                              label="Chunks"
                              currentSort={tableSorting["knowledge-activity"]}
                              onSort={(col) => handleSort("knowledge-activity", col)}
                            />
                            <SortableHeader
                              column="totalFileSize"
                              label="Size"
                              currentSort={tableSorting["knowledge-activity"]}
                              onSort={(col) => handleSort("knowledge-activity", col)}
                            />
                            <SortableHeader
                              column="conversationCount"
                              label="Conversations"
                              currentSort={tableSorting["knowledge-activity"]}
                              onSort={(col) => handleSort("knowledge-activity", col)}
                            />
                            <SortableHeader
                              column="queryCount"
                              label="Queries"
                              currentSort={tableSorting["knowledge-activity"]}
                              onSort={(col) => handleSort("knowledge-activity", col)}
                            />
                          </tr>
                        </thead>
                        <tbody>
                          {sortData(knowledgeStats, "knowledge-activity", tableSorting["knowledge-activity"]?.column || "knowledgeName").map((kb, index) => (
                            <tr key={index} className="border-b hover:bg-muted/50 transition-colors">
                              <td className="p-2 font-medium">{kb.knowledgeName || kb.knowledgeId}</td>
                              <td className="p-2">{kb.vectorStore}</td>
                              <td className="p-2">{kb.documentCount}</td>
                              <td className="p-2">{kb.chunkCount}</td>
                              <td className="p-2">{formatFileSize(kb.totalFileSize)}</td>
                              <td className="p-2">{kb.conversationCount}</td>
                              <td className="p-2">{kb.queryCount}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <div className="space-y-3">
                      {knowledgeStats.map((kb, index) => (
                        <div key={index} className="flex items-center justify-between p-4 border rounded-lg">
                          <div className="flex items-center space-x-4">
                            <KnowledgeIcon className="h-6 w-6 text-muted-foreground" />
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
        </ResponsiveGridLayout>
        )}
      </div>
    </div>
  );
}