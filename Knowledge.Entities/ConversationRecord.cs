namespace Knowledge.Entities;

/// <summary>
/// Database record for chat conversations
/// </summary>
public class ConversationRecord
{
    public string ConversationId { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? Title { get; set; }
    public string? KnowledgeId { get; set; }
    public string? Provider { get; set; }
    public string? ModelName { get; set; }
    public double? Temperature { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsArchived { get; set; }
}

/// <summary>
/// Database record for chat messages
/// </summary>
public class MessageRecord
{
    public string ConversationId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? TokenCount { get; set; }
    public DateTime Timestamp { get; set; }
    public int MessageIndex { get; set; }
}