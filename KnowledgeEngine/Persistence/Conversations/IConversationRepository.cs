namespace KnowledgeEngine.Persistence.Conversations;

public interface IConversationRepository
{
    Task<string> CreateAsync(string? knowledgeId, CancellationToken ct = default);
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string conversationId, CancellationToken ct = default);
    Task AppendMessageAsync(string conversationId, ChatMessage message, CancellationToken ct = default);
}