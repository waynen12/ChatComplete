using MongoDB.Bson.Serialization.Attributes;

namespace KnowledgeEngine.Persistence.Conversations;

public class ChatMessage
{
    [BsonElement("role")]
    public string Role { get; set; } = "";          // "user" | "assistant"

    [BsonElement("content")]
    public string Content { get; set; } = "";

    [BsonElement("ts")]
    public DateTime Ts { get; set; } = DateTime.UtcNow;
}