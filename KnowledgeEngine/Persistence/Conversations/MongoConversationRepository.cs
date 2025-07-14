using MongoDB.Driver;

namespace KnowledgeEngine.Persistence.Conversations;

public sealed class MongoConversationRepository : IConversationRepository
{
    private readonly IMongoCollection<Conversation> _col;

    public MongoConversationRepository(IMongoDatabase db)
    {
        _col = db.GetCollection<Conversation>("conversations");

        // First-run index to speed up look-ups by knowledgeId (optional)
        _col.Indexes.CreateOne(
            new CreateIndexModel<Conversation>(
                Builders<Conversation>.IndexKeys.Ascending(c => c.KnowledgeId)));
    }

    public async Task<string> CreateAsync(string? knowledgeId, CancellationToken ct = default)
    {
        var convo = new Conversation { KnowledgeId = knowledgeId ?? string.Empty };
        await _col.InsertOneAsync(convo, cancellationToken: ct);
        return convo.Id;
    }

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string conversationId, CancellationToken ct = default)
    {
        var convo = await _col.Find(c => c.Id == conversationId).FirstOrDefaultAsync(ct);
        if (convo?.Messages != null)
        {
            return (IReadOnlyList<ChatMessage>)convo.Messages ?? [];
        }
        else
        {
            return new List<ChatMessage>();
        }
    }

    public async Task AppendMessageAsync(string conversationId, ChatMessage message, CancellationToken ct = default)
    {
        var update = Builders<Conversation>.Update.Push(c => c.Messages, message);
        await _col.UpdateOneAsync(c => c.Id == conversationId, update, cancellationToken: ct);
    }
}