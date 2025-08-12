using System.Data;
using Microsoft.Data.Sqlite;

namespace KnowledgeEngine.Persistence.Sqlite;

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

    private async Task ExecuteNonQueryAsync(string sql)
    {
        if (_connection == null) return;

        using var command = new SqliteCommand(sql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}