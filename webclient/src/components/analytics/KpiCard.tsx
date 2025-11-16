import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { ReactNode } from "react";

interface KpiCardProps {
  id: string;
  title: string;
  icon: ReactNode;
  value: string | number;
  description: string;
  isText?: boolean;
  className?: string;
}

export function KpiCard({ title, icon, value, description, isText, className }: KpiCardProps) {
  return (
    <Card className={`h-full hover:shadow-md transition-shadow ${className || ""}`}>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        {icon}
      </CardHeader>
      <CardContent>
        <div 
          className={isText ? "text-sm font-bold truncate" : "text-2xl font-bold"} 
          title={typeof value === 'string' ? value : undefined}
        >
          {value}
        </div>
        <p className="text-xs text-muted-foreground">
          {description}
        </p>
      </CardContent>
    </Card>
  );
}
