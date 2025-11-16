# Analytics Components

This directory contains reusable components for the Analytics Dashboard.

## Components Overview

### KpiCard

A reusable component for displaying Key Performance Indicator (KPI) cards in the analytics dashboard.

**Props:**
- `id` (string, required): Unique identifier for the card
- `title` (string, required): The title/label of the KPI
- `icon` (ReactNode, required): Icon component to display
- `value` (string | number, required): The KPI value to display
- `description` (string, required): Description text below the value
- `isText` (boolean, optional): If true, displays value as text instead of large number
- `className` (string, optional): Additional CSS classes

**Example Usage:**
```tsx
<KpiCard
  id="kpi-1"
  title="Total Conversations"
  icon={<ConversationIcon className="h-6 w-6" />}
  value={1234}
  description="Across all models"
/>
```

### DashboardWidget

A base component providing common structure for dashboard widgets including header, actions, and content area.

**Props:**
- `id` (string, required): Unique identifier for the widget
- `title` (string, required): Widget title
- `description` (string, optional): Widget description
- `children` (ReactNode, required): Widget content
- `onMaximize` (() => void, optional): Handler for maximize button
- `onToggleTableView` (() => void, optional): Handler for table view toggle
- `showTableViewButton` (boolean, optional, default: false): Show table view button
- `showMaximizeButton` (boolean, optional, default: true): Show maximize button
- `className` (string, optional): Additional CSS classes

**Example Usage:**
```tsx
<DashboardWidget
  id="my-widget"
  title="My Widget"
  description="Widget description"
  onMaximize={() => handleMaximize("my-widget")}
  showMaximizeButton={true}
>
  <div>Widget content here</div>
</DashboardWidget>
```

### TableWidget

An advanced widget component that extends DashboardWidget with sortable table and card view support.

**Props:**
- `id` (string, required): Unique identifier for the widget
- `title` (string, required): Widget title
- `description` (string, optional): Widget description
- `columns` (ColumnDef[], required): Column definitions for the table
  - `key` (string): Data key for the column
  - `label` (string): Column header label
  - `sortable` (boolean, optional, default: true): Whether column is sortable
- `data` (T[], required): Array of data to display
- `emptyMessage` (string, optional): Message when data is empty
- `tableView` (boolean, optional, default: false): Current view mode
- `sortConfig` (SortConfig, optional): Current sort configuration
  - `column` (string): Column being sorted
  - `direction` ('asc' | 'desc'): Sort direction
- `onSort` ((column: string) => void, optional): Sort handler
- `onMaximize` (() => void, optional): Maximize handler
- `onToggleTableView` (() => void, optional): View toggle handler
- `renderTableRow` ((item: T, index: number) => ReactNode, required): Table row renderer
- `renderCardView` ((item: T, index: number) => ReactNode, required): Card view renderer
- `className` (string, optional): Additional CSS classes

**Example Usage:**
```tsx
<TableWidget
  id="model-performance"
  title="Model Performance"
  description="Usage statistics for AI models"
  columns={[
    { key: "modelName", label: "Model" },
    { key: "provider", label: "Provider" },
    { key: "conversationCount", label: "Conversations" },
  ]}
  data={sortedData}
  emptyMessage="No data available"
  tableView={tableViewEnabled}
  sortConfig={currentSort}
  onSort={handleSort}
  onMaximize={handleMaximize}
  onToggleTableView={handleToggleView}
  renderTableRow={(model, index) => (
    <tr key={index}>
      <td>{model.modelName}</td>
      <td>{model.provider}</td>
      <td>{model.conversationCount}</td>
    </tr>
  )}
  renderCardView={(model, index) => (
    <div key={index} className="p-4 border rounded">
      <div>{model.modelName}</div>
      <div>{model.conversationCount} conversations</div>
    </div>
  )}
/>
```

## Component Hierarchy

```
TableWidget
  └── DashboardWidget
       └── Card (shadcn/ui)
            ├── CardHeader
            └── CardContent
```

## Design Patterns

All components follow these patterns:
- Use shadcn/ui components for consistent styling
- Support both light and dark modes
- Accept className props for customization
- Use TypeScript for type safety
- Follow accessibility best practices

## Migration Guide

### From Inline Widget to TableWidget

**Before:**
```tsx
<Card className="h-full">
  <CardHeader>
    <CardTitle>My Table</CardTitle>
    <Button onClick={handleMaximize}>
      <Maximize2 />
    </Button>
  </CardHeader>
  <CardContent>
    {tableView ? (
      <table>...</table>
    ) : (
      <div>...</div>
    )}
  </CardContent>
</Card>
```

**After:**
```tsx
<TableWidget
  id="my-table"
  title="My Table"
  columns={columns}
  data={data}
  tableView={tableView}
  onMaximize={handleMaximize}
  onToggleTableView={handleToggleView}
  renderTableRow={(item, i) => <tr>...</tr>}
  renderCardView={(item, i) => <div>...</div>}
/>
```

This reduces code by ~60-80 lines per widget while maintaining all functionality.
