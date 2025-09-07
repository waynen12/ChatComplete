using Knowledge.Data.Interfaces;
using Knowledge.Entities;
using Microsoft.Data.Sqlite;

namespace Knowledge.Data.Repositories;

public class SqliteProviderUsageRepository : IProviderUsageRepository
{
    private readonly SqliteDbContext _dbContext;

    public SqliteProviderUsageRepository(SqliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<ProviderUsageRecord>> GetUsageByProviderAsync(string provider, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.GetConnectionAsync();
        var sql = "SELECT * FROM ProviderUsage WHERE Provider = @Provider";
        var parameters = new List<SqliteParameter> { new("@Provider", provider) };

        if (startDate.HasValue)
        {
            sql += " AND UsageDate >= @StartDate";
            parameters.Add(new SqliteParameter("@StartDate", startDate.Value.Date));
        }

        if (endDate.HasValue)
        {
            sql += " AND UsageDate <= @EndDate";
            parameters.Add(new SqliteParameter("@EndDate", endDate.Value.Date));
        }

        sql += " ORDER BY UsageDate DESC";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<ProviderUsageRecord>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapFromReader(reader));
        }

        return results;
    }

    public async Task<IEnumerable<ProviderUsageRecord>> GetUsageByModelAsync(string modelName, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.GetConnectionAsync();
        var sql = "SELECT * FROM ProviderUsage WHERE ModelName = @ModelName";
        var parameters = new List<SqliteParameter> { new("@ModelName", modelName) };

        if (startDate.HasValue)
        {
            sql += " AND UsageDate >= @StartDate";
            parameters.Add(new SqliteParameter("@StartDate", startDate.Value.Date));
        }

        if (endDate.HasValue)
        {
            sql += " AND UsageDate <= @EndDate";
            parameters.Add(new SqliteParameter("@EndDate", endDate.Value.Date));
        }

        sql += " ORDER BY UsageDate DESC";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<ProviderUsageRecord>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapFromReader(reader));
        }

        return results;
    }

    public async Task<IEnumerable<ProviderUsageRecord>> GetUsageByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.GetConnectionAsync();
        const string sql = "SELECT * FROM ProviderUsage WHERE UsageDate >= @StartDate AND UsageDate <= @EndDate ORDER BY UsageDate DESC";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.Add(new SqliteParameter("@StartDate", startDate.Date));
        command.Parameters.Add(new SqliteParameter("@EndDate", endDate.Date));

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<ProviderUsageRecord>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapFromReader(reader));
        }

        return results;
    }

    public async Task<ProviderUsageRecord?> GetDailyUsageAsync(string provider, string? modelName, DateTime date, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.GetConnectionAsync();
        var sql = "SELECT * FROM ProviderUsage WHERE Provider = @Provider AND UsageDate = @Date";
        var parameters = new List<SqliteParameter>
        {
            new("@Provider", provider),
            new("@Date", date.Date)
        };

        if (!string.IsNullOrEmpty(modelName))
        {
            sql += " AND ModelName = @ModelName";
            parameters.Add(new SqliteParameter("@ModelName", modelName));
        }

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapFromReader(reader);
        }

        return null;
    }

    public async Task CreateOrUpdateDailyUsageAsync(ProviderUsageRecord usage, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.GetConnectionAsync();
        
        // Check if record exists
        var existing = await GetDailyUsageAsync(usage.Provider, usage.ModelName, usage.UsageDate, cancellationToken);
        
        if (existing != null)
        {
            // Update existing record
            const string updateSql = """
                UPDATE ProviderUsage 
                SET TokensUsed = @TokensUsed, CostUSD = @CostUSD, RequestCount = @RequestCount,
                    SuccessRate = @SuccessRate, AvgResponseTimeMs = @AvgResponseTimeMs, UpdatedAt = @UpdatedAt
                WHERE Id = @Id
                """;

            using var updateCommand = new SqliteCommand(updateSql, connection);
            updateCommand.Parameters.AddRange([
                new SqliteParameter("@Id", existing.Id),
                new SqliteParameter("@TokensUsed", usage.TokensUsed),
                new SqliteParameter("@CostUSD", usage.CostUSD),
                new SqliteParameter("@RequestCount", usage.RequestCount),
                new SqliteParameter("@SuccessRate", usage.SuccessRate),
                new SqliteParameter("@AvgResponseTimeMs", (object?)usage.AvgResponseTimeMs ?? DBNull.Value),
                new SqliteParameter("@UpdatedAt", DateTime.UtcNow)
            ]);

            await updateCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            // Create new record
            const string insertSql = """
                INSERT INTO ProviderUsage (Provider, ModelName, UsageDate, TokensUsed, CostUSD, RequestCount, 
                                         SuccessRate, AvgResponseTimeMs, CreatedAt, UpdatedAt)
                VALUES (@Provider, @ModelName, @UsageDate, @TokensUsed, @CostUSD, @RequestCount,
                        @SuccessRate, @AvgResponseTimeMs, @CreatedAt, @UpdatedAt)
                """;

            using var insertCommand = new SqliteCommand(insertSql, connection);
            insertCommand.Parameters.AddRange([
                new SqliteParameter("@Provider", usage.Provider),
                new SqliteParameter("@ModelName", (object?)usage.ModelName ?? DBNull.Value),
                new SqliteParameter("@UsageDate", usage.UsageDate.Date),
                new SqliteParameter("@TokensUsed", usage.TokensUsed),
                new SqliteParameter("@CostUSD", usage.CostUSD),
                new SqliteParameter("@RequestCount", usage.RequestCount),
                new SqliteParameter("@SuccessRate", usage.SuccessRate),
                new SqliteParameter("@AvgResponseTimeMs", (object?)usage.AvgResponseTimeMs ?? DBNull.Value),
                new SqliteParameter("@CreatedAt", usage.CreatedAt),
                new SqliteParameter("@UpdatedAt", usage.UpdatedAt)
            ]);

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<decimal> GetTotalCostByProviderAsync(string provider, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.GetConnectionAsync();
        var sql = "SELECT SUM(CostUSD) FROM ProviderUsage WHERE Provider = @Provider";
        var parameters = new List<SqliteParameter> { new("@Provider", provider) };

        if (startDate.HasValue)
        {
            sql += " AND UsageDate >= @StartDate";
            parameters.Add(new SqliteParameter("@StartDate", startDate.Value.Date));
        }

        if (endDate.HasValue)
        {
            sql += " AND UsageDate <= @EndDate";
            parameters.Add(new SqliteParameter("@EndDate", endDate.Value.Date));
        }

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == DBNull.Value ? 0m : Convert.ToDecimal(result);
    }

    public async Task<IEnumerable<(string Provider, decimal TotalCost)>> GetCostBreakdownAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var connection = await _dbContext.GetConnectionAsync();
        var sql = "SELECT Provider, SUM(CostUSD) as TotalCost FROM ProviderUsage WHERE 1=1";
        var parameters = new List<SqliteParameter>();

        if (startDate.HasValue)
        {
            sql += " AND UsageDate >= @StartDate";
            parameters.Add(new SqliteParameter("@StartDate", startDate.Value.Date));
        }

        if (endDate.HasValue)
        {
            sql += " AND UsageDate <= @EndDate";
            parameters.Add(new SqliteParameter("@EndDate", endDate.Value.Date));
        }

        sql += " GROUP BY Provider ORDER BY TotalCost DESC";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<(string Provider, decimal TotalCost)>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var provider = reader.GetString(0);
            var totalCost = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
            results.Add((provider, totalCost));
        }

        return results;
    }

    private static ProviderUsageRecord MapFromReader(SqliteDataReader reader)
    {
        return new ProviderUsageRecord
        {
            Id = reader.GetInt32(0),
            Provider = reader.GetString(1),
            ModelName = reader.IsDBNull(2) ? null : reader.GetString(2),
            UsageDate = reader.GetDateTime(3),
            TokensUsed = reader.GetInt32(4),
            CostUSD = reader.GetDecimal(5),
            RequestCount = reader.GetInt32(6),
            SuccessRate = reader.GetDecimal(7),
            AvgResponseTimeMs = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
            CreatedAt = reader.GetDateTime(9),
            UpdatedAt = reader.GetDateTime(10)
        };
    }
}