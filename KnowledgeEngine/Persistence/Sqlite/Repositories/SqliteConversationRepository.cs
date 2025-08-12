using KnowledgeEngine.Persistence.Conversations;
using Microsoft.Data.Sqlite;

namespace KnowledgeEngine.Persistence.Sqlite.Repositories;

/// <summary>
/// SQLite implementation of IConversationRepository for persistent chat history
/// Replaces MongoDB conversation storage with local database
/// </summary>
public class SqliteConversationRepository : IConversationRepository
{
    private readonly SqliteDbContext _dbContext;

    public SqliteConversationRepository(SqliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates a new conversation and returns its ID
    /// </summary>
    public async Task<string> CreateAsync(string? knowledgeId, CancellationToken ct = default)
    {
        var conversationId = Guid.NewGuid().ToString();
        
        const string sql = """
            INSERT INTO Conversations (ConversationId, KnowledgeId, CreatedAt)
            VALUES (@conversationId, @knowledgeId, CURRENT_TIMESTAMP)
            """;

        var connection = await _dbContext.GetConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@conversationId", conversationId);
        command.Parameters.AddWithValue("@knowledgeId", knowledgeId ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(ct);
        return conversationId;
    }

    /// <summary>
    /// Retrieves all messages for a conversation in chronological order
    /// </summary>
    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string conversationId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Role, Content, Timestamp
            FROM Messages 
            WHERE ConversationId = @conversationId 
            ORDER BY MessageIndex ASC, Timestamp ASC
            """;

        var messages = new List<ChatMessage>();
        var connection = await _dbContext.GetConnectionAsync();

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@conversationId", conversationId);

        using var reader = await command.ExecuteReaderAsync(ct);
        
        while (await reader.ReadAsync(ct))
        {
            messages.Add(new ChatMessage
            {
                Role = reader.GetString(0),
                Content = reader.GetString(1),
                Ts = reader.GetDateTime(2)
            });
        }

        return messages;
    }

    /// <summary>
    /// Appends a new message to an existing conversation
    /// </summary>
    public async Task AppendMessageAsync(string conversationId, ChatMessage message, CancellationToken ct = default)
    {
        // Get the next message index for proper ordering
        var nextIndex = await GetNextMessageIndexAsync(conversationId, ct);

        const string sql = """
            INSERT INTO Messages 
            (ConversationId, Role, Content, Timestamp, MessageIndex)
            VALUES (@conversationId, @role, @content, @timestamp, @messageIndex)
            """;

        // Update conversation timestamp
        const string updateConversationSql = """
            UPDATE Conversations 
            SET UpdatedAt = CURRENT_TIMESTAMP 
            WHERE ConversationId = @conversationId
            """;

        var connection = await _dbContext.GetConnectionAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Insert message
            using var command = new SqliteCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@conversationId", conversationId);
            command.Parameters.AddWithValue("@role", message.Role);
            command.Parameters.AddWithValue("@content", message.Content);
            command.Parameters.AddWithValue("@timestamp", message.Ts);
            command.Parameters.AddWithValue("@messageIndex", nextIndex);
            
            await command.ExecuteNonQueryAsync(ct);

            // Update conversation timestamp
            using var updateCommand = new SqliteCommand(updateConversationSql, connection, transaction);
            updateCommand.Parameters.AddWithValue("@conversationId", conversationId);
            await updateCommand.ExecuteNonQueryAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>
    /// Gets the next message index for a conversation to ensure proper ordering
    /// </summary>
    private async Task<int> GetNextMessageIndexAsync(string conversationId, CancellationToken ct)
    {
        const string sql = """
            SELECT COALESCE(MAX(MessageIndex), -1) + 1 
            FROM Messages 
            WHERE ConversationId = @conversationId
            """;

        var connection = await _dbContext.GetConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@conversationId", conversationId);

        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Gets conversation metadata (useful for conversation management UI)
    /// </summary>
    public async Task<(string ConversationId, string? KnowledgeId, DateTime CreatedAt, DateTime UpdatedAt)?>
        GetConversationMetadataAsync(string conversationId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT ConversationId, KnowledgeId, CreatedAt, UpdatedAt
            FROM Conversations 
            WHERE ConversationId = @conversationId
            """;

        var connection = await _dbContext.GetConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@conversationId", conversationId);

        using var reader = await command.ExecuteReaderAsync(ct);
        
        if (!await reader.ReadAsync(ct))
            return null;

        return (
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.GetDateTime(2),
            reader.GetDateTime(3)
        );
    }

    /// <summary>
    /// Lists recent conversations for management UI
    /// </summary>
    public async Task<IEnumerable<(string ConversationId, string? KnowledgeId, DateTime UpdatedAt, int MessageCount)>>
        GetRecentConversationsAsync(int limit = 50, CancellationToken ct = default)
    {
        const string sql = """
            SELECT c.ConversationId, c.KnowledgeId, c.UpdatedAt, COUNT(m.Id) as MessageCount
            FROM Conversations c
            LEFT JOIN Messages m ON c.ConversationId = m.ConversationId
            WHERE c.IsArchived = 0
            GROUP BY c.ConversationId, c.KnowledgeId, c.UpdatedAt
            ORDER BY c.UpdatedAt DESC
            LIMIT @limit
            """;

        var conversations = new List<(string, string?, DateTime, int)>();
        var connection = await _dbContext.GetConnectionAsync();

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@limit", limit);

        using var reader = await command.ExecuteReaderAsync(ct);
        
        while (await reader.ReadAsync(ct))
        {
            conversations.Add((
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.GetDateTime(2),
                reader.GetInt32(3)
            ));
        }

        return conversations;
    }
}