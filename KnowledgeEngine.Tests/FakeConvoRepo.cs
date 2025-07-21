using KnowledgeEngine.Persistence.Conversations;

internal sealed class FakeConvoRepo : IConversationRepository
{
    private readonly List<ChatMessage> _store;

    public FakeConvoRepo(int messageCount)
    {
        _store = Enumerable.Range(0, messageCount)
            .Select(i => new ChatMessage
            {
                Role    = i % 2 == 0 ? "user" : "assistant",
                Content = $"msg {i}",
                Ts      = DateTime.UtcNow.AddMinutes(-i)
            }).ToList();
    }

    public Task<string> CreateAsync(string? k, CancellationToken ct = default)
        => Task.FromResult("cid-123");

    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string c, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ChatMessage>>(_store);

    public Task AppendMessageAsync(string c, ChatMessage m, CancellationToken ct = default)
    {
        _store.Add(m);
        return Task.CompletedTask;
    }
}