import { Page } from '@playwright/test';

/**
 * Mock API helpers for Playwright E2E tests
 * These functions mock common API endpoints to avoid dependency on a running backend
 */

/**
 * Mock an empty knowledge bases list
 */
export async function mockEmptyKnowledgeBases(page: Page) {
  await page.route('/api/knowledge', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([]),
    });
  });
}

/**
 * Mock a list of knowledge bases with sample data
 */
export async function mockKnowledgeBases(page: Page, data?: any[]) {
  const defaultData = [
    {
      id: 'kb-1',
      name: 'Test Knowledge Base 1',
      description: 'Test description 1',
      createdAt: '2024-01-01T00:00:00Z',
    },
    {
      id: 'kb-2',
      name: 'Test Knowledge Base 2',
      description: 'Test description 2',
      createdAt: '2024-01-02T00:00:00Z',
    },
  ];

  await page.route('/api/knowledge', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(data || defaultData),
    });
  });
}

/**
 * Mock Ollama models endpoint
 */
export async function mockOllamaModels(page: Page, models?: string[]) {
  const defaultModels = ['llama2', 'mistral', 'codellama'];
  
  await page.route('/api/ollama/models', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(models || defaultModels),
    });
  });
}

/**
 * Mock analytics endpoints with sample data
 */
export async function mockAnalyticsEndpoints(page: Page) {
  // Mock provider analytics
  await page.route('/api/analytics/providers', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { provider: 'OpenAI', count: 100, avgResponseTime: 1.5 },
        { provider: 'Anthropic', count: 50, avgResponseTime: 1.2 },
      ]),
    });
  });

  // Mock provider accounts
  await page.route('/api/analytics/providers/accounts', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          provider: 'OpenAI',
          isConnected: true,
          apiKeyConfigured: true,
          lastSyncAt: '2024-01-15T10:30:00Z',
          balance: 150.00,
          balanceUnit: 'USD',
          monthlyUsage: 85.25
        },
        {
          provider: 'Anthropic',
          isConnected: true,
          apiKeyConfigured: true,
          lastSyncAt: '2024-01-15T10:25:00Z',
          balance: 200.00,
          balanceUnit: 'USD',
          monthlyUsage: 40.25
        },
        {
          provider: 'Google',
          isConnected: false,
          apiKeyConfigured: false,
          monthlyUsage: 0
        },
        {
          provider: 'Ollama',
          isConnected: true,
          apiKeyConfigured: true,
          lastSyncAt: '2024-01-15T10:20:00Z',
          monthlyUsage: 0
        },
      ]),
    });
  });

  // Mock system health
  await page.route('/api/analytics/health', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        status: 'healthy',
        uptime: 123456,
        memoryUsage: 0.5,
      }),
    });
  });

  // Mock conversation stats
  await page.route('/api/analytics/conversations', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        total: 1000,
        active: 50,
        avgLength: 10,
      }),
    });
  });

  // Mock model performance
  await page.route('/api/analytics/models', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          modelName: 'gpt-4',
          provider: 'OpenAI',
          conversationCount: 50,
          totalTokens: 100000,
          averageTokensPerRequest: 2000,
          averageResponseTime: 2.1,
          lastUsed: '2024-01-15T10:30:00Z',
          successfulRequests: 475,
          failedRequests: 25,
          successRate: 0.95,
          totalRequests: 500
        },
        {
          modelName: 'claude-3-sonnet',
          provider: 'Anthropic',
          conversationCount: 30,
          totalTokens: 75000,
          averageTokensPerRequest: 2500,
          averageResponseTime: 1.8,
          lastUsed: '2024-01-15T09:15:00Z',
          successfulRequests: 279,
          failedRequests: 21,
          successRate: 0.93,
          totalRequests: 300
        },
      ]),
    });
  });

  // Mock knowledge bases analytics
  await page.route('/api/analytics/knowledge-bases', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          knowledgeId: 'kb-1',
          knowledgeName: 'Test Knowledge Base 1',
          documentCount: 50,
          chunkCount: 500,
          conversationCount: 25,
          queryCount: 100,
          lastQueried: '2024-01-15T10:00:00Z',
          createdAt: '2024-01-01T00:00:00Z',
          vectorStore: 'Qdrant',
          totalFileSize: 1048576
        },
        {
          knowledgeId: 'kb-2',
          knowledgeName: 'Test Knowledge Base 2',
          documentCount: 30,
          chunkCount: 300,
          conversationCount: 15,
          queryCount: 75,
          lastQueried: '2024-01-14T15:30:00Z',
          createdAt: '2024-01-05T00:00:00Z',
          vectorStore: 'Qdrant',
          totalFileSize: 524288
        },
      ]),
    });
  });

  // Mock usage trends (matches pattern /api/analytics/usage-trends?days=7)
  await page.route(/\/api\/analytics\/usage-trends/, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          date: '2024-01-08',
          totalRequests: 100,
          successfulRequests: 95,
          totalTokens: 200000,
          uniqueConversations: 20
        },
        {
          date: '2024-01-09',
          totalRequests: 150,
          successfulRequests: 145,
          totalTokens: 300000,
          uniqueConversations: 30
        },
        {
          date: '2024-01-10',
          totalRequests: 120,
          successfulRequests: 115,
          totalTokens: 240000,
          uniqueConversations: 25
        },
        {
          date: '2024-01-11',
          totalRequests: 180,
          successfulRequests: 175,
          totalTokens: 360000,
          uniqueConversations: 35
        },
        {
          date: '2024-01-12',
          totalRequests: 200,
          successfulRequests: 195,
          totalTokens: 400000,
          uniqueConversations: 40
        },
        {
          date: '2024-01-13',
          totalRequests: 170,
          successfulRequests: 165,
          totalTokens: 340000,
          uniqueConversations: 32
        },
        {
          date: '2024-01-14',
          totalRequests: 190,
          successfulRequests: 185,
          totalTokens: 380000,
          uniqueConversations: 38
        },
      ]),
    });
  });

  // Mock cost breakdown (matches pattern /api/analytics/cost-breakdown?days=30)
  await page.route(/\/api\/analytics\/cost-breakdown/, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          provider: 'OpenAI',
          totalCost: 85.25,
          period: {
            startDate: '2023-12-15',
            endDate: '2024-01-15',
            days: 30
          }
        },
        {
          provider: 'Anthropic',
          totalCost: 40.25,
          period: {
            startDate: '2023-12-15',
            endDate: '2024-01-15',
            days: 30
          }
        },
      ]),
    });
  });

  // Mock Ollama usage analytics (matches pattern /api/analytics/ollama/usage?days=30)
  await page.route(/\/api\/analytics\/ollama\/usage/, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        totalRequests: 50,
        totalTokens: 100000,
        averageTokensPerRequest: 2000,
        activeModels: ['llama2', 'mistral'],
        period: {
          startDate: '2023-12-15',
          endDate: '2024-01-15',
          days: 30
        }
      }),
    });
  });
}

/**
 * Mock all common endpoints at once
 * Useful for tests that just need the page to load without backend
 */
export async function mockCommonEndpoints(page: Page) {
  await mockKnowledgeBases(page);
  await mockOllamaModels(page);
  await mockAnalyticsEndpoints(page);
}
