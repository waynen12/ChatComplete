using KnowledgeEngine.Persistence.Sqlite.Services;
using Microsoft.Data.Sqlite;

namespace KnowledgeEngine.Persistence.Sqlite.Repositories;

/// <summary>
/// SQLite repository for application settings with encryption support
/// Handles both plain text and encrypted sensitive configuration
/// </summary>
public class SqliteAppSettingsRepository
{
    private readonly SqliteDbContext _dbContext;

    public SqliteAppSettingsRepository(SqliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets a configuration value, automatically decrypting if encrypted
    /// </summary>
    public async Task<string?> GetValueAsync(string name, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Value, EncryptedValue, IsEncrypted 
            FROM AppSettings 
            WHERE Name = @name
            """;

        var connection = await _dbContext.GetConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@name", name);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var isEncrypted = reader.GetBoolean(2);
        
        if (isEncrypted)
        {
            if (reader.IsDBNull(1))
                return null;
            var encryptedBytes = (byte[])reader[1];
            return EncryptionService.Decrypt(encryptedBytes);
        }
        else
        {
            return reader.IsDBNull(0) ? null : reader.GetString(0);
        }
    }

    /// <summary>
    /// Sets a configuration value, encrypting if marked as sensitive
    /// </summary>
    public async Task SetValueAsync(
        string name, 
        string? value, 
        bool isEncrypted = false, 
        string? description = null,
        string category = "General",
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO AppSettings 
            (Name, Value, EncryptedValue, IsEncrypted, Description, Category, UpdatedAt)
            VALUES (@name, @value, @encryptedValue, @isEncrypted, @description, @category, CURRENT_TIMESTAMP)
            ON CONFLICT(Name) DO UPDATE SET
                Value = @value,
                EncryptedValue = @encryptedValue,
                IsEncrypted = @isEncrypted,
                Description = COALESCE(@description, Description),
                Category = @category,
                UpdatedAt = CURRENT_TIMESTAMP
            """;

        var connection = await _dbContext.GetConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@description", description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@category", category);
        command.Parameters.AddWithValue("@isEncrypted", isEncrypted);

        if (isEncrypted && !string.IsNullOrEmpty(value))
        {
            command.Parameters.AddWithValue("@value", DBNull.Value);
            command.Parameters.AddWithValue("@encryptedValue", EncryptionService.Encrypt(value));
        }
        else
        {
            command.Parameters.AddWithValue("@value", value ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@encryptedValue", DBNull.Value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all settings in a category (useful for configuration UI)
    /// </summary>
    public async Task<Dictionary<string, string?>> GetCategoryAsync(
        string category, 
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Name, Value, EncryptedValue, IsEncrypted 
            FROM AppSettings 
            WHERE Category = @category
            ORDER BY Name
            """;

        var results = new Dictionary<string, string?>();
        var connection = await _dbContext.GetConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@category", category);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader.GetString(0);
            var isEncrypted = reader.GetBoolean(3);
            
            string? value;
            if (isEncrypted)
            {
                var encryptedBytes = (byte[])reader[2];
                value = EncryptionService.Decrypt(encryptedBytes);
            }
            else
            {
                value = reader.IsDBNull(1) ? null : reader.GetString(1);
            }
            
            results[name] = value;
        }

        return results;
    }

    /// <summary>
    /// Initializes default settings if they don't exist
    /// </summary>
    public async Task InitializeDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var defaults = new List<(string name, string? value, bool isEncrypted, string description, string category)>
        {
            // LLM Provider Settings (encrypted)
            ("OpenAI.ApiKey", null, true, "OpenAI API Key for embeddings and chat", "LLM"),
            ("Anthropic.ApiKey", null, true, "Anthropic API Key for Claude models", "LLM"),
            ("Gemini.ApiKey", null, true, "Google Gemini API Key", "LLM"),
            
            // Model Configuration
            ("OpenAI.Model", "gpt-4o", false, "Default OpenAI chat model", "LLM"),
            ("Anthropic.Model", "claude-sonnet-4-20250514", false, "Default Anthropic model", "LLM"),
            ("Gemini.Model", "gemini-2.5-flash", false, "Default Gemini model", "LLM"),
            ("Ollama.Model", "gemma3:12b", false, "Default Ollama model", "LLM"),
            ("Ollama.BaseUrl", "http://localhost:11434", false, "Ollama server URL", "LLM"),
            
            // Embedding Configuration  
            ("Embedding.Model", "text-embedding-ada-002", false, "Text embedding model", "Embedding"),
            ("Embedding.VectorSize", "1536", false, "Vector embedding dimensions", "Embedding"),
            
            // Chat Configuration
            ("Chat.DefaultTemperature", "0.7", false, "Default temperature for chat responses", "Chat"),
            ("Chat.MaxTurns", "10", false, "Maximum conversation turns", "Chat"),
            ("Chat.MaxTokens", "4000", false, "Maximum tokens per response", "Chat"),
            
            // Document Processing
            ("Documents.ChunkSize", "4096", false, "Character limit per document chunk", "Documents"),
            ("Documents.ChunkOverlap", "40", false, "Character overlap between chunks", "Documents"),
            ("Documents.MaxFileSize", "52428800", false, "Maximum upload file size in bytes", "Documents"),
            
            // Vector Store Configuration
            ("VectorStore.Provider", "Qdrant", false, "Vector database provider", "VectorStore"),
            ("VectorStore.Qdrant.Host", "localhost", false, "Qdrant server hostname", "VectorStore"),
            ("VectorStore.Qdrant.Port", "6334", false, "Qdrant gRPC port", "VectorStore"),
        };

        foreach (var (name, value, isEncrypted, description, category) in defaults)
        {
            // Only set if doesn't exist
            var existing = await GetValueAsync(name, cancellationToken);
            if (existing == null)
            {
                await SetValueAsync(name, value, isEncrypted, description, category, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Removes a setting
    /// </summary>
    public async Task DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM AppSettings WHERE Name = @name";
        
        var connection = await _dbContext.GetConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@name", name);
        
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}