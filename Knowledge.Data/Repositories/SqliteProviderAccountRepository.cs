using Knowledge.Data.Interfaces;
using Knowledge.Entities;
using Microsoft.Data.Sqlite;

namespace Knowledge.Data.Repositories;

public class SqliteProviderAccountRepository : IProviderAccountRepository
{
    private readonly SqliteDbContext _dbContext;

    public SqliteProviderAccountRepository(SqliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<ProviderAccountRecord>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        const string sql = "SELECT * FROM ProviderAccounts ORDER BY Provider";

        using var command = new SqliteCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<ProviderAccountRecord>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapFromReader(reader));
        }

        return results;
    }

    public async Task<ProviderAccountRecord?> GetAccountByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        const string sql = "SELECT * FROM ProviderAccounts WHERE Provider = @Provider";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.Add(new SqliteParameter("@Provider", provider));

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapFromReader(reader);
        }

        return null;
    }

    public async Task CreateOrUpdateAccountAsync(ProviderAccountRecord account, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        
        var existing = await GetAccountByProviderAsync(account.Provider, cancellationToken);
        
        if (existing != null)
        {
            // Update existing record
            const string updateSql = """
                UPDATE ProviderAccounts 
                SET IsConnected = @IsConnected, ApiKeyConfigured = @ApiKeyConfigured, LastSyncAt = @LastSyncAt,
                    Balance = @Balance, BalanceUnit = @BalanceUnit, MonthlyUsage = @MonthlyUsage,
                    ErrorMessage = @ErrorMessage, UpdatedAt = @UpdatedAt
                WHERE Provider = @Provider
                """;

            using var updateCommand = new SqliteCommand(updateSql, connection);
            updateCommand.Parameters.AddRange([
                new SqliteParameter("@Provider", account.Provider),
                new SqliteParameter("@IsConnected", account.IsConnected),
                new SqliteParameter("@ApiKeyConfigured", account.ApiKeyConfigured),
                new SqliteParameter("@LastSyncAt", (object?)account.LastSyncAt ?? DBNull.Value),
                new SqliteParameter("@Balance", (object?)account.Balance ?? DBNull.Value),
                new SqliteParameter("@BalanceUnit", (object?)account.BalanceUnit ?? DBNull.Value),
                new SqliteParameter("@MonthlyUsage", account.MonthlyUsage),
                new SqliteParameter("@ErrorMessage", (object?)account.ErrorMessage ?? DBNull.Value),
                new SqliteParameter("@UpdatedAt", DateTime.UtcNow)
            ]);

            await updateCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            // Create new record
            const string insertSql = """
                INSERT INTO ProviderAccounts (Provider, IsConnected, ApiKeyConfigured, LastSyncAt, Balance, 
                                            BalanceUnit, MonthlyUsage, ErrorMessage, CreatedAt, UpdatedAt)
                VALUES (@Provider, @IsConnected, @ApiKeyConfigured, @LastSyncAt, @Balance,
                        @BalanceUnit, @MonthlyUsage, @ErrorMessage, @CreatedAt, @UpdatedAt)
                """;

            using var insertCommand = new SqliteCommand(insertSql, connection);
            insertCommand.Parameters.AddRange([
                new SqliteParameter("@Provider", account.Provider),
                new SqliteParameter("@IsConnected", account.IsConnected),
                new SqliteParameter("@ApiKeyConfigured", account.ApiKeyConfigured),
                new SqliteParameter("@LastSyncAt", (object?)account.LastSyncAt ?? DBNull.Value),
                new SqliteParameter("@Balance", (object?)account.Balance ?? DBNull.Value),
                new SqliteParameter("@BalanceUnit", (object?)account.BalanceUnit ?? DBNull.Value),
                new SqliteParameter("@MonthlyUsage", account.MonthlyUsage),
                new SqliteParameter("@ErrorMessage", (object?)account.ErrorMessage ?? DBNull.Value),
                new SqliteParameter("@CreatedAt", account.CreatedAt),
                new SqliteParameter("@UpdatedAt", account.UpdatedAt)
            ]);

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task UpdateConnectionStatusAsync(string provider, bool isConnected, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        const string sql = """
            UPDATE ProviderAccounts 
            SET IsConnected = @IsConnected, ErrorMessage = @ErrorMessage, UpdatedAt = @UpdatedAt
            WHERE Provider = @Provider
            """;

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddRange([
            new SqliteParameter("@Provider", provider),
            new SqliteParameter("@IsConnected", isConnected),
            new SqliteParameter("@ErrorMessage", (object?)errorMessage ?? DBNull.Value),
            new SqliteParameter("@UpdatedAt", DateTime.UtcNow)
        ]);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateBalanceAsync(string provider, decimal? balance, string? balanceUnit = null, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        const string sql = """
            UPDATE ProviderAccounts 
            SET Balance = @Balance, BalanceUnit = @BalanceUnit, UpdatedAt = @UpdatedAt
            WHERE Provider = @Provider
            """;

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddRange([
            new SqliteParameter("@Provider", provider),
            new SqliteParameter("@Balance", (object?)balance ?? DBNull.Value),
            new SqliteParameter("@BalanceUnit", (object?)balanceUnit ?? DBNull.Value),
            new SqliteParameter("@UpdatedAt", DateTime.UtcNow)
        ]);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateLastSyncAsync(string provider, DateTime syncTime, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        const string sql = """
            UPDATE ProviderAccounts 
            SET LastSyncAt = @LastSyncAt, UpdatedAt = @UpdatedAt
            WHERE Provider = @Provider
            """;

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddRange([
            new SqliteParameter("@Provider", provider),
            new SqliteParameter("@LastSyncAt", syncTime),
            new SqliteParameter("@UpdatedAt", DateTime.UtcNow)
        ]);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetConnectedProvidersAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _dbContext.CreateConnectionAsync();
        const string sql = "SELECT Provider FROM ProviderAccounts WHERE IsConnected = 1";

        using var command = new SqliteCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetString(0));
        }

        return results;
    }

    private static ProviderAccountRecord MapFromReader(SqliteDataReader reader)
    {
        return new ProviderAccountRecord
        {
            Id = reader.GetInt32(0),
            Provider = reader.GetString(1),
            IsConnected = reader.GetBoolean(2),
            ApiKeyConfigured = reader.GetBoolean(3),
            LastSyncAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            Balance = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
            BalanceUnit = reader.IsDBNull(6) ? null : reader.GetString(6),
            MonthlyUsage = reader.GetDecimal(7),
            ErrorMessage = reader.IsDBNull(8) ? null : reader.GetString(8),
            CreatedAt = reader.GetDateTime(9),
            UpdatedAt = reader.GetDateTime(10)
        };
    }
}