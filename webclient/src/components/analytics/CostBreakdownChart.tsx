import { useMemo } from 'react';
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip, Legend } from 'recharts';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

interface CostBreakdownChartProps {
  data: Array<{ provider: string; totalCost: number }>;
  loading?: boolean;
}

const PROVIDER_COLORS: Record<string, string> = {
  'OpenAi': '#10b981', // emerald-500
  'Anthropic': '#f97316', // orange-500
  'Google': '#3b82f6', // blue-500
  'Ollama': '#a855f7', // purple-500
};

export function CostBreakdownChart({ data, loading }: CostBreakdownChartProps) {
  const chartData = useMemo(() => {
    if (!data || data.length === 0) {
      // Generate sample data if no real data
      return [
        { provider: 'OpenAi', totalCost: 15.75, color: PROVIDER_COLORS.OpenAi },
        { provider: 'Anthropic', totalCost: 8.25, color: PROVIDER_COLORS.Anthropic },
        { provider: 'Google', totalCost: 4.50, color: PROVIDER_COLORS.Google },
        { provider: 'Ollama', totalCost: 0, color: PROVIDER_COLORS.Ollama },
      ];
    }

    return data.map(item => ({
      provider: item.provider,
      totalCost: item.totalCost,
      color: PROVIDER_COLORS[item.provider] || '#6b7280'
    })).filter(item => item.totalCost > 0);
  }, [data]);

  const totalCost = chartData.reduce((sum, item) => sum + item.totalCost, 0);

  const CustomTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      const data = payload[0].payload;
      return (
        <div className="bg-background border border-border rounded-md p-3 shadow-md">
          <p className="font-medium">{data.provider}</p>
          <p className="text-sm text-muted-foreground">
            ${data.totalCost.toFixed(2)} ({((data.totalCost / totalCost) * 100).toFixed(1)}%)
          </p>
        </div>
      );
    }
    return null;
  };

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Cost Breakdown</CardTitle>
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
        <CardTitle>Cost Breakdown</CardTitle>
        <CardDescription>
          Monthly costs by provider (${totalCost.toFixed(2)} total)
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="h-80">
          {chartData.length > 0 ? (
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={chartData}
                  cx="50%"
                  cy="50%"
                  innerRadius={60}
                  outerRadius={120}
                  paddingAngle={5}
                  dataKey="totalCost"
                >
                  {chartData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip content={<CustomTooltip />} />
                <Legend 
                  verticalAlign="bottom" 
                  height={36}
                  formatter={(value, entry: any) => (
                    <span style={{ color: entry.color }}>
                      {value}: ${entry.payload.totalCost.toFixed(2)}
                    </span>
                  )}
                />
              </PieChart>
            </ResponsiveContainer>
          ) : (
            <div className="flex items-center justify-center h-full text-muted-foreground">
              <div className="text-center">
                <div className="text-6xl mb-2">💰</div>
                <p>No cost data available</p>
                <p className="text-sm">Start using external providers to see cost breakdown</p>
              </div>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}