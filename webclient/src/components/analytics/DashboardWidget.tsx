import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Maximize2, Table } from "lucide-react";
import type { ReactNode } from "react";

interface DashboardWidgetProps {
  id: string;
  title: string;
  description?: string;
  children: ReactNode;
  onMaximize?: () => void;
  onToggleTableView?: () => void;
  className?: string;
  showTableViewButton?: boolean;
  showMaximizeButton?: boolean;
}

export function DashboardWidget({
  title,
  description,
  children,
  onMaximize,
  onToggleTableView,
  className,
  showTableViewButton = false,
  showMaximizeButton = true,
}: DashboardWidgetProps) {
  const hasActions = showTableViewButton || showMaximizeButton;

  return (
    <Card className={`h-full ${className || ""}`}>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex-1">
            <CardTitle>{title}</CardTitle>
            {description && <CardDescription>{description}</CardDescription>}
          </div>
          {hasActions && (
            <div className="flex gap-1">
              {showTableViewButton && onToggleTableView && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={onToggleTableView}
                  className="flex-shrink-0 h-9 px-3"
                  title="Toggle table view"
                >
                  <Table className="h-4 w-4" />
                </Button>
              )}
              {showMaximizeButton && onMaximize && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={onMaximize}
                  className={`flex-shrink-0 h-9 px-3 ${showTableViewButton ? 'ml-0' : ''}`}
                  title="Maximize widget"
                >
                  <Maximize2 className="h-4 w-4" />
                </Button>
              )}
            </div>
          )}
        </div>
      </CardHeader>
      <CardContent>{children}</CardContent>
    </Card>
  );
}
