namespace Knowledge.Entities;

public class ProviderUsageRecord
{
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public DateTime UsageDate { get; set; }
    public int TokensUsed { get; set; }
    public decimal CostUSD { get; set; }
    public int RequestCount { get; set; }
    public decimal SuccessRate { get; set; } = 100.00m;
    public decimal? AvgResponseTimeMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}