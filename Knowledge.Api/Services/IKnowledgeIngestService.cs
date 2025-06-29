// Knowledge.Api/Services/IKnowledgeIngestService.cs
using Microsoft.AspNetCore.Http;

namespace Knowledge.Api.Services;

/// <summary>
/// Defines the interface for ingesting knowledge data from files.
/// </summary>
public interface IKnowledgeIngestService
{
    /// <summary>
    /// Imports a file into the specified knowledge collection.
    /// </summary>
    /// <param name="file">The file to import.</param>
    /// <param name="collection">The name of the collection to import into.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ImportAsync(IFormFile file, string collection, CancellationToken ct = default);
}
