using Knowledge.Analytics.Models;
using Knowledge.Contracts.Types;
using Knowledge.Data;
using Knowledge.Data.Interfaces;
using Knowledge.Entities;

namespace Knowledge.Analytics.Services;

public class SqliteUsageTrackingService : IUsageTrackingService
{
    private readonly IUsageMetricsRepository _usageRepository;
    private readonly IOllamaRepository _ollamaRepository;
    private readonly SqliteDbContext _dbContext;

    public SqliteUsageTrackingService(IUsageMetricsRepository usageRepository, IOllamaRepository ollamaRepository, SqliteDbContext dbContext)
    {
        _usageRepository = usageRepository;
        _ollamaRepository = ollamaRepository;
        _dbContext = dbContext;
    }

    public async Task TrackUsageAsync(UsageMetric metric, CancellationToken cancellationToken = default)
    {
        var record = new UsageMetricRecord
        {
            Id = metric.Id,
            ConversationId = metric.ConversationId,
            KnowledgeId = metric.KnowledgeId,
            Provider = metric.Provider.ToString(),
            ModelName = metric.ModelName,
            InputTokens = metric.InputTokens,
            OutputTokens = metric.OutputTokens,
            Temperature = metric.Temperature,
            UsedAgentCapabilities = metric.UsedAgentCapabilities,
            ToolExecutions = metric.ToolExecutions,
            ResponseTimeMs = metric.ResponseTime.TotalMilliseconds,
            Timestamp = metric.Timestamp,
            ClientId = metric.ClientId,
            WasSuccessful = metric.WasSuccessful,
            ErrorMessage = metric.ErrorMessage
        };

        await _usageRepository.TrackUsageAsync(record, cancellationToken);
    }

    public async Task<IEnumerable<UsageMetric>> GetUsageHistoryAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var records = await _usageRepository.GetUsageHistoryAsync(days, cancellationToken);
        
        return records.Select(r => new UsageMetric
        {
            Id = r.Id,
            ConversationId = r.ConversationId,
            KnowledgeId = r.KnowledgeId,
            Provider = Enum.Parse<AiProvider>(r.Provider),
            ModelName = r.ModelName,
            InputTokens = r.InputTokens,
            OutputTokens = r.OutputTokens,
            Temperature = r.Temperature,
            UsedAgentCapabilities = r.UsedAgentCapabilities,
            ToolExecutions = r.ToolExecutions,
            ResponseTime = TimeSpan.FromMilliseconds(r.ResponseTimeMs),
            Timestamp = r.Timestamp,
            ClientId = r.ClientId,
            WasSuccessful = r.WasSuccessful,
            ErrorMessage = r.ErrorMessage
        });
    }

    public async Task<IEnumerable<ModelUsageStats>> GetModelUsageStatsAsync(CancellationToken cancellationToken = default)
    {
        // Use existing complex SQL queries for aggregated stats
        const string sql = """
            SELECT 
                ModelName,
                Provider,
                COUNT(DISTINCT ConversationId) as ConversationCount,
                SUM(InputTokens + OutputTokens) as TotalTokens,
                CAST(AVG(InputTokens + OutputTokens) AS INTEGER) as AverageTokensPerRequest,
                AVG(ResponseTimeMs) as AverageResponseTimeMs,
                MAX(Timestamp) as LastUsed,
                SUM(CASE WHEN WasSuccessful = 1 THEN 1 ELSE 0 END) as SuccessfulRequests,
                SUM(CASE WHEN WasSuccessful = 0 THEN 1 ELSE 0 END) as FailedRequests
            FROM UsageMetrics 
            WHERE ModelName IS NOT NULL
            GROUP BY ModelName, Provider
            ORDER BY TotalTokens DESC
            """;

        var connection = await _dbContext.GetConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        var stats = new List<ModelUsageStats>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var modelName = reader.GetString(0);
            var provider = Enum.Parse<AiProvider>(reader.GetString(1));
            
            // Get tool support info for Ollama models
            bool? supportsTools = null;
            if (provider == AiProvider.Ollama)
            {
                var ollamaModel = await _ollamaRepository.GetModelAsync(modelName, cancellationToken);
                supportsTools = ollamaModel?.SupportsTools;
            }

            stats.Add(new ModelUsageStats
            {
                ModelName = modelName,
                Provider = provider,
                ConversationCount = reader.GetInt32(2),
                TotalTokens = reader.GetInt32(3),
                AverageTokensPerRequest = reader.GetInt32(4),
                AverageResponseTime = TimeSpan.FromMilliseconds(reader.GetDouble(5)),
                LastUsed = reader.GetDateTime(6),
                SupportsTools = supportsTools,
                SuccessfulRequests = reader.GetInt32(7),
                FailedRequests = reader.GetInt32(8)
            });
        }

        return stats;
    }

    public async Task<IEnumerable<KnowledgeUsageStats>> GetKnowledgeUsageStatsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                kc.CollectionId as KnowledgeId,
                kc.Name as KnowledgeName,
                kc.DocumentCount,
                kc.ChunkCount,
                kc.VectorStore,
                kc.CreatedAt,
                COALESCE(SUM(kd.FileSize), 0) as TotalFileSize,
                COUNT(DISTINCT um.ConversationId) as ConversationCount,
                COUNT(um.Id) as QueryCount,
                MAX(um.Timestamp) as LastQueried
            FROM KnowledgeCollections kc
            LEFT JOIN KnowledgeDocuments kd ON kc.CollectionId = kd.CollectionId
            LEFT JOIN UsageMetrics um ON kc.CollectionId = um.KnowledgeId
            GROUP BY kc.CollectionId, kc.Name, kc.DocumentCount, kc.ChunkCount, kc.VectorStore, kc.CreatedAt
            ORDER BY QueryCount DESC, kc.Name
            """;

        var connection = await _dbContext.GetConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        var stats = new List<KnowledgeUsageStats>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            stats.Add(new KnowledgeUsageStats
            {
                KnowledgeId = reader.GetString(0),
                KnowledgeName = reader.GetString(1),
                DocumentCount = reader.GetInt32(2),
                ChunkCount = reader.GetInt32(3),
                VectorStore = reader.GetString(4),
                CreatedAt = reader.GetDateTime(5),
                TotalFileSize = reader.GetInt64(6),
                ConversationCount = reader.GetInt32(7),
                QueryCount = reader.GetInt32(8),
                LastQueried = reader.IsDBNull(9) ? DateTime.MinValue : reader.GetDateTime(9)
            });
        }

        return stats;
    }

    public async Task<UsageMetric?> GetUsageByConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var record = await _usageRepository.GetUsageByConversationAsync(conversationId, cancellationToken);
        
        if (record == null) return null;

        return new UsageMetric
        {
            Id = record.Id,
            ConversationId = record.ConversationId,
            KnowledgeId = record.KnowledgeId,
            Provider = Enum.Parse<AiProvider>(record.Provider),
            ModelName = record.ModelName,
            InputTokens = record.InputTokens,
            OutputTokens = record.OutputTokens,
            Temperature = record.Temperature,
            UsedAgentCapabilities = record.UsedAgentCapabilities,
            ToolExecutions = record.ToolExecutions,
            ResponseTime = TimeSpan.FromMilliseconds(record.ResponseTimeMs),
            Timestamp = record.Timestamp,
            ClientId = record.ClientId,
            WasSuccessful = record.WasSuccessful,
            ErrorMessage = record.ErrorMessage
        };
    }
}