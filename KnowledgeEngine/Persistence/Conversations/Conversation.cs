using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KnowledgeEngine.Persistence.Conversations;

public class Conversation
{
    [BsonId]                                  // MongoDB _id
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("startedUtc")]
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

    [BsonElement("knowledgeId")]
    public string KnowledgeId { get; set; } = string.Empty;

    [BsonElement("messages")]
    public List<ChatMessage> Messages { get; set; } = new();
}