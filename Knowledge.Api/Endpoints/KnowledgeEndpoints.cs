using Knowledge.Api.Filters;
using Knowledge.Api.Services;
using Knowledge.Contracts;
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Persistence.IndexManagers;
using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace Knowledge.Api.Endpoints;

/// <summary>
/// Knowledge management endpoints for uploading, listing, and deleting knowledge collections.
/// </summary>
public static class KnowledgeEndpoints
{
    /// <summary>
    /// Maps knowledge-related endpoints to the application.
    /// </summary>
    public static RouteGroupBuilder MapKnowledgeEndpoints(this RouteGroupBuilder group)
    {
        // 1) GET /api/knowledge
        group.MapGet("/", async ([FromServices] IVectorStoreStrategy vectorStore, CancellationToken ct) =>
            {
                // Use vector store strategy to list collections directly
                var collections = await vectorStore.ListCollectionsAsync(ct);
                
                var summaries = collections.Select(name => new KnowledgeSummaryDto
                {
                    Id = name,
                    Name = name,
                    DocumentCount = 0 // Could be enhanced to get actual count
                }).OrderBy(x => x.Name);
                
                return Results.Ok(summaries);
            })
            .WithOpenApi()
            .Produces<IEnumerable<KnowledgeSummaryDto>>()
            .WithTags("Knowledge");

        // 2) POST /api/knowledge
        group.MapPost("/", async (
                [FromForm] string name,
                [FromForm] IFormFileCollection files,
                [FromServices] IKnowledgeIngestService ingest,
                CancellationToken ct
            ) =>
            {
                if (files.Count == 0)
                    return Results.BadRequest("No files uploaded.");

                foreach (var file in files)
                {
                    if (FileValidation.Check(file) is {} err)
                    {
                        return Results.BadRequest(err);
                    }
                }

                foreach (var f in files)
                {
                    await ingest.ImportAsync(f, name, ct);
                }

                return Results.Created($"/api/knowledge/{name}", new KnowledgeCreatedDto { Id = name, FilesProcessed = files.Count });
            })
            .Accepts<IFormFileCollection>("multipart/form-data")
            .Produces<KnowledgeCreatedDto>(201)
            .Produces<string>(400)
            .DisableAntiforgery();

        // 3) DELETE /api/knowledge/{id}
        group.MapDelete("/{id}", async (string id,
                [FromServices] IKnowledgeRepository repo,
                [FromServices] IIndexManager indexMgr,
                CancellationToken ct) =>
            {
                // 0. Fast-fail if the collection doesn't exist
                if (!await repo.ExistsAsync(id, ct))
                    return Results.NotFound($"Collection \"{id}\" not found.");

                // 1. Drop the collection (+ vector store rows)
                await repo.DeleteAsync(id, ct);

                // 2. Drop the search index (ignore 404 – index may not exist)
                await indexMgr.DeleteIndexAsync(id, ct);

                return Results.NoContent();           // 204
            })
            .WithOpenApi(o =>                        // for Swagger
            {
                o.Summary     = "Deletes a knowledge collection and its search index";
                o.Description = "Removes the MongoDB collection and its Atlas vector / search index.";
                o.Tags = [ new OpenApiTag { Name = "Knowledge" } ];
                return o;
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return group;
    }
}