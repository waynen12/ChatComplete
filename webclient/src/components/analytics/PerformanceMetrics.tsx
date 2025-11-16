import { useMemo, useState } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, ReferenceLine } from 'recharts';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { PerformanceIcon } from "@/components/icons";
import { ArrowUpDown, ArrowUp, ArrowDown } from 'lucide-react';

interface ModelMetrics {
  modelName: string;
  provider: string;
  averageResponseTime: number; // in milliseconds
  successRate: number; // percentage
  totalRequests: number;
  conversationCount: number;
}

interface PerformanceMetricsProps {
  data: ModelMetrics[];
  loading?: boolean;
  tableView?: boolean;
}

export function PerformanceMetrics({ data, loading, tableView = false }: PerformanceMetricsProps) {
  const [sortConfig, setSortConfig] = useState<{ column: string; direction: 'asc' | 'desc' } | null>(null);

  const chartData = useMemo(() => {
    if (!data || data.length === 0) {
      // Sample data for demonstration
      return [
        { 
          model: 'gpt-4', 
          provider: 'OpenAi', 
          responseTime: 1.2, 
          successRate: 98.5, 
          requests: 234,
          color: '#10b981'
        },
        { 
          model: 'claude-3.5', 
          provider: 'Anthropic', 
          responseTime: 0.9, 
          successRate: 99.1, 
          requests: 89,
          color: '#f97316'
        },
        { 
          model: 'gemini-pro', 
          provider: 'Google', 
          responseTime: 1.5, 
          successRate: 96.8, 
          requests: 67,
          color: '#8b5cf6'
        },
        { 
          model: 'llama3.1:8b', 
          provider: 'Ollama', 
          responseTime: 2.1, 
          successRate: 95.2, 
          requests: 456,
          color: '#a855f7'
        },
      ];
    }

    const providerColors: Record<string, string> = {
      'OpenAi': '#10b981',
      'Anthropic': '#f97316', 
      'Google': '#8b5cf6',
      'Ollama': '#a855f7'
    };

    return data.map(item => ({
      model: item.modelName,
      provider: item.provider,
      responseTime: item.averageResponseTime / 1000, // Convert to seconds
      successRate: item.successRate,
      requests: item.totalRequests,
      color: providerColors[item.provider] || '#6b7280'
    })).sort((a, b) => b.requests - a.requests); // Sort by request count
  }, [data]);

  const averageResponseTime = chartData.reduce((sum, item) => sum + item.responseTime, 0) / chartData.length;

  const sortedData = useMemo(() => {
    if (!sortConfig) return chartData;

    return [...chartData].sort((a, b) => {
      const aVal = a[sortConfig.column as keyof typeof a];
      const bVal = b[sortConfig.column as keyof typeof b];
      
      if (typeof aVal === 'string' && typeof bVal === 'string') {
        return sortConfig.direction === 'asc' 
          ? aVal.localeCompare(bVal)
          : bVal.localeCompare(aVal);
      }
      
      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return sortConfig.direction === 'asc' ? aVal - bVal : bVal - aVal;
      }
      
      return 0;
    });
  }, [chartData, sortConfig]);

  const handleSort = (column: string) => {
    setSortConfig(current => {
      if (current?.column === column) {
        return { column, direction: current.direction === 'asc' ? 'desc' : 'asc' };
      }
      return { column, direction: 'asc' };
    });
  };

  const SortableHeader = ({ column, label }: { column: string; label: string }) => {
    const isActive = sortConfig?.column === column;
    const direction = sortConfig?.direction;

    return (
      <th className="text-left p-2">
        <button
          onClick={() => handleSort(column)}
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
  };

  interface TooltipProps {
    active?: boolean;
    payload?: Array<{
      payload: {
        model: string;
        provider: string;
        responseTime: number;
        successRate: number;
        requests: number;
        color: string;
      };
    }>;
    label?: string;
  }

  const CustomTooltip = ({ active, payload, label }: TooltipProps) => {
    if (active && payload && payload.length) {
      const data = payload[0].payload;
      return (
        <div className="bg-background border border-border rounded-md p-3 shadow-md">
          <p className="font-medium">{label}</p>
          <div className="space-y-1 text-sm">
            <p className="flex justify-between">
              <span>Provider:</span>
              <Badge variant="outline" style={{ borderColor: data.color }}>
                {data.provider}
              </Badge>
            </p>
            <p className="flex justify-between">
              <span>Response Time:</span>
              <span>{data.responseTime.toFixed(2)}s</span>
            </p>
            <p className="flex justify-between">
              <span>Success Rate:</span>
              <span>{data.successRate.toFixed(1)}%</span>
            </p>
            <p className="flex justify-between">
              <span>Total Requests:</span>
              <span>{data.requests}</span>
            </p>
          </div>
        </div>
      );
    }
    return null;
  };

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Performance Metrics</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="h-80 animate-pulse bg-gray-100 rounded"></div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Performance Metrics</CardTitle>
        <CardDescription>
          Average response times by model (Avg: {averageResponseTime.toFixed(2)}s)
        </CardDescription>
      </CardHeader>
      <CardContent>
        {tableView ? (
          <div className="overflow-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <SortableHeader column="model" label="Model" />
                  <SortableHeader column="provider" label="Provider" />
                  <SortableHeader column="responseTime" label="Response Time (s)" />
                  <SortableHeader column="successRate" label="Success Rate (%)" />
                  <SortableHeader column="requests" label="Total Requests" />
                  <th className="text-left p-2">Conversations</th>
                </tr>
              </thead>
              <tbody>
                {sortedData.map((item, index) => (
                  <tr key={index} className="border-b hover:bg-muted/50 transition-colors">
                    <td className="p-2 font-medium">{item.model}</td>
                    <td className="p-2">{item.provider}</td>
                    <td className="p-2">{item.responseTime.toFixed(2)}</td>
                    <td className="p-2">{item.successRate.toFixed(1)}</td>
                    <td className="p-2">{item.requests}</td>
                    <td className="p-2">{data.find(d => d.modelName === item.model)?.conversationCount || 0}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="h-80">
            {chartData.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={chartData} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis 
                    dataKey="model" 
                    tick={{ fontSize: 11 }}
                    interval={0}
                    angle={-45}
                    textAnchor="end"
                    height={80}
                  />
                  <YAxis 
                    tick={{ fontSize: 12 }}
                    label={{ value: 'Response Time (s)', angle: -90, position: 'insideLeft' }}
                  />
                  <Tooltip content={<CustomTooltip />} />
                  <ReferenceLine 
                    y={averageResponseTime} 
                    stroke="hsl(var(--muted-foreground))" 
                    strokeDasharray="5 5" 
                  />
                  <Bar dataKey="responseTime" radius={[4, 4, 0, 0]}>
                    {chartData.map((entry, index) => (
                      <Bar key={`bar-${index}`} fill={entry.color} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex items-center justify-center h-full text-muted-foreground">
                <div className="text-center">
                  <PerformanceIcon className="h-16 w-16 mb-2 mx-auto text-muted-foreground" />
                  <p>No performance data available</p>
                  <p className="text-sm">Start conversations to see model performance metrics</p>
                </div>
              </div>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}