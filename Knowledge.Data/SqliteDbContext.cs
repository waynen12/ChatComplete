using System.Data;
using Microsoft.Data.Sqlite;

namespace Knowledge.Data;

/// <summary>
/// SQLite database context for local configuration and metadata storage
/// Provides connection management and schema initialization
/// </summary>
public class SqliteDbContext : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public SqliteDbContext(string databasePath)
    {
        // Ensure the directory exists before creating the connection string
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        _connectionString = $"Data Source={databasePath}";
    }

    /// <summary>
    /// Gets an open SQLite connection, creating and initializing the database if needed
    /// </summary>
    public async Task<SqliteConnection> GetConnectionAsync()
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync();
            await EnsureDatabaseInitializedAsync();
        }

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync();
        }

        return _connection;
    }

    /// <summary>
    /// Creates a new SQLite connection for concurrent operations
    /// Use this for operations that might run in parallel to avoid connection sharing issues
    /// </summary>
    public async Task<SqliteConnection> CreateConnectionAsync()
    {
        // Ensure database is initialized with the shared connection first
        await GetConnectionAsync();
        
        // Create a new connection for this operation
        var newConnection = new SqliteConnection(_connectionString);
        await newConnection.OpenAsync();
        return newConnection;
    }

    /// <summary>
    /// Creates database schema if it doesn't exist
    /// </summary>
    private async Task EnsureDatabaseInitializedAsync()
    {
        if (_connection == null) return;

        // Enable WAL mode for better concurrency
        await ExecuteNonQueryAsync("PRAGMA journal_mode=WAL;");
        await ExecuteNonQueryAsync("PRAGMA foreign_keys=ON;");

        await CreateAppSettingsTableAsync();
        await CreateChatHistoryTablesAsync();
        await CreateKnowledgeMetadataTablesAsync();
        await CreateOllamaModelTablesAsync();
        await CreateUsageMetricsTablesAsync();
        await CreateProviderTrackingTablesAsync();
        await MigrateDatabaseAsync();
    }

    private async Task CreateAppSettingsTableAsync()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS AppSettings (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name VARCHAR(255) NOT NULL UNIQUE,
                Description VARCHAR(500),
                Value TEXT,
                EncryptedValue BLOB,
                IsEncrypted BOOLEAN DEFAULT 0,
                Category VARCHAR(100) DEFAULT 'General',
                DataType VARCHAR(50) DEFAULT 'String',
                IsRequired BOOLEAN DEFAULT 0,
                DefaultValue TEXT,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX IF NOT EXISTS idx_appsettings_name ON AppSettings(Name);
            CREATE INDEX IF NOT EXISTS idx_appsettings_category ON AppSettings(Category);
            """;

        await ExecuteNonQueryAsync(sql);
    }

    private async Task CreateChatHistoryTablesAsync()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS Conversations (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ConversationId VARCHAR(36) NOT NULL UNIQUE,
                ClientId VARCHAR(255),
                Title VARCHAR(500),
                KnowledgeId VARCHAR(255),
                Provider VARCHAR(50),
                ModelName VARCHAR(100),
                Temperature REAL,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                IsArchived BOOLEAN DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Messages (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ConversationId VARCHAR(36) NOT NULL,
                Role VARCHAR(20) NOT NULL,
                Content TEXT NOT NULL,
                TokenCount INTEGER,
                Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                MessageIndex INTEGER NOT NULL,
                FOREIGN KEY (ConversationId) REFERENCES Conversations(ConversationId) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_conversations_id ON Conversations(ConversationId);
            CREATE INDEX IF NOT EXISTS idx_conversations_client ON Conversations(ClientId);
            CREATE INDEX IF NOT EXISTS idx_messages_conversation ON Messages(ConversationId);
            CREATE INDEX IF NOT EXISTS idx_messages_timestamp ON Messages(Timestamp);
            """;

        await ExecuteNonQueryAsync(sql);
    }

    private async Task CreateKnowledgeMetadataTablesAsync()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS KnowledgeCollections (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CollectionId VARCHAR(255) NOT NULL UNIQUE,
                Name VARCHAR(500) NOT NULL,
                Description TEXT,
                DocumentCount INTEGER DEFAULT 0,
                ChunkCount INTEGER DEFAULT 0,
                TotalTokens INTEGER DEFAULT 0,
                EmbeddingModel VARCHAR(100),
                VectorStore VARCHAR(50) DEFAULT 'Qdrant',
                Status VARCHAR(50) DEFAULT 'Active',
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS KnowledgeDocuments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CollectionId VARCHAR(255) NOT NULL,
                DocumentId VARCHAR(36) NOT NULL,
                OriginalFileName VARCHAR(500),
                FileSize INTEGER,
                FileType VARCHAR(50),
                ChunkCount INTEGER DEFAULT 0,
                ProcessingStatus VARCHAR(50) DEFAULT 'Pending',
                ErrorMessage TEXT,
                UploadedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                ProcessedAt DATETIME,
                FOREIGN KEY (CollectionId) REFERENCES KnowledgeCollections(CollectionId) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS KnowledgeChunks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CollectionId VARCHAR(255) NOT NULL,
                DocumentId VARCHAR(36) NOT NULL,
                ChunkId VARCHAR(36) NOT NULL UNIQUE,
                ChunkText TEXT NOT NULL,
                ChunkOrder INTEGER NOT NULL,
                TokenCount INTEGER,
                CharacterCount INTEGER,
                VectorStored BOOLEAN DEFAULT 0,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (CollectionId) REFERENCES KnowledgeCollections(CollectionId) ON DELETE CASCADE,
                FOREIGN KEY (DocumentId) REFERENCES KnowledgeDocuments(DocumentId) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_collections_id ON KnowledgeCollections(CollectionId);
            CREATE INDEX IF NOT EXISTS idx_documents_collection ON KnowledgeDocuments(CollectionId);
            CREATE INDEX IF NOT EXISTS idx_chunks_collection ON KnowledgeChunks(CollectionId);
            CREATE INDEX IF NOT EXISTS idx_chunks_document ON KnowledgeChunks(DocumentId);
            """;

        await ExecuteNonQueryAsync(sql);
    }

    private async Task CreateOllamaModelTablesAsync()
    {
        // First, check if we need to migrate from old schema with foreign keys
        await DropForeignKeyConstraintsIfExistsAsync();
        
        const string sql = """
            CREATE TABLE IF NOT EXISTS OllamaModels (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name VARCHAR(255) NOT NULL UNIQUE,
                DisplayName VARCHAR(255),
                Size INTEGER DEFAULT 0,
                Family VARCHAR(100),
                ParameterSize VARCHAR(50),
                QuantizationLevel VARCHAR(50),
                Format VARCHAR(50),
                Template TEXT,
                Parameters TEXT,
                ModifiedAt DATETIME,
                InstalledAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                LastUsedAt DATETIME,
                IsAvailable BOOLEAN DEFAULT 1,
                Status VARCHAR(50) DEFAULT 'Ready',
                SupportsTools BIT DEFAULT NULL,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS OllamaDownloads (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ModelName VARCHAR(255) NOT NULL UNIQUE,
                Status VARCHAR(50) NOT NULL DEFAULT 'Pending',
                BytesDownloaded INTEGER DEFAULT 0,
                TotalBytes INTEGER DEFAULT 0,
                PercentComplete REAL DEFAULT 0,
                ErrorMessage TEXT,
                StartedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                CompletedAt DATETIME,
                UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS OllamaModelCache (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ModelName VARCHAR(255) NOT NULL,
                SearchTerm VARCHAR(255) NOT NULL,
                ResultData TEXT,
                CachedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                ExpiresAt DATETIME,
                HitCount INTEGER DEFAULT 1,
                UNIQUE(ModelName, SearchTerm)
            );

            CREATE INDEX IF NOT EXISTS idx_ollama_models_name ON OllamaModels(Name);
            CREATE INDEX IF NOT EXISTS idx_ollama_models_family ON OllamaModels(Family);
            CREATE INDEX IF NOT EXISTS idx_ollama_models_status ON OllamaModels(Status);
            CREATE INDEX IF NOT EXISTS idx_ollama_downloads_model ON OllamaDownloads(ModelName);
            CREATE INDEX IF NOT EXISTS idx_ollama_downloads_status ON OllamaDownloads(Status);
            CREATE INDEX IF NOT EXISTS idx_ollama_cache_search ON OllamaModelCache(SearchTerm);
            CREATE INDEX IF NOT EXISTS idx_ollama_cache_expires ON OllamaModelCache(ExpiresAt);
            """;

        await ExecuteNonQueryAsync(sql);
    }

    private async Task CreateUsageMetricsTablesAsync()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS UsageMetrics (
                Id VARCHAR(36) PRIMARY KEY,
                ConversationId VARCHAR(36),
                KnowledgeId VARCHAR(255),
                Provider VARCHAR(50) NOT NULL,
                ModelName VARCHAR(100),
                InputTokens INTEGER DEFAULT 0,
                OutputTokens INTEGER DEFAULT 0,
                Temperature REAL DEFAULT 0.7,
                UsedAgentCapabilities BOOLEAN DEFAULT 0,
                ToolExecutions INTEGER DEFAULT 0,
                ResponseTimeMs REAL DEFAULT 0,
                Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                ClientId VARCHAR(255),
                WasSuccessful BOOLEAN DEFAULT 1,
                ErrorMessage TEXT,
                FOREIGN KEY (ConversationId) REFERENCES Conversations(ConversationId) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_usage_metrics_timestamp ON UsageMetrics(Timestamp);
            CREATE INDEX IF NOT EXISTS idx_usage_metrics_conversation ON UsageMetrics(ConversationId);
            CREATE INDEX IF NOT EXISTS idx_usage_metrics_knowledge ON UsageMetrics(KnowledgeId);
            CREATE INDEX IF NOT EXISTS idx_usage_metrics_provider ON UsageMetrics(Provider);
            CREATE INDEX IF NOT EXISTS idx_usage_metrics_model ON UsageMetrics(ModelName);
            """;

        await ExecuteNonQueryAsync(sql);
    }

    private async Task CreateProviderTrackingTablesAsync()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS ProviderUsage (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Provider VARCHAR(50) NOT NULL,
                ModelName VARCHAR(100),
                UsageDate DATE NOT NULL,
                TokensUsed INTEGER DEFAULT 0,
                CostUSD DECIMAL(10,4) DEFAULT 0,
                RequestCount INTEGER DEFAULT 0,
                SuccessRate DECIMAL(5,2) DEFAULT 100.00,
                AvgResponseTimeMs DECIMAL(8,2),
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS ProviderAccounts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Provider VARCHAR(50) NOT NULL UNIQUE,
                IsConnected BOOLEAN DEFAULT 0,
                ApiKeyConfigured BOOLEAN DEFAULT 0,
                LastSyncAt DATETIME,
                Balance DECIMAL(10,4),
                BalanceUnit VARCHAR(20),
                MonthlyUsage DECIMAL(10,4) DEFAULT 0,
                ErrorMessage TEXT,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX IF NOT EXISTS idx_provider_usage_provider ON ProviderUsage(Provider);
            CREATE INDEX IF NOT EXISTS idx_provider_usage_date ON ProviderUsage(UsageDate);
            CREATE INDEX IF NOT EXISTS idx_provider_usage_model ON ProviderUsage(ModelName);
            CREATE INDEX IF NOT EXISTS idx_provider_accounts_provider ON ProviderAccounts(Provider);
            """;

        await ExecuteNonQueryAsync(sql);
    }

    private async Task MigrateDatabaseAsync()
    {
        // Add SupportsTools column to OllamaModels table if it doesn't exist
        try
        {
            const string checkColumnSql = """
                SELECT COUNT(*) FROM pragma_table_info('OllamaModels') 
                WHERE name = 'SupportsTools'
                """;
            
            using var checkCommand = new SqliteCommand(checkColumnSql, _connection);
            var columnExists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
            
            if (!columnExists)
            {
                const string addColumnSql = """
                    ALTER TABLE OllamaModels 
                    ADD COLUMN SupportsTools BIT DEFAULT NULL
                    """;
                
                await ExecuteNonQueryAsync(addColumnSql);
            }
        }
        catch (Exception)
        {
            // Column migration failed - this is acceptable as the table might not exist yet
        }
    }

    private async Task ExecuteNonQueryAsync(string sql)
    {
        if (_connection == null) return;

        using var command = new SqliteCommand(sql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropForeignKeyConstraintsIfExistsAsync()
    {
        try
        {
            // Check if the OllamaDownloads table exists and has foreign key constraints
            const string checkTableSql = """
                SELECT sql FROM sqlite_master 
                WHERE type='table' AND name='OllamaDownloads' AND sql LIKE '%FOREIGN KEY%'
                """;
            
            var connection = await GetConnectionAsync();
            using var command = connection.CreateCommand();
            command.CommandText = checkTableSql;
            var result = await command.ExecuteScalarAsync();
            
            if (result != null)
            {
                // Table exists with foreign key constraints, need to recreate it
                const string migrationSql = """
                    -- Create new table without foreign key constraints
                    CREATE TABLE OllamaDownloads_new (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ModelName VARCHAR(255) NOT NULL UNIQUE,
                        Status VARCHAR(50) NOT NULL DEFAULT 'Pending',
                        BytesDownloaded INTEGER DEFAULT 0,
                        TotalBytes INTEGER DEFAULT 0,
                        PercentComplete REAL DEFAULT 0,
                        ErrorMessage TEXT,
                        StartedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        CompletedAt DATETIME,
                        UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );
                    
                    -- Copy data from old table
                    INSERT INTO OllamaDownloads_new (ModelName, Status, BytesDownloaded, TotalBytes, PercentComplete, ErrorMessage, StartedAt, CompletedAt, UpdatedAt)
                    SELECT ModelName, Status, BytesDownloaded, TotalBytes, PercentComplete, ErrorMessage, StartedAt, CompletedAt, UpdatedAt
                    FROM OllamaDownloads;
                    
                    -- Drop old table and rename new one
                    DROP TABLE OllamaDownloads;
                    ALTER TABLE OllamaDownloads_new RENAME TO OllamaDownloads;
                    
                    -- Recreate indexes
                    CREATE INDEX IF NOT EXISTS idx_ollama_downloads_model ON OllamaDownloads(ModelName);
                    CREATE INDEX IF NOT EXISTS idx_ollama_downloads_status ON OllamaDownloads(Status);
                    """;
                
                await ExecuteNonQueryAsync(migrationSql);
                // Successfully migrated table
            }
        }
        catch (Exception)
        {
            // Failed to check/migrate foreign key constraints - continuing with table creation
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}