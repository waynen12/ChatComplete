using System.Text.Json.Serialization;

namespace Knowledge.Contracts;

/// <summary>
/// Represents the response from the server after initiating a chat session.
/// </summary>
public class ChatResponseDto
{
    /// <summary>
    /// A unique identifier for the established chat session, if applicable.
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// The assistant's reply to the user's message.
    /// </summary>
    public string Reply { get; set; } = string.Empty;
    

}
