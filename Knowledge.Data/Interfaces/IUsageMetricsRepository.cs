using Knowledge.Entities;

namespace Knowledge.Data.Interfaces;

/// <summary>
/// Repository interface for usage metrics tracking
/// </summary>
public interface IUsageMetricsRepository
{
    Task TrackUsageAsync(UsageMetricRecord metric, CancellationToken cancellationToken = default);
    Task<IEnumerable<UsageMetricRecord>> GetUsageHistoryAsync(int days = 30, CancellationToken cancellationToken = default);
    Task<UsageMetricRecord?> GetUsageByConversationAsync(string conversationId, CancellationToken cancellationToken = default);
}