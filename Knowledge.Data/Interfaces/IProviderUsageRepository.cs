using Knowledge.Entities;

namespace Knowledge.Data.Interfaces;

public interface IProviderUsageRepository
{
    Task<IEnumerable<ProviderUsageRecord>> GetUsageByProviderAsync(string provider, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProviderUsageRecord>> GetUsageByModelAsync(string modelName, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProviderUsageRecord>> GetUsageByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<ProviderUsageRecord?> GetDailyUsageAsync(string provider, string? modelName, DateTime date, CancellationToken cancellationToken = default);
    Task CreateOrUpdateDailyUsageAsync(ProviderUsageRecord usage, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalCostByProviderAsync(string provider, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<(string Provider, decimal TotalCost)>> GetCostBreakdownAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
}