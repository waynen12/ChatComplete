import { useMemo } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, ReferenceLine } from 'recharts';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { PerformanceIcon } from "@/components/icons";

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
}

export function PerformanceMetrics({ data, loading }: PerformanceMetricsProps) {
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
          color: '#3b82f6'
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
      'Google': '#3b82f6',
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

  const CustomTooltip = ({ active, payload, label }: any) => {
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
      </CardContent>
    </Card>
  );
}