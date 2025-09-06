using Knowledge.Analytics.Models;
using Knowledge.Contracts.Types;

namespace Knowledge.Analytics.Services;

public interface IUsageTrackingService
{
    Task TrackUsageAsync(UsageMetric metric, CancellationToken cancellationToken = default);
    Task<IEnumerable<UsageMetric>> GetUsageHistoryAsync(int days = 30, CancellationToken cancellationToken = default);
    Task<IEnumerable<ModelUsageStats>> GetModelUsageStatsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<KnowledgeUsageStats>> GetKnowledgeUsageStatsAsync(CancellationToken cancellationToken = default);
    Task<UsageMetric?> GetUsageByConversationAsync(string conversationId, CancellationToken cancellationToken = default);
}