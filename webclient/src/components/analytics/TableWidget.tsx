import { DashboardWidget } from "./DashboardWidget";
import { ArrowUpDown, ArrowUp, ArrowDown } from 'lucide-react';
import type { ReactNode } from "react";

interface SortConfig {
  column: string;
  direction: 'asc' | 'desc';
}

interface ColumnDef {
  key: string;
  label: string;
  sortable?: boolean;
}

interface TableWidgetProps<T> {
  id: string;
  title: string;
  description?: string;
  columns: ColumnDef[];
  data: T[];
  emptyMessage?: string;
  tableView?: boolean;
  sortConfig?: SortConfig;
  onSort?: (column: string) => void;
  onMaximize?: () => void;
  onToggleTableView?: () => void;
  renderTableRow: (item: T, index: number) => ReactNode;
  renderCardView: (item: T, index: number) => ReactNode;
  className?: string;
}

interface SortableHeaderProps {
  column: string;
  label: string;
  currentSort?: SortConfig;
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

export function TableWidget<T>({
  id,
  title,
  description,
  columns,
  data,
  emptyMessage = "No data available",
  tableView = false,
  sortConfig,
  onSort,
  onMaximize,
  onToggleTableView,
  renderTableRow,
  renderCardView,
  className,
}: TableWidgetProps<T>) {
  return (
    <DashboardWidget
      id={id}
      title={title}
      description={description}
      onMaximize={onMaximize}
      onToggleTableView={onToggleTableView}
      showTableViewButton={true}
      showMaximizeButton={true}
      className={className}
    >
      <div className="space-y-4">
        {data.length === 0 ? (
          <p className="text-center text-muted-foreground py-8">{emptyMessage}</p>
        ) : tableView ? (
          <div className="overflow-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  {columns.map((col) =>
                    col.sortable !== false && onSort ? (
                      <SortableHeader
                        key={col.key}
                        column={col.key}
                        label={col.label}
                        currentSort={sortConfig}
                        onSort={onSort}
                      />
                    ) : (
                      <th key={col.key} className="text-left p-2">
                        {col.label}
                      </th>
                    )
                  )}
                </tr>
              </thead>
              <tbody>
                {data.map((item, index) => renderTableRow(item, index))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="space-y-3">
            {data.map((item, index) => renderCardView(item, index))}
          </div>
        )}
      </div>
    </DashboardWidget>
  );
}
