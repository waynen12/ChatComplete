# 📊 Model & Usage Analytics Dashboard - Comprehensive Implementation Plan

## Overview

This document outlines the complete plan for implementing a comprehensive Model & Usage Analytics Dashboard that will provide users with detailed insights into their AI model usage, costs, performance, and analytics across all providers (OpenAI, Anthropic, Google AI, and Ollama).

## 📁 Available Data Sources

### 🗃️ Local SQLite Database (Rich Data Available)

#### 1. Ollama Models (`OllamaModels` table)
- **Model metadata**: name, display name, size, family, parameter size
- **Technical details**: quantization level, format, template, parameters
- **Usage tracking**: installation date, last used, availability status
- **Capabilities**: SupportsTools flag (recently added)
- **Download management**: progress tracking and status

#### 2. Conversation Analytics (`Conversations` + `Messages` tables)
- **Usage statistics**: conversations per model/provider counts
- **Token analytics**: message counts, token usage per conversation
- **Provider breakdown**: usage patterns by provider (OpenAI, Gemini, Anthropic, Ollama)
- **Time-based analytics**: usage trends and patterns over time

#### 3. Knowledge Base Usage (`KnowledgeCollections`, `KnowledgeDocuments`)
- **Model-knowledge correlation**: which models are used with which knowledge bases
- **Processing statistics**: document processing stats, chunk counts
- **Performance analytics**: model performance per knowledge domain

### 🌐 External Provider APIs (Available but Requires Implementation)

#### 1. OpenAI API
- ✅ **Usage statistics**: `/v1/usage`
- ✅ **Billing information**: `/v1/dashboard/billing/subscription`
- ✅ **Credit balance**: `/v1/dashboard/billing/credit_grants`
- ✅ **Token usage per model**: Real-time usage tracking
- ✅ **Export capabilities**: CSV format for detailed analysis

#### 2. Anthropic Claude API
- ✅ **Usage reports**: `/v1/organizations/usage_report/messages`
- ✅ **Cost reports**: `/v1/organizations/cost_report`
- ✅ **Credit balance tracking**: Real-time balance monitoring
- ✅ **Admin API**: Requires admin API key (sk-ant-admin...)
- ✅ **Workspace tracking**: Claude Code workspace integration

#### 3. Google Gemini API
- ✅ **Cloud Billing integration**: Via `generativelanguage.googleapis.com`
- ✅ **Usage monitoring**: Google Cloud Console APIs
- ✅ **Token counting**: `countTokens` API for usage calculation
- ✅ **Firebase Console**: Usage and billing dashboard integration
- ✅ **Budget alerts**: Cost management and monitoring

## 🎨 Navigation & UI Architecture

### Navigation Structure
The analytics dashboard integrates seamlessly with the existing application navigation:

```
┌─────────────────────────────────────────────────────┐
│ [📚 Knowledge] [💬 Chat] [📊 Analytics]             │
└─────────────────────────────────────────────────────┘
```

**Design Philosophy:**
- **Knowledge Management & Chat** remain the primary workflows
- **Analytics** serves as a **supporting tool** for optimization
- Users can access analytics from any page via the navigation bar
- Analytics provide actionable insights to improve knowledge management effectiveness

**Analytics Positioning:**
- Help users identify which knowledge bases get the most use
- Show which models work best for specific content types
- Provide cost optimization opportunities
- Offer performance insights to improve setup and configuration

**Landing Page (Simplified):**
Since analytics is accessible via navigation, the landing page can focus on the primary action:
```
┌─────────────────────────────────┐
│  📚 Manage Knowledge           │  ← Primary workflow entry
└─────────────────────────────────┘
```

**Smart Integration Opportunities:**
1. **Knowledge Listing Page** - Add analytics badges:
   - "📈 234 conversations this month"  
   - "⚡ Avg response: 1.2s"
   - "💰 $12.34 total cost"

2. **In-Chat Analytics** - Contextual performance data:
   - Current session cost
   - Model performance for active knowledge base
   - Provider switching recommendations

3. **Analytics Dashboard** - Focus on actionable insights:
   - "Your most cost-effective model for technical docs is..."
   - "Consider switching Provider X to Provider Y for 30% savings"
   - "Knowledge Base Y has 90% better success rates with Model Z"

## 🎨 Proposed Dashboard UI Layout

### 1. Model Overview Dashboard
```
┌─────────────────────────────────────────────────────────────────┐
│  🤖 Model Management Dashboard                                   │
├─────────────────────────────────────────────────────────────────┤
│  📊 Quick Stats                                                 │
│  ┌────────────┬────────────┬────────────┬─────────────────────┐  │
│  │ 12 Models  │ 3 Providers│ 847 Chats  │ $23.45 This Month   │  │
│  │ Available  │ Connected  │ Total      │ API Costs           │  │
│  └────────────┴────────────┴────────────┴─────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### 2. Provider Status Cards
```
┌──────────── OpenAI ────────────┐ ┌──────── Anthropic ──────────┐
│ 🟢 Active • $12.34 balance     │ │ 🟢 Active • 1,247 credits   │
│ • gpt-4: 234 conversations     │ │ • claude-3.5: 89 chats      │
│ • gpt-3.5: 123 conversations   │ │ • claude-3: 45 chats        │
│ • Usage: 1.2M tokens today     │ │ • Usage: 567K tokens today   │
└────────────────────────────────┘ └──────────────────────────────┘

┌────────── Google AI ───────────┐ ┌─────────── Ollama ──────────┐
│ 🟢 Active • $5.67 this month   │ │ 🟢 Local • 8 models         │
│ • gemini-pro: 67 conversations │ │ • llama3.1:8b: 456 chats    │
│ • gemini-1.5: 34 conversations │ │ • codellama: 123 chats       │
│ • Usage: 890K tokens today     │ │ • Tools: 3/8 models support │
└────────────────────────────────┘ └──────────────────────────────┘
```

### 3. Detailed Model Information Table
```
┌───────────────────────────────────────────────────────────────────────────────────┐
│ Model Name        │Provider│Size  │Tools│Conversations│Last Used │Performance      │
├───────────────────────────────────────────────────────────────────────────────────┤
│ 🦙 llama3.1:8b    │Ollama  │4.7GB │✅   │456          │2 hrs ago │⭐⭐⭐⭐⭐      │
│ 🤖 gpt-4          │OpenAI  │-     │✅   │234          │1 hr ago  │⭐⭐⭐⭐⭐      │
│ 🧠 claude-3.5     │Anthropic│-    │✅   │89           │30m ago   │⭐⭐⭐⭐⭐      │
│ 💎 gemini-pro     │Google  │-     │✅   │67           │45m ago   │⭐⭐⭐⭐        │
│ 💻 codellama      │Ollama  │3.8GB │❌   │123          │3 hrs ago │⭐⭐⭐⭐        │
└───────────────────────────────────────────────────────────────────────────────────┘
```

### 4. Usage Analytics Charts
- **Daily/Weekly/Monthly usage trends** per model
- **Knowledge base correlation** - which models work with which documents  
- **Cost breakdown** by provider
- **Performance metrics** - response times, success rates
- **Token usage patterns** and efficiency analysis
- **Tool usage statistics** for models that support function calling

### 5. Provider Account Integration Panel
```
┌─────────────────────────────────────────────────────────────────┐
│ 🔗 API Account Status                                           │
├─────────────────────────────────────────────────────────────────┤
│ OpenAI   │ ✅ Connected │ 🔄 Sync Usage │ $12.34 balance        │
│ Anthropic│ ✅ Connected │ 🔄 Sync Usage │ 1,247 credits         │  
│ Google   │ ❌ Configure │ ➕ Add API Key│ -                     │
├─────────────────────────────────────────────────────────────────┤
│ 📈 Real-time usage monitoring • 🔄 Auto-refresh every 5min      │
└─────────────────────────────────────────────────────────────────┘
```

## 🛠️ Implementation Plan

### Phase 1: Backend APIs (Priority 1)
**Goal**: Create robust backend infrastructure for usage analytics and provider integration

#### 1.1 Usage Analytics Endpoints
- **`/api/analytics/models`** - Model usage statistics and metadata
- **`/api/analytics/conversations`** - Conversation analytics per model/provider
- **`/api/analytics/usage-trends`** - Time-based usage patterns
- **`/api/analytics/knowledge-correlation`** - Model-knowledge base relationships
- **`/api/analytics/cost-breakdown`** - Provider cost analysis

#### 1.2 External Provider Integration Services
- **OpenAI Integration Service**
  - Usage data synchronization
  - Billing and balance retrieval
  - Token usage tracking
  - Error handling and rate limiting

- **Anthropic Integration Service**
  - Admin API key management
  - Usage report collection
  - Cost report aggregation
  - Credit balance monitoring

- **Google AI Integration Service**
  - Cloud Billing API integration
  - Firebase console data sync
  - Token counting integration
  - Budget alert management

#### 1.3 Data Management Layer
- **Caching strategy** for external API data (Redis or in-memory)
- **Background sync jobs** for periodic data refresh
- **Database schema extensions** for storing external provider data
- **Rate limiting** to respect provider API limits

### Phase 2: Frontend Components (Priority 2)
**Goal**: Create intuitive and informative user interface components

#### 2.1 Dashboard Components
- **Model overview cards** with real-time statistics
- **Provider status indicators** with connection health
- **Quick stats summary** with key metrics
- **Navigation and filtering** for detailed views

#### 2.2 Analytics Visualizations  
- **Usage trend charts** (Chart.js or similar)
- **Cost breakdown pie charts** by provider
- **Performance metrics** with historical data
- **Token usage efficiency** analysis

#### 2.3 Model Management Interface
- **Detailed model information** tables with sorting/filtering
- **Tool support indicators** and capabilities
- **Usage history** and performance tracking
- **Provider account management** and configuration

### Phase 3: Real-time Updates (Priority 3)
**Goal**: Implement live data updates and notifications

#### 3.1 Real-time Data Pipeline
- **WebSocket connections** for live usage updates
- **Server-Sent Events** for dashboard notifications
- **Event-driven architecture** for data changes
- **Optimized refresh strategies** to minimize API calls

#### 3.2 Smart Caching and Sync
- **Intelligent cache invalidation** based on usage patterns
- **Batch API requests** to optimize external provider calls
- **Offline mode support** with cached data
- **Sync conflict resolution** for concurrent updates

#### 3.3 Advanced Features
- **Usage alerts and notifications** for cost thresholds
- **Predictive analytics** for usage forecasting
- **Model recommendation engine** based on performance
- **Export capabilities** for usage reports

## 📊 Data Models and Schemas

### Extended SQLite Schema Additions

#### Provider Usage Tracking Table
```sql
CREATE TABLE ProviderUsage (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Provider VARCHAR(50) NOT NULL,
    ModelName VARCHAR(100),
    UsageDate DATE NOT NULL,
    TokensUsed INTEGER DEFAULT 0,
    CostUSD DECIMAL(10,4) DEFAULT 0,
    RequestCount INTEGER DEFAULT 0,
    SuccessRate DECIMAL(5,2) DEFAULT 100.00,
    AvgResponseTime DECIMAL(8,2),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

#### Provider Account Status Table
```sql
CREATE TABLE ProviderAccounts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Provider VARCHAR(50) NOT NULL UNIQUE,
    IsConnected BOOLEAN DEFAULT 0,
    ApiKeyConfigured BOOLEAN DEFAULT 0,
    LastSyncAt DATETIME,
    Balance DECIMAL(10,4),
    BalanceUnit VARCHAR(20), -- USD, credits, etc.
    MonthlyUsage DECIMAL(10,4) DEFAULT 0,
    ErrorMessage TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

## 🔧 Technical Requirements

### Backend Dependencies
- **ASP.NET 8 Minimal APIs** (existing)
- **Entity Framework Core** or **Dapper** for data access
- **HttpClient** with retry policies for external APIs
- **Background services** for periodic sync jobs
- **Caching layer** (IMemoryCache or Redis)

### Frontend Dependencies  
- **React + Vite** (existing)
- **shadcn/ui** (existing) + additional chart components
- **Chart.js** or **Recharts** for visualizations
- **TanStack Query** for data fetching and caching
- **WebSocket client** for real-time updates

### Infrastructure Considerations
- **Rate limiting** for external provider APIs
- **Error handling** and graceful degradation
- **Security** for API key management
- **Performance** optimization for large datasets
- **Scalability** for multi-user environments

## 🚀 Deployment Strategy

### Development Phase
1. **Local SQLite database** with sample data
2. **Mock external provider APIs** for testing
3. **Component development** with Storybook
4. **Unit tests** for all backend services

### Testing Phase
1. **Integration tests** with real provider APIs (sandbox)
2. **Performance testing** with large datasets  
3. **User acceptance testing** with stakeholders
4. **Security auditing** of API key handling

### Production Deployment
1. **Docker containerization** (existing infrastructure)
2. **Environment configuration** for provider API keys
3. **Monitoring and alerting** for sync job health
4. **Backup strategy** for usage analytics data

## 📈 Success Metrics

### User Experience
- **Dashboard load time** < 2 seconds
- **Real-time update latency** < 5 seconds
- **User engagement** with analytics features
- **Reduced support queries** about model usage

### Technical Performance
- **API response times** < 500ms for local data
- **External provider sync** success rate > 95%
- **Cache hit ratio** > 80% for frequently accessed data
- **System uptime** > 99.5%

### Business Value
- **Increased user retention** through better insights
- **Cost optimization** through usage awareness
- **Model performance** improvement through analytics
- **Provider relationship** optimization through usage data

## 🔄 Maintenance and Updates

### Regular Tasks
- **Weekly sync** of external provider API changes
- **Monthly review** of usage patterns and optimization opportunities
- **Quarterly assessment** of new provider features and integrations
- **Annual security audit** of API key management

### Monitoring
- **Health checks** for all external provider connections
- **Performance monitoring** for database queries
- **Error tracking** for sync job failures
- **User activity** analytics for feature usage

---

**Implementation Start Date**: [To be determined]
**Target Completion**: Phase 1 (4 weeks), Phase 2 (6 weeks), Phase 3 (4 weeks)
**Team**: Backend + Frontend developers
**Dependencies**: Current Agent Implementation branch merge to main