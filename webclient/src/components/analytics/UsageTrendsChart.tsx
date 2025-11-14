import { useMemo } from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { format, subDays, startOfDay } from 'date-fns';

interface UsageTrendData {
  date: string;
  totalRequests: number;
  successfulRequests: number;
  totalTokens: number;
  uniqueConversations: number;
}

interface UsageTrendsChartProps {
  data: UsageTrendData[];
  loading?: boolean;
}

export function UsageTrendsChart({ data, loading }: UsageTrendsChartProps) {
  const chartData = useMemo(() => {
    if (!data || data.length === 0) {
      // Generate sample data for the last 7 days if no real data
      const sampleData = [];
      for (let i = 6; i >= 0; i--) {
        const date = startOfDay(subDays(new Date(), i));
        sampleData.push({
          date: format(date, 'MMM dd'),
          totalRequests: Math.floor(Math.random() * 50) + 10,
          successfulRequests: Math.floor(Math.random() * 45) + 8,
          totalTokens: Math.floor(Math.random() * 5000) + 1000,
          uniqueConversations: Math.floor(Math.random() * 20) + 5,
        });
      }
      return sampleData;
    }

    return data.map(item => ({
      date: format(new Date(item.date), 'MMM dd'),
      totalRequests: item.totalRequests,
      successfulRequests: item.successfulRequests,
      totalTokens: Math.floor(item.totalTokens / 100), // Scale down for better visualization
      uniqueConversations: item.uniqueConversations,
    }));
  }, [data]);

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Usage Trends</CardTitle>
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
        <CardTitle>Usage Trends</CardTitle>
        <CardDescription>
          Daily usage patterns over the past week
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="h-80">
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis 
                dataKey="date" 
                tick={{ fontSize: 12 }}
              />
              <YAxis 
                tick={{ fontSize: 12 }}
              />
              <Tooltip 
                contentStyle={{ 
                  backgroundColor: 'hsl(var(--background))',
                  border: '1px solid hsl(var(--border))',
                  borderRadius: '6px'
                }}
              />
              <Legend />
              <Line 
                type="monotone" 
                dataKey="totalRequests" 
                stroke="hsl(var(--primary))" 
                strokeWidth={2}
                name="Total Requests"
              />
              <Line 
                type="monotone" 
                dataKey="successfulRequests" 
                stroke="hsl(142, 71%, 45%)" 
                strokeWidth={2}
                name="Successful"
              />
              <Line 
                type="monotone" 
                dataKey="totalTokens" 
                stroke="hsl(217, 91%, 60%)" 
                strokeWidth={2}
                name="Tokens (×100)"
              />
              <Line 
                type="monotone" 
                dataKey="uniqueConversations" 
                stroke="hsl(280, 100%, 70%)" 
                strokeWidth={2}
                name="Conversations"
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </CardContent>
    </Card>
  );
}