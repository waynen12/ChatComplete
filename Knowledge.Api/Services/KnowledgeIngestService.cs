// Knowledge.Api/Services/KnowledgeIngestService.cs
using KnowledgeEngine;
using KnowledgeEngine.Logging;
namespace Knowledge.Api.Services;

/// <summary>
/// A service for ingesting knowledge from files.
/// </summary>
public sealed class KnowledgeIngestService : IKnowledgeIngestService
{
    private readonly KnowledgeManager _manager; // or the helper owning SaveToMemoryAsync
    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeIngestService"/> class.
    /// </summary>
    /// <param name="manager">The knowledge manager.</param>
    public KnowledgeIngestService(KnowledgeManager manager)
    {
        _manager = manager;
    }

    /// <summary>
    /// Imports a file into the knowledge base.
    /// </summary>
    /// <param name="file">The file to import.</param>
    /// <param name="collection">The collection to import the file into.</param>
    /// <param name="ct">The cancellation token.</param>
    public async Task ImportAsync(IFormFile file, string collection, CancellationToken ct)
    {
        if (FileValidation.Check(file) is { } err)
        {
            throw new InvalidOperationException(err);
        }

        // persist to temp file (IFormFile → stream)
        var tmpPath = Path.Combine(Path.GetTempPath(), file.FileName);
        await using (var fs = File.Create(tmpPath))
            await file.CopyToAsync(fs, ct);

        await _manager.SaveToMemoryAsync(tmpPath, collection);

        File.Delete(tmpPath); // clean-up
    }
}
