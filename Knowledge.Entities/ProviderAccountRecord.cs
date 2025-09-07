namespace Knowledge.Entities;

public class ProviderAccountRecord
{
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public bool ApiKeyConfigured { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public decimal? Balance { get; set; }
    public string? BalanceUnit { get; set; } // USD, credits, etc.
    public decimal MonthlyUsage { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}