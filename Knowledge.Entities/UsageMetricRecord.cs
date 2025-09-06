namespace Knowledge.Entities;

/// <summary>
/// Database record for usage metrics tracking
/// </summary>
public class UsageMetricRecord
{
    public string Id { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? KnowledgeId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public double Temperature { get; set; }
    public bool UsedAgentCapabilities { get; set; }
    public int ToolExecutions { get; set; }
    public double ResponseTimeMs { get; set; }
    public DateTime Timestamp { get; set; }
    public string? ClientId { get; set; }
    public bool WasSuccessful { get; set; } = true;
    public string? ErrorMessage { get; set; }
}