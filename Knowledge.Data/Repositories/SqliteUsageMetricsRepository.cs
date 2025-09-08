using Knowledge.Data.Interfaces;
using Knowledge.Entities;
using Microsoft.Data.Sqlite;

namespace Knowledge.Data.Repositories;

/// <summary>
/// SQLite repository for usage metrics tracking
/// </summary>
public class SqliteUsageMetricsRepository : IUsageMetricsRepository
{
    private readonly SqliteDbContext _dbContext;

    public SqliteUsageMetricsRepository(SqliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task TrackUsageAsync(UsageMetricRecord metric, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO UsageMetrics (
                Id, ConversationId, KnowledgeId, Provider, ModelName, 
                InputTokens, OutputTokens, Temperature, UsedAgentCapabilities, 
                ToolExecutions, ResponseTimeMs, Timestamp, ClientId, 
                WasSuccessful, ErrorMessage
            ) VALUES (
                @Id, @ConversationId, @KnowledgeId, @Provider, @ModelName,
                @InputTokens, @OutputTokens, @Temperature, @UsedAgentCapabilities,
                @ToolExecutions, @ResponseTimeMs, @Timestamp, @ClientId,
                @WasSuccessful, @ErrorMessage
            )
            """;

        using var connection = await _dbContext.CreateConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        
        command.Parameters.AddWithValue("@Id", metric.Id);
        command.Parameters.AddWithValue("@ConversationId", metric.ConversationId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@KnowledgeId", metric.KnowledgeId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Provider", metric.Provider);
        command.Parameters.AddWithValue("@ModelName", metric.ModelName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@InputTokens", metric.InputTokens);
        command.Parameters.AddWithValue("@OutputTokens", metric.OutputTokens);
        command.Parameters.AddWithValue("@Temperature", metric.Temperature);
        command.Parameters.AddWithValue("@UsedAgentCapabilities", metric.UsedAgentCapabilities);
        command.Parameters.AddWithValue("@ToolExecutions", metric.ToolExecutions);
        command.Parameters.AddWithValue("@ResponseTimeMs", metric.ResponseTimeMs);
        command.Parameters.AddWithValue("@Timestamp", metric.Timestamp);
        command.Parameters.AddWithValue("@ClientId", metric.ClientId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@WasSuccessful", metric.WasSuccessful);
        command.Parameters.AddWithValue("@ErrorMessage", metric.ErrorMessage ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IEnumerable<UsageMetricRecord>> GetUsageHistoryAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, ConversationId, KnowledgeId, Provider, ModelName, 
                   InputTokens, OutputTokens, Temperature, UsedAgentCapabilities, 
                   ToolExecutions, ResponseTimeMs, Timestamp, ClientId, 
                   WasSuccessful, ErrorMessage
            FROM UsageMetrics 
            WHERE Timestamp >= datetime('now', '-' || @Days || ' days')
            ORDER BY Timestamp DESC
            """;

        using var connection = await _dbContext.CreateConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@Days", days);

        var metrics = new List<UsageMetricRecord>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            metrics.Add(new UsageMetricRecord
            {
                Id = reader.GetString(0),
                ConversationId = reader.IsDBNull(1) ? null : reader.GetString(1),
                KnowledgeId = reader.IsDBNull(2) ? null : reader.GetString(2),
                Provider = reader.GetString(3),
                ModelName = reader.IsDBNull(4) ? null : reader.GetString(4),
                InputTokens = reader.GetInt32(5),
                OutputTokens = reader.GetInt32(6),
                Temperature = reader.GetDouble(7),
                UsedAgentCapabilities = reader.GetBoolean(8),
                ToolExecutions = reader.GetInt32(9),
                ResponseTimeMs = reader.GetDouble(10),
                Timestamp = reader.GetDateTime(11),
                ClientId = reader.IsDBNull(12) ? null : reader.GetString(12),
                WasSuccessful = reader.GetBoolean(13),
                ErrorMessage = reader.IsDBNull(14) ? null : reader.GetString(14)
            });
        }

        return metrics;
    }

    public async Task<UsageMetricRecord?> GetUsageByConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, ConversationId, KnowledgeId, Provider, ModelName, 
                   InputTokens, OutputTokens, Temperature, UsedAgentCapabilities, 
                   ToolExecutions, ResponseTimeMs, Timestamp, ClientId, 
                   WasSuccessful, ErrorMessage
            FROM UsageMetrics 
            WHERE ConversationId = @ConversationId
            ORDER BY Timestamp DESC 
            LIMIT 1
            """;

        using var connection = await _dbContext.CreateConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@ConversationId", conversationId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        if (await reader.ReadAsync(cancellationToken))
        {
            return new UsageMetricRecord
            {
                Id = reader.GetString(0),
                ConversationId = reader.IsDBNull(1) ? null : reader.GetString(1),
                KnowledgeId = reader.IsDBNull(2) ? null : reader.GetString(2),
                Provider = reader.GetString(3),
                ModelName = reader.IsDBNull(4) ? null : reader.GetString(4),
                InputTokens = reader.GetInt32(5),
                OutputTokens = reader.GetInt32(6),
                Temperature = reader.GetDouble(7),
                UsedAgentCapabilities = reader.GetBoolean(8),
                ToolExecutions = reader.GetInt32(9),
                ResponseTimeMs = reader.GetDouble(10),
                Timestamp = reader.GetDateTime(11),
                ClientId = reader.IsDBNull(12) ? null : reader.GetString(12),
                WasSuccessful = reader.GetBoolean(13),
                ErrorMessage = reader.IsDBNull(14) ? null : reader.GetString(14)
            };
        }

        return null;
    }
}