// Knowledge.Api/Program.cs

using ChatCompletion;
using ChatCompletion.Config;
using Knowledge.Api.Options;
using Knowledge.Api.Services;
using Knowledge.Contracts;
using KnowledgeEngine;
using KnowledgeEngine.Chat;
using KnowledgeEngine.Logging; // whatever namespace holds LoggerProvider
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Persistence.Conversations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
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

var mongoUri =
    Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
    ?? throw new InvalidOperationException("MONGODB_CONNECTION_STRING missing");
var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
var mongoClient = new MongoClient(mongoUri);
builder.Services.AddSingleton<IMongoClient>(mongoClient);
var cfg = builder.Configuration.GetSection("ChatCompleteSettings").Get<ChatCompleteSettings>();

// 1️⃣  Vector store + embeddings
var db = mongoClient.GetDatabase(cfg!.Atlas.ClusterName);
builder.Services.AddSingleton<Microsoft.SemanticKernel.Connectors.MongoDB.MongoVectorStore>(_ => 
    new Microsoft.SemanticKernel.Connectors.MongoDB.MongoVectorStore(db));

// Add modern Microsoft.Extensions.AI embedding service directly
builder.Services.AddOpenAIEmbeddingGenerator(settings.TextEmbeddingModelName, openAiKey);

// 2. Kernel that uses the memory + OpenAI
// 2️⃣  Chat-model provider factory (we keep provider switch here)
builder.Services.AddSingleton<KernelFactory>();

builder.Services.AddSingleton<IMongoDatabase>(sp =>
    mongoClient.GetDatabase(settings.Atlas.DatabaseName)
);
builder.Services.AddSingleton<IKnowledgeRepository, MongoKnowledgeRepository>();
builder.Services.AddConversationPersistence();

// 1️⃣  AtlasIndexManager (async factory → sync bridge)
builder.Services.AddSingleton<AtlasIndexManager>(sp =>
{
    // Get the default collection name – or pass empty, the ingest step
    // will call CreateAsync(collectionName) again per import
    var defaultColl = settings.Atlas.CollectionName;
    // Safe because this runs only once at startup
    var mgr = AtlasIndexManager.CreateAsync(defaultColl).GetAwaiter().GetResult();

    if (mgr == null)
        throw new InvalidOperationException("Failed to create AtlasIndexManager");

    return mgr;
});

// 2️⃣  KnowledgeManager depends on vector store + embedding service + index-manager
builder.Services.AddSingleton<KnowledgeManager>(sp =>
{
    var vectorStore = sp.GetRequiredService<Microsoft.SemanticKernel.Connectors.MongoDB.MongoVectorStore>();
    var embeddingService = sp.GetRequiredService<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>();
    var index = sp.GetRequiredService<AtlasIndexManager>();
    var mongoDatabase = sp.GetRequiredService<IMongoDatabase>();
    return new KnowledgeManager(vectorStore, embeddingService, index, mongoDatabase);
});

// 3️⃣  Ingest service (already added earlier)
builder.Services.AddSingleton<IKnowledgeIngestService, KnowledgeIngestService>();

// Register ChatComplete service for MongoChatService dependency (after KnowledgeManager)
builder.Services.AddSingleton<ChatComplete>(sp =>
{
    var knowledgeManager = sp.GetRequiredService<KnowledgeManager>();
    var cfg = sp.GetRequiredService<IOptions<ChatCompleteSettings>>().Value;
    return new ChatComplete(knowledgeManager, cfg);
});

// ── CORS policy for the Vite front-end ────────────────────────────────────────
const string DevCors = "DevFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        DevCors,
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

// reuse existing helpers
builder.Services.AddSingleton<IChatService, MongoChatService>();

var app = builder.Build();

// ─── API route group ─────────────────────────────────────────────────────────
var api = app.MapGroup("/api").WithOpenApi();

// .WithGroupName("api");

// ─── Stub endpoints (no group) ───────────────────────────────────────────────

// 1) GET /api/knowledge
api.MapGet(
        "/knowledge",
        async (IKnowledgeRepository repo, CancellationToken ct) =>
            Results.Ok(await repo.GetAllAsync(ct))
    )
    .WithOpenApi()
    .Produces<IEnumerable<KnowledgeSummaryDto>>(StatusCodes.Status200OK)
    .WithTags("Knowledge");

// 2) POST /api/knowledge
api.MapPost(
        "/knowledge",
        async (
            [FromForm] string name,
            [FromForm] IFormFileCollection files,
            IKnowledgeIngestService ingest,
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
        async (ChatRequestDto dto, IChatService chat, CancellationToken ct) =>
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
            ["provider"]             = new OpenApiString("Ollama")
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
    .Produces<ChatResponseDto>(StatusCodes.Status200OK)
    .WithTags("Chat");

// 4) DELETE /api/knowledge/{id}
api.MapDelete(
        "/knowledge/{id}",
        async (string id,
            IKnowledgeRepository repo,
            AtlasIndexManager indexMgr,
            CancellationToken ct) =>
        {
            // 0. Fast-fail if the collection doesn’t exist
            if (!await repo.ExistsAsync(id, ct))
                return Results.NotFound($"Collection “{id}” not found.");

            // 1. Drop the MongoDB collection (+ vector store rows)
            await repo.DeleteAsync(id, ct);

            // 2. Drop the Atlas search index (ignore 404 – index may not exist)
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


api.MapGet("/ping", () => Results.Ok("pong"))
    .WithTags("Health")
    .WithOpenApi()
    .Produces<string>(StatusCodes.Status200OK)
    .WithName("Health")
    .WithOpenApi();

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
app.UseCors(DevCors); // CORS must appear before MapXXX

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

app.Run();
