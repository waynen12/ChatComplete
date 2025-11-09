using Microsoft.Data.Sqlite;

namespace Knowledge.Data;

/// <summary>
/// Interface for SQLite database context operations
/// </summary>
public interface ISqliteDbContext : IDisposable
{
    /// <summary>
    /// Gets an open SQLite connection, creating and initializing the database if needed
    /// </summary>
    Task<SqliteConnection> GetConnectionAsync();
    
    /// <summary>
    /// Initializes the database schema if it doesn't exist
    /// </summary>
    Task InitializeDatabaseAsync();
}