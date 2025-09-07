using Knowledge.Analytics.Models;

namespace Knowledge.Analytics.Services;

public interface IProviderApiService
{
    Task<ProviderApiAccountInfo?> GetAccountInfoAsync(CancellationToken cancellationToken = default);
    Task<ProviderApiUsageInfo?> GetUsageInfoAsync(int days = 30, CancellationToken cancellationToken = default);
    bool IsConfigured { get; }
    string ProviderName { get; }
}

public record ProviderApiAccountInfo
{
    public string ProviderName { get; init; } = string.Empty;
    public string? AccountId { get; init; }
    public decimal? Balance { get; init; }
    public string? Currency { get; init; }
    public decimal? CreditUsed { get; init; }
    public decimal? CreditLimit { get; init; }
    public string? PlanType { get; init; }
    public Dictionary<string, object> AdditionalInfo { get; init; } = new();
}

public record ProviderApiUsageInfo
{
    public string ProviderName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal TotalCost { get; init; }
    public string Currency { get; init; } = "USD";
    public int TotalRequests { get; init; }
    public int TotalTokens { get; init; }
    public List<ModelUsageInfo> ModelBreakdown { get; init; } = new();
}

public record ModelUsageInfo
{
    public string ModelName { get; init; } = string.Empty;
    public int Requests { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public decimal Cost { get; init; }
}