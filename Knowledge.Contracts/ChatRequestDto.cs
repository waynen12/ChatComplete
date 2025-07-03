namespace Knowledge.Contracts;

using System.ComponentModel.DataAnnotations;
/// <summary>
/// The contract for the Knowledge domain. This file contains all the data annotations and models used in this domain.
/// </summary>
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

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

    /// <summary>
    /// The temperature parameter for the chat model, which controls randomness in the generated responses.
    /// </summary>
    // Temperature (0-1).  –1 ⇒ use server-side default from appsettings.
    [Range(-1, 1)]
    [SwaggerSchema(
        Description = "Sampling temperature. 0 = deterministic, 1 = most creative. "
            + "Omit or set to -1 to use the server default."
    )]
    public double Temperature { get; set; } = -1;

    /// <summary>
    /// Whether to strip markdown from the response.
    /// </summary>
    [SwaggerSchema(Description = "Return plain-text reply instead of Markdown; default false")]
    public bool StripMarkdown { get; set; } = false;
    
    /// <summary>
    /// When set to true the AI chat will be configured with an extended system prompt
    /// </summary>
    public bool UseExtendedInstructions { get; set; } = false;
}
