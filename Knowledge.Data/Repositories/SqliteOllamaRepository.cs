using Knowledge.Data.Interfaces;
using Knowledge.Entities;
using Microsoft.Data.Sqlite;

namespace Knowledge.Data.Repositories;

/// <summary>
/// SQLite repository for Ollama model management and caching
/// Supports user-choice model architecture with download tracking
/// </summary>
public class SqliteOllamaRepository : IOllamaRepository
{
    private readonly SqliteDbContext _dbContext;

    public SqliteOllamaRepository(SqliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets all installed Ollama models from local cache
    /// </summary>
    public async Task<List<OllamaModelRecord>> GetInstalledModelsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Name, DisplayName, Size, Family, ParameterSize, QuantizationLevel, 
                   Format, Template, Parameters, ModifiedAt, InstalledAt, LastUsedAt, 
                   IsAvailable, Status, SupportsTools
            FROM OllamaModels 
            WHERE IsAvailable = 1
            ORDER BY LastUsedAt DESC, InstalledAt DESC
            """;

        var models = new List<OllamaModelRecord>();
        using var connection = await _dbContext.CreateConnectionAsync();
        using var command = new SqliteCommand(sql, connection);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            models.Add(new OllamaModelRecord
            {
                Name = reader.GetString(0),
                DisplayName = reader.IsDBNull(1) ? null : reader.GetString(1),
                Size = reader.GetInt64(2),
                Family = reader.IsDBNull(3) ? null : reader.GetString(3),
                ParameterSize = reader.IsDBNull(4) ? null : reader.GetString(4),
                QuantizationLevel = reader.IsDBNull(5) ? null : reader.GetString(5),
                Format = reader.IsDBNull(6) ? null : reader.GetString(6),
                Template = reader.IsDBNull(7) ? null : reader.GetString(7),
                Parameters = reader.IsDBNull(8) ? null : reader.GetString(8),
                ModifiedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                InstalledAt = reader.GetDateTime(10),
                LastUsedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                IsAvailable = reader.GetBoolean(12),
                Status = reader.GetString(13),
                SupportsTools = reader.IsDBNull(14) ? null : reader.GetBoolean(14)
            });
        }

        return models;
    }

    /// <summary>
    /// Adds or updates a model record when it's downloaded/discovered
    /// </summary>
    public async Task UpsertModelAsync(OllamaModelRecord model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO OllamaModels 
            (Name, DisplayName, Size, Family, ParameterSize, QuantizationLevel, Format, 
             Template, Parameters, ModifiedAt, IsAvailable, Status, SupportsTools, UpdatedAt)
            VALUES (@name, @displayName, @size, @family, @parameterSize, @quantizationLevel, 
                    @format, @template, @parameters, @modifiedAt, @isAvailable, @status, @supportsTools, CURRENT_TIMESTAMP)
            ON CONFLICT(Name) DO UPDATE SET
                DisplayName = @displayName,
                Size = @size,
                Family = @family,
                ParameterSize = @parameterSize,
                QuantizationLevel = @quantizationLevel,
                Format = @format,
                Template = @template,
                Parameters = @parameters,
                ModifiedAt = @modifiedAt,
                IsAvailable = @isAvailable,
                Status = @status,
                SupportsTools = COALESCE(@supportsTools, SupportsTools),
                UpdatedAt = CURRENT_TIMESTAMP
            """;

        using var connection = await _dbContext.CreateConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        
        command.Parameters.AddWithValue("@name", model.Name);
        command.Parameters.AddWithValue("@displayName", model.DisplayName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@size", model.Size);
        command.Parameters.AddWithValue("@family", model.Family ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@parameterSize", model.ParameterSize ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@quantizationLevel", model.QuantizationLevel ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@format", model.Format ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@template", model.Template ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@parameters", model.Parameters ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@modifiedAt", model.ModifiedAt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@isAvailable", model.IsAvailable);
        command.Parameters.AddWithValue("@status", model.Status);
        command.Parameters.AddWithValue("@supportsTools", model.SupportsTools ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Marks a model as used (updates LastUsedAt timestamp)
    /// </summary>
    public async Task MarkModelUsedAsync(string modelName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE OllamaModels 
            SET LastUsedAt = CURRENT_TIMESTAMP, UpdatedAt = CURRENT_TIMESTAMP
            WHERE Name = @name
            """;

        using var connection = await _dbContext.CreateConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@name", modelName);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a specific model by name
    /// </summary>
    public async Task<OllamaModelRecord?> GetModelAsync(string modelName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Name, DisplayName, Size, Family, ParameterSize, QuantizationLevel, 
                   Format, Template, Parameters, ModifiedAt, InstalledAt, LastUsedAt, 
                   IsAvailable, Status, SupportsTools
            FROM OllamaModels 
            WHERE Name = @name
            """;

        using var connection = await _dbContext.CreateConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@name", modelName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new OllamaModelRecord
            {
                Name = reader.GetString(0),
                DisplayName = reader.IsDBNull(1) ? null : reader.GetString(1),
                Size = reader.GetInt64(2),
                Family = reader.IsDBNull(3) ? null : reader.GetString(3),
                ParameterSize = reader.IsDBNull(4) ? null : reader.GetString(4),
                QuantizationLevel = reader.IsDBNull(5) ? null : reader.GetString(5),
                Format = reader.IsDBNull(6) ? null : reader.GetString(6),
                Template = reader.IsDBNull(7) ? null : reader.GetString(7),
                Parameters = reader.IsDBNull(8) ? null : reader.GetString(8),
                ModifiedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                InstalledAt = reader.GetDateTime(10),
                LastUsedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                IsAvailable = reader.GetBoolean(12),
                Status = reader.GetString(13),
                SupportsTools = reader.IsDBNull(14) ? null : reader.GetBoolean(14)
            };
        }

        return null;
    }

    /// <summary>
    /// Updates the tool support status for a model
    /// </summary>
    public async Task UpdateSupportsToolsAsync(string modelName, bool supportsTools, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE OllamaModels 
            SET SupportsTools = @supportsTools, UpdatedAt = CURRENT_TIMESTAMP
            WHERE Name = @name
            """;

        using var connection = await _dbContext.CreateConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@name", modelName);
        command.Parameters.AddWithValue("@supportsTools", supportsTools);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Removes a model from local cache when deleted
    /// </summary>
    public async Task DeleteModelAsync(string modelName, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        
        // Start a transaction to ensure both deletions succeed or fail together
        using var transaction = connection.BeginTransaction();
        
        try
        {
            // Delete from OllamaModels
            const string deleteModelSql = "DELETE FROM OllamaModels WHERE Name = @name";
            using var deleteModelCommand = new SqliteCommand(deleteModelSql, connection, transaction);
            deleteModelCommand.Parameters.AddWithValue("@name", modelName);
            await deleteModelCommand.ExecuteNonQueryAsync(cancellationToken);

            // Also clean up any related download records
            const string deleteDownloadSql = "DELETE FROM OllamaDownloads WHERE ModelName = @name";
            using var deleteDownloadCommand = new SqliteCommand(deleteDownloadSql, connection, transaction);
            deleteDownloadCommand.Parameters.AddWithValue("@name", modelName);
            await deleteDownloadCommand.ExecuteNonQueryAsync(cancellationToken);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Tracks download progress for a model
    /// </summary>
    public async Task UpsertDownloadProgressAsync(OllamaDownloadRecord download, CancellationToken cancellationToken = default)
    {
        // First, try to update existing record
        const string updateSql = """
            UPDATE OllamaDownloads 
            SET Status = @status,
                BytesDownloaded = @bytesDownloaded,
                TotalBytes = @totalBytes,
                PercentComplete = @percentComplete,
                ErrorMessage = @errorMessage,
                UpdatedAt = CURRENT_TIMESTAMP,
                CompletedAt = CASE WHEN @status = 'Completed' THEN CURRENT_TIMESTAMP ELSE CompletedAt END
            WHERE ModelName = @modelName
            """;

        using var connection = await _dbContext.CreateConnectionAsync();
        using var updateCommand = new SqliteCommand(updateSql, connection);
        
        updateCommand.Parameters.AddWithValue("@modelName", download.ModelName);
        updateCommand.Parameters.AddWithValue("@status", download.Status);
        updateCommand.Parameters.AddWithValue("@bytesDownloaded", download.BytesDownloaded);
        updateCommand.Parameters.AddWithValue("@totalBytes", download.TotalBytes);
        updateCommand.Parameters.AddWithValue("@percentComplete", download.PercentComplete);
        updateCommand.Parameters.AddWithValue("@errorMessage", download.ErrorMessage ?? (object)DBNull.Value);

        var rowsAffected = await updateCommand.ExecuteNonQueryAsync(cancellationToken);

        // If no rows were updated, insert a new record
        if (rowsAffected == 0)
        {
            const string insertSql = """
                INSERT INTO OllamaDownloads 
                (ModelName, Status, BytesDownloaded, TotalBytes, PercentComplete, ErrorMessage, UpdatedAt)
                VALUES (@modelName, @status, @bytesDownloaded, @totalBytes, @percentComplete, @errorMessage, CURRENT_TIMESTAMP)
                """;

            using var insertCommand = new SqliteCommand(insertSql, connection);
            
            insertCommand.Parameters.AddWithValue("@modelName", download.ModelName);
            insertCommand.Parameters.AddWithValue("@status", download.Status);
            insertCommand.Parameters.AddWithValue("@bytesDownloaded", download.BytesDownloaded);
            insertCommand.Parameters.AddWithValue("@totalBytes", download.TotalBytes);
            insertCommand.Parameters.AddWithValue("@percentComplete", download.PercentComplete);
            insertCommand.Parameters.AddWithValue("@errorMessage", download.ErrorMessage ?? (object)DBNull.Value);

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Gets current download status for a model
    /// </summary>
    public async Task<OllamaDownloadRecord?> GetDownloadStatusAsync(string modelName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ModelName, Status, BytesDownloaded, TotalBytes, PercentComplete, 
                   ErrorMessage, StartedAt, CompletedAt
            FROM OllamaDownloads 
            WHERE ModelName = @modelName
            """;

        using var connection = await _dbContext.CreateConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@modelName", modelName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new OllamaDownloadRecord
            {
                ModelName = reader.GetString(0),
                Status = reader.GetString(1),
                BytesDownloaded = reader.GetInt64(2),
                TotalBytes = reader.GetInt64(3),
                PercentComplete = reader.GetDouble(4),
                ErrorMessage = reader.IsDBNull(5) ? null : reader.GetString(5),
                StartedAt = reader.GetDateTime(6),
                CompletedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            };
        }

        return null;
    }

    /// <summary>
    /// Gets all active downloads
    /// </summary>
    public async Task<List<OllamaDownloadRecord>> GetActiveDownloadsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ModelName, Status, BytesDownloaded, TotalBytes, PercentComplete, 
                   ErrorMessage, StartedAt, CompletedAt
            FROM OllamaDownloads 
            WHERE Status IN ('Pending', 'Downloading', 'In Progress')
            ORDER BY StartedAt DESC
            """;

        var downloads = new List<OllamaDownloadRecord>();
        using var connection = await _dbContext.CreateConnectionAsync();
        using var command = new SqliteCommand(sql, connection);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            downloads.Add(new OllamaDownloadRecord
            {
                ModelName = reader.GetString(0),
                Status = reader.GetString(1),
                BytesDownloaded = reader.GetInt64(2),
                TotalBytes = reader.GetInt64(3),
                PercentComplete = reader.GetDouble(4),
                ErrorMessage = reader.IsDBNull(5) ? null : reader.GetString(5),
                StartedAt = reader.GetDateTime(6),
                CompletedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            });
        }

        return downloads;
    }

    /// <summary>
    /// Cleans up old download records (keep last 30 days)
    /// </summary>
    public async Task<List<OllamaDownloadRecord>> GetDownloadHistoryAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ModelName, Status, BytesDownloaded, TotalBytes, PercentComplete, 
                   ErrorMessage, StartedAt, CompletedAt
            FROM OllamaDownloads 
            WHERE StartedAt >= @Since
            ORDER BY StartedAt DESC
            """;

        var downloads = new List<OllamaDownloadRecord>();
        using var connection = await _dbContext.CreateConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@Since", since);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            downloads.Add(new OllamaDownloadRecord
            {
                ModelName = reader.GetString(0),
                Status = reader.GetString(1),
                BytesDownloaded = reader.GetInt64(2),
                TotalBytes = reader.GetInt64(3),
                PercentComplete = reader.GetDouble(4),
                ErrorMessage = reader.IsDBNull(5) ? null : reader.GetString(5),
                StartedAt = reader.GetDateTime(6),
                CompletedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            });
        }

        return downloads;
    }

    public async Task CleanupOldDownloadsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM OllamaDownloads 
            WHERE Status IN ('Completed', 'Failed', 'Cancelled') 
              AND UpdatedAt < datetime('now', '-30 days')
            """;

        using var connection = await _dbContext.CreateConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}