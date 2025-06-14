namespace Knowledge.Contracts;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// The contract for the Knowledge domain. This file contains all the data annotations and models used in this domain.
/// </summary>
using Newtonsoft.Json;
/// <summary>
/// Represents a request to initiate a chat session.
/// </summary>
public class ChatRequestDto
{
    /// <summary>
    /// The unique identifier for the chat session, if applicable.
    /// </summary>
    [JsonIgnore]
    public string? Id { get; set; }

    /// <summary>
    /// The unique identifier of the knowledge collection to use for this chat session.
    /// </summary>
    public string? KnowledgeId { get; set; } = string.Empty;

    /// <summary>
    /// The user's message to be processed by the assistant.
    /// </summary>
     [Required]
    [MinLength(1)]
    public string Message { get; set; } = string.Empty;
}