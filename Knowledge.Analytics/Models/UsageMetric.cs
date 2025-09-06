using Knowledge.Contracts.Types;

namespace Knowledge.Analytics.Models;

public record UsageMetric
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string? ConversationId { get; init; }
    public string? KnowledgeId { get; init; }
    public AiProvider Provider { get; init; }
    public string? ModelName { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int TotalTokens => InputTokens + OutputTokens;
    public double Temperature { get; init; }
    public bool UsedAgentCapabilities { get; init; }
    public int ToolExecutions { get; init; }
    public TimeSpan ResponseTime { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? ClientId { get; init; }
    public bool WasSuccessful { get; init; } = true;
    public string? ErrorMessage { get; init; }
}

public record ModelUsageStats
{
    public string ModelName { get; init; } = string.Empty;
    public AiProvider Provider { get; init; }
    public int ConversationCount { get; init; }
    public int TotalTokens { get; init; }
    public int AverageTokensPerRequest { get; init; }
    public TimeSpan AverageResponseTime { get; init; }
    public DateTime LastUsed { get; init; }
    public bool? SupportsTools { get; init; }
    public int SuccessfulRequests { get; init; }
    public int FailedRequests { get; init; }
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests : 0;
    public int TotalRequests => SuccessfulRequests + FailedRequests;
}

public record KnowledgeUsageStats  
{
    public string KnowledgeId { get; init; } = string.Empty;
    public string KnowledgeName { get; init; } = string.Empty;
    public int DocumentCount { get; init; }
    public int ChunkCount { get; init; }
    public int ConversationCount { get; init; }
    public int QueryCount { get; init; }
    public DateTime LastQueried { get; init; }
    public DateTime CreatedAt { get; init; }
    public string VectorStore { get; init; } = string.Empty;
    public long TotalFileSize { get; init; }
}