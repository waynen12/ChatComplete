using Knowledge.Analytics.Models;
using Microsoft.Extensions.Logging;

namespace Knowledge.Analytics.Services;

public class ProviderAggregationService
{
    private readonly IEnumerable<IProviderApiService> _providerServices;
    private readonly ILogger<ProviderAggregationService> _logger;

    public ProviderAggregationService(
        IEnumerable<IProviderApiService> providerServices,
        ILogger<ProviderAggregationService> logger)
    {
        _providerServices = providerServices;
        _logger = logger;
    }

    public async Task<List<ProviderAccountInfo>> GetAllAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ProviderAccountInfo>();
        
        var tasks = _providerServices
            .Where(p => p.IsConfigured)
            .Select(async provider =>
            {
                try
                {
                    var accountInfo = await provider.GetAccountInfoAsync(cancellationToken);
                    if (accountInfo != null)
                    {
                        return accountInfo;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get account info for provider {Provider}", provider.ProviderName);
                }
                return null;
            });

        var accountInfos = await Task.WhenAll(tasks);
        results.AddRange(accountInfos.Where(info => info != null)!);

        return results;
    }

    public async Task<List<ProviderUsageInfo>> GetAllUsageInfoAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var results = new List<ProviderUsageInfo>();
        
        var tasks = _providerServices
            .Where(p => p.IsConfigured)
            .Select(async provider =>
            {
                try
                {
                    var usageInfo = await provider.GetUsageInfoAsync(days, cancellationToken);
                    if (usageInfo != null)
                    {
                        return usageInfo;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get usage info for provider {Provider}", provider.ProviderName);
                }
                return null;
            });

        var usageInfos = await Task.WhenAll(tasks);
        results.AddRange(usageInfos.Where(info => info != null)!);

        return results;
    }

    public async Task<ProviderSummary> GetProviderSummaryAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var accountTasks = GetAllAccountInfoAsync(cancellationToken);
        var usageTasks = GetAllUsageInfoAsync(days, cancellationToken);

        await Task.WhenAll(accountTasks, usageTasks);

        var accounts = await accountTasks;
        var usages = await usageTasks;

        return new ProviderSummary
        {
            TotalProviders = _providerServices.Count(),
            ConfiguredProviders = _providerServices.Count(p => p.IsConfigured),
            TotalCost = usages.Sum(u => u.TotalCost),
            TotalRequests = usages.Sum(u => u.TotalRequests),
            TotalTokens = usages.Sum(u => u.TotalTokens),
            Currency = usages.FirstOrDefault()?.Currency ?? "USD",
            AccountSummaries = accounts.Select(a => new ProviderAccountSummary
            {
                ProviderName = a.ProviderName,
                IsConfigured = true,
                Balance = a.Balance,
                CreditUsed = a.CreditUsed,
                PlanType = a.PlanType
            }).ToList(),
            UsageSummaries = usages.Select(u => new ProviderUsageSummary
            {
                ProviderName = u.ProviderName,
                Cost = u.TotalCost,
                Requests = u.TotalRequests,
                Tokens = u.TotalTokens,
                TopModel = u.ModelBreakdown.OrderByDescending(m => m.Requests).FirstOrDefault()?.ModelName
            }).ToList()
        };
    }

    public List<string> GetConfiguredProviders()
    {
        return _providerServices.Where(p => p.IsConfigured).Select(p => p.ProviderName).ToList();
    }

    public List<string> GetUnconfiguredProviders()
    {
        return _providerServices.Where(p => !p.IsConfigured).Select(p => p.ProviderName).ToList();
    }
}

public record ProviderSummary
{
    public int TotalProviders { get; init; }
    public int ConfiguredProviders { get; init; }
    public decimal TotalCost { get; init; }
    public int TotalRequests { get; init; }
    public int TotalTokens { get; init; }
    public string Currency { get; init; } = "USD";
    public List<ProviderAccountSummary> AccountSummaries { get; init; } = new();
    public List<ProviderUsageSummary> UsageSummaries { get; init; } = new();
}

public record ProviderAccountSummary
{
    public string ProviderName { get; init; } = string.Empty;
    public bool IsConfigured { get; init; }
    public decimal? Balance { get; init; }
    public decimal? CreditUsed { get; init; }
    public string? PlanType { get; init; }
}

public record ProviderUsageSummary
{
    public string ProviderName { get; init; } = string.Empty;
    public decimal Cost { get; init; }
    public int Requests { get; init; }
    public int Tokens { get; init; }
    public string? TopModel { get; init; }
}