import { useMemo, useState } from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { format, subDays, startOfDay } from 'date-fns';
import { ArrowUpDown, ArrowUp, ArrowDown } from 'lucide-react';

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
  tableView?: boolean;
}

export function UsageTrendsChart({ data, loading, tableView = false }: UsageTrendsChartProps) {
  const [sortConfig, setSortConfig] = useState<{ column: string; direction: 'asc' | 'desc' } | null>(null);

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
        {tableView ? (
          <div className="overflow-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <SortableHeader column="date" label="Date" />
                  <SortableHeader column="totalRequests" label="Total Requests" />
                  <SortableHeader column="successfulRequests" label="Successful" />
                  <SortableHeader column="totalTokens" label="Tokens (×100)" />
                  <SortableHeader column="uniqueConversations" label="Conversations" />
                </tr>
              </thead>
              <tbody>
                {sortedData.map((item, index) => (
                  <tr key={index} className="border-b hover:bg-muted/50 transition-colors">
                    <td className="p-2 font-medium">{item.date}</td>
                    <td className="p-2">{item.totalRequests}</td>
                    <td className="p-2">{item.successfulRequests}</td>
                    <td className="p-2">{item.totalTokens}</td>
                    <td className="p-2">{item.uniqueConversations}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
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
        )}
      </CardContent>
    </Card>
  );
}