using System.ComponentModel.DataAnnotations;

namespace Knowledge.Contracts;

/// <summary>
/// Represents the response when a knowledge collection is successfully created.
/// </summary>
public class KnowledgeCreatedDto
{
    /// <summary>
    /// The unique identifier of the created knowledge collection.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Optional message about the creation process.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Number of files processed in this collection.
    /// </summary>
    public int? FilesProcessed { get; set; }
}