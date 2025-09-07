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
                    var apiAccountInfo = await provider.GetAccountInfoAsync(cancellationToken);
                    if (apiAccountInfo != null)
                    {
                        // Convert from API model to Analytics model
                        return new ProviderAccountInfo
                        {
                            Provider = apiAccountInfo.ProviderName,
                            IsConnected = provider.IsConfigured,
                            ApiKeyConfigured = provider.IsConfigured,
                            LastSyncAt = DateTime.UtcNow,
                            Balance = apiAccountInfo.Balance,
                            BalanceUnit = apiAccountInfo.Currency ?? "USD",
                            MonthlyUsage = apiAccountInfo.CreditUsed ?? 0m,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
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
                    var apiUsageInfo = await provider.GetUsageInfoAsync(days, cancellationToken);
                    if (apiUsageInfo != null)
                    {
                        // Convert from API model to Analytics model
                        return new ProviderUsageInfo
                        {
                            Provider = apiUsageInfo.ProviderName,
                            TotalCost = apiUsageInfo.TotalCost,
                            TotalRequests = apiUsageInfo.TotalRequests,
                            SuccessRate = 100.0, // Default success rate since API models don't track this
                            ModelBreakdown = apiUsageInfo.ModelBreakdown.ToDictionary(
                                m => m.ModelName,
                                m => new ModelUsageBreakdown
                                {
                                    ModelName = m.ModelName,
                                    Requests = m.Requests,
                                    TokensUsed = m.InputTokens + m.OutputTokens,
                                    Cost = m.Cost
                                }
                            ),
                            PeriodStart = apiUsageInfo.StartDate,
                            PeriodEnd = apiUsageInfo.EndDate
                        };
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
            ConnectedProviders = _providerServices.Count(p => p.IsConfigured),
            TotalMonthlyCost = usages.Sum(u => u.TotalCost),
            TotalRequests = usages.Sum(u => u.TotalRequests),
            AverageSuccessRate = usages.Any() ? usages.Average(u => u.SuccessRate) : 0,
            Providers = accounts.ToDictionary(
                a => a.Provider,
                a => new ProviderInfo
                {
                    Provider = a.Provider,
                    IsConnected = a.IsConnected,
                    MonthlyCost = usages.FirstOrDefault(u => u.Provider == a.Provider)?.TotalCost ?? 0,
                    RequestCount = usages.FirstOrDefault(u => u.Provider == a.Provider)?.TotalRequests ?? 0,
                    SuccessRate = usages.FirstOrDefault(u => u.Provider == a.Provider)?.SuccessRate ?? 0
                }
            ),
            ProviderBreakdown = usages.ToDictionary(
                u => u.Provider,
                u => new ProviderBreakdown
                {
                    Cost = u.TotalCost,
                    Requests = u.TotalRequests,
                    SuccessRate = u.SuccessRate,
                    IsConnected = accounts.Any(a => a.Provider == u.Provider && a.IsConnected)
                }
            ),
            LastUpdated = DateTime.UtcNow
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

