// Knowledge.Api/Program.cs

using ChatCompletion;
using ChatCompletion.Config;
using Knowledge.Api.Options;
using Knowledge.Api.Services;
using Knowledge.Contracts;
using KnowledgeEngine;
using KnowledgeEngine.Chat;
using KnowledgeEngine.Extensions;
using KnowledgeEngine.Logging; // whatever namespace holds LoggerProvider
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Persistence.IndexManagers;
using KnowledgeEngine.Persistence.VectorStores;
using KnowledgeEngine.Persistence.Conversations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Any;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;
 

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050


LoggerProvider.ConfigureLogger(); // boots Log.Logger

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(LoggerProvider.Logger, dispose: true);

// ── Configuration binding ─────────────────────────────────────────────────────
builder.Services.Configure<ChatCompleteSettings>(
    builder.Configuration.GetSection(nameof(ChatCompleteSettings))
);

builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection("Cors"));

builder.Services.Configure<JsonOptions>(opts =>
{
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var settings = builder.Configuration.GetSection("ChatCompleteSettings").Get<ChatCompleteSettings>();
if (settings == null)
{
    LoggerProvider.Logger.Error("Missing ChatCompleteSettings");    
    throw new InvalidOperationException("Missing ChatCompleteSettings");    
}
SettingsProvider.Initialize(settings); 

var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;

// Add modern Microsoft.Extensions.AI embedding service directly
builder.Services.AddOpenAIEmbeddingGenerator(settings.TextEmbeddingModelName, openAiKey);

// Add modern KernelFactory
builder.Services.AddSingleton<KernelFactory>();

// Add conversation persistence (MongoDB is always used for conversations)
builder.Services.AddConversationPersistence();

// Use ServiceCollectionExtensions for strategy pattern registration
builder.Services.AddKnowledgeServices(settings);

// 3️⃣  Ingest service (scoped to match KnowledgeManager)
builder.Services.AddScoped<IKnowledgeIngestService, KnowledgeIngestService>();

// Register ChatComplete service for MongoChatService dependency (after KnowledgeManager)
// Changed from Singleton to Scoped to match KnowledgeManager lifetime
builder.Services.AddScoped<ChatComplete>(sp =>
{
    var knowledgeManager = sp.GetRequiredService<KnowledgeManager>();
    var cfg = sp.GetRequiredService<IOptions<ChatCompleteSettings>>().Value;
    return new ChatComplete(knowledgeManager, cfg);
});

// ── CORS policy for the Vite front-end ────────────────────────────────────────
const string devCors = "DevFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        devCors,
        (policyBuilder) =>
        {
            var cors = builder.Configuration.GetSection("Cors").Get<CorsOptions>();
            if (cors is null)
            {
                throw new InvalidOperationException(
                    "Missing Cors configuration. Please add Cors settings to appsettings.json"
                );
            }

            policyBuilder
                .WithOrigins(cors.AllowedOrigins)
                .WithHeaders(cors.AllowedHeaders)
                .AllowAnyMethod()
                .SetPreflightMaxAge(TimeSpan.FromHours(cors.MaxAgeHours));
            // no .AllowCredentials() yet
        }
    );
});

// (later) services for Swagger, endpoints, KnowledgeEngine DI, etc.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Pull in minimal-API metadata automatically
    options.SwaggerDoc("v1", new() { Title = "Knowledge API", Version = "v1" });

    // ✦ XML comments for enriched Swagger descriptions
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "Knowledge.Api.xml");
    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    options.OperationFilter<PythonCodeSampleFilter>(); //  ← new line
});

// reuse existing helpers - changed to Scoped to match ChatComplete lifetime
builder.Services.AddScoped<IChatService, MongoChatService>();

var app = builder.Build();

// ─── API route group ─────────────────────────────────────────────────────────
var api = app.MapGroup("/api").WithOpenApi();

// .WithGroupName("api");

// ─── Stub endpoints (no group) ───────────────────────────────────────────────

// 1) GET /api/knowledge
api.MapGet(
        "/knowledge",
        async ([FromServices] IVectorStoreStrategy vectorStore, CancellationToken ct) =>
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
        }
    )
    .WithOpenApi()
    .Produces<IEnumerable<KnowledgeSummaryDto>>()
    .WithTags("Knowledge");

// 2) POST /api/knowledge
api.MapPost(
        "/knowledge",
        async (
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

            return Results.Created($"/api/knowledge/{name}", new { id = name });
        }
    )
    .Accepts<IFormFileCollection>("multipart/form-data")
    .Produces(201)
    .DisableAntiforgery();

// 3) POST /api/chat
api.MapPost(
        "/chat",
        async (ChatRequestDto dto, [FromServices] IChatService chat, CancellationToken ct) =>
        {
            var reply = await chat.GetReplyAsync(dto, dto.Provider, ct);
            if (dto.StripMarkdown)
                reply = MarkdownStripper.ToPlain(reply);

            return Results.Ok(new ChatResponseDto
            {
                  Reply = reply,
                  ConversationId = dto.ConversationId!
            });
        }
    )
    .WithOpenApi(op =>
    {
        op.Summary = "Chat with a model";

        // Build a sample JSON object
        var sample = new OpenApiObject
        {
            ["knowledgeId"]          = new OpenApiNull(),          // null = global chat
            ["message"]              = new OpenApiString("Hello"),
            ["temperature"]          = new OpenApiDouble(0.7),
            ["stripMarkdown"]        = new OpenApiBoolean(false),
            ["useExtendedInstructions"] = new OpenApiBoolean(true),
            ["conversationId"]       = new OpenApiNull(),
            ["provider"]             = new OpenApiString("Ollama"),
            ["ollamaModel"]          = new OpenApiString("llama3.2:3b")
        };

        // Ensure requestBody is present and targeted at application/json
        op.RequestBody ??= new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>()
        };
        op.RequestBody.Content["application/json"] = new OpenApiMediaType
        {
            Example = sample         // 👈 here’s the example
        };

        return op;
    })
    .Produces<ChatResponseDto>()
    .WithTags("Chat");

// 4) DELETE /api/knowledge/{id}
api.MapDelete(
        "/knowledge/{id}",
        async (string id,
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


// Basic ping endpoint for simple health checks
api.MapGet("/ping", () => Results.Ok("pong"))
    .WithTags("Health")
    .WithOpenApi()
    .Produces<string>()
    .WithName("BasicHealth");

// Comprehensive health check endpoint
api.MapGet("/health", async (
    [FromServices] IVectorStoreStrategy vectorStore,
    [FromServices] IIndexManager indexManager,
    CancellationToken ct) =>
{
    var healthStatus = new
    {
        Status = "healthy",
        Timestamp = DateTime.UtcNow,
        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
        Container = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true",
        Checks = new Dictionary<string, object>()
    };

    try
    {
        // Check vector store connectivity
        try
        {
            var collections = await vectorStore.ListCollectionsAsync(ct);
            healthStatus.Checks["VectorStore"] = new { Status = "healthy", Collections = collections?.Count() ?? 0 };
        }
        catch (Exception ex)
        {
            healthStatus.Checks["VectorStore"] = new { Status = "unhealthy", Error = ex.Message };
        }

        // Check disk space (data directory)
        try
        {
            var dataPath = "/app/data";
            if (Directory.Exists(dataPath))
            {
                var drive = new DriveInfo(Path.GetPathRoot(dataPath) ?? "/");
                var freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                healthStatus.Checks["DiskSpace"] = new 
                { 
                    Status = freeSpaceGB > 1.0 ? "healthy" : "warning",
                    AvailableGB = Math.Round(freeSpaceGB, 2),
                    TotalGB = Math.Round(drive.TotalSize / (1024.0 * 1024.0 * 1024.0), 2)
                };
            }
            else
            {
                healthStatus.Checks["DiskSpace"] = new { Status = "unknown", Message = "Data directory not found" };
            }
        }
        catch (Exception ex)
        {
            healthStatus.Checks["DiskSpace"] = new { Status = "error", Error = ex.Message };
        }

        // Check memory usage
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var memoryMB = process.WorkingSet64 / (1024.0 * 1024.0);
            healthStatus.Checks["Memory"] = new 
            { 
                Status = memoryMB < 1000 ? "healthy" : "warning",
                WorkingSetMB = Math.Round(memoryMB, 2)
            };
        }
        catch (Exception ex)
        {
            healthStatus.Checks["Memory"] = new { Status = "error", Error = ex.Message };
        }

        // Determine overall status
        var hasUnhealthyChecks = healthStatus.Checks.Values
            .Any(check => check.GetType().GetProperty("Status")?.GetValue(check)?.ToString() == "unhealthy");
        
        return hasUnhealthyChecks 
            ? Results.Json(healthStatus with { Status = "degraded" }, statusCode: 503)
            : Results.Ok(healthStatus);
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            Status = "unhealthy",
            Timestamp = DateTime.UtcNow,
            Error = ex.Message
        }, statusCode: 503);
    }
})
.WithOpenApi(op =>
{
    op.Summary = "Comprehensive health check";
    op.Description = "Returns detailed health status including vector store, disk space, and memory usage";
    op.Tags = [ new OpenApiTag { Name = "Health" } ];
    return op;
})
.WithTags("Health");

// 5) GET /api/ollama/models - Fetch available Ollama models
api.MapGet("/ollama/models", async (CancellationToken ct) =>
    {
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "ollama";
            process.StartInfo.Arguments = "list";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            if (!process.Start())
            {
                return Results.Problem("Failed to start ollama command", statusCode: 500);
            }

            await process.WaitForExitAsync(ct);
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            var error = await process.StandardError.ReadToEndAsync(ct);

            if (process.ExitCode != 0)
            {
                return Results.Problem($"Ollama command failed: {error}", statusCode: 500);
            }

            // Parse the output - Ollama list format is typically:
            // NAME            ID              SIZE    MODIFIED
            // model1:latest   abc123def...    1.2GB   2 hours ago
            // model2:8b       def456ghi...    8.5GB   1 day ago
            
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var models = new List<string>();
            
            // Skip header line (first line) and process model names
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    // Get the first column (model name)
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        models.Add(parts[0]);
                    }
                }
            }

            return Results.Ok(models);
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(ex, "Failed to fetch Ollama models");
            return Results.Problem("Failed to fetch Ollama models", statusCode: 500);
        }
    })
    .WithOpenApi(op =>
    {
        op.Summary = "Get available Ollama models";
        op.Description = "Returns a list of locally installed Ollama models by executing 'ollama list'";
        op.Tags = [ new OpenApiTag { Name = "Ollama" } ];
        return op;
    })
    .Produces<List<string>>()
    .WithTags("Ollama");

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseSerilogRequestLogging(); // keep logs consistent
app.Use(
    (ctx, next) =>
    {
        // trim accidental double-slashes in the path
        if (!string.IsNullOrEmpty(ctx.Request.Path.Value))
        {
            ctx.Request.Path = ctx.Request.Path.Value.Replace("//", "/");
        }
        return next();
    }
);
app.UseCors(devCors); // CORS must appear before MapXXX

// if (app.Environment.IsDevelopment())
// {
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    // ---- Customisations start here ----
    options.RoutePrefix = "docs"; // UI will be at /docs
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Knowledge API v1");
    options.DocumentTitle = "Knowledge API – Swagger";

    options.ConfigObject.AdditionalItems["requestSnippetsEnabled"] = true;
    options.ConfigObject.AdditionalItems["requestSnippets"] = new Dictionary<string, object?>
    {
        ["defaultExpanded"] = true,
        ["generators"] = new Dictionary<string, object?>
        {
            ["python"] = new Dictionary<string, object?>
            {
                ["lang"] = "Python",
                ["library"] = "requests",
            },
        },
    };
    options.DocumentTitle = "Knowledge API – Swagger";
});

app.UseReDoc(options =>
{
    options.RoutePrefix = "redoc"; // docs at /redoc
    options.SpecUrl = "/swagger/v1/swagger.json";
    options.DocumentTitle = "Knowledge API – ReDoc";
});

//}

// ── Static file serving for React frontend (for container deployment) ────────
if (app.Environment.IsProduction() || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
{
    // Serve static files from wwwroot (React build output)
    app.UseStaticFiles();
    
    // Configure default files (index.html)
    app.UseDefaultFiles();
}

// ── Client-side routing fallback ─────────────────────────────────────────────
// This must come after API routes but before app.Run()
// Fallback to index.html for any non-API routes (React client-side routing)
if (app.Environment.IsProduction() || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
{
    app.MapFallbackToFile("index.html");
}

app.Run();
