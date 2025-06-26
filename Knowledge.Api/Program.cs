// Knowledge.Api/Program.cs
using ChatCompletion;
using ChatCompletion.Config;
using Knowledge.Api.Filters;
using Knowledge.Api.Options;
using Knowledge.Contracts;
using KnowledgeEngine; // namespace where ChatCompleteSettings lives
using KnowledgeEngine.Chat;
using KnowledgeEngine.Logging; // whatever namespace holds LoggerProvider
using KnowledgeEngine.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using MongoDB.Driver;
using Serilog;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

LoggerProvider.ConfigureLogger(); // boots Log.Logger

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(LoggerProvider.Logger, dispose: true);

// ── Configuration binding ─────────────────────────────────────────────────────
builder.Services.Configure<ChatCompleteSettings>(
    builder.Configuration.GetSection(nameof(ChatCompleteSettings))
);

builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection("Cors"));

var settings = builder.Configuration.GetSection("ChatCompleteSettings").Get<ChatCompleteSettings>();

var mongoUri =
    Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
    ?? throw new InvalidOperationException("MONGODB_CONNECTION_STRING missing");
var mongoClient = new MongoClient(mongoUri);
builder.Services.AddSingleton<IMongoClient>(mongoClient);

// 1. ISemanticTextMemory  (vector store + embeddings)
builder.Services.AddSingleton<ISemanticTextMemory>(sp =>
{
    var cfg = sp.GetRequiredService<IOptions<ChatCompleteSettings>>().Value;

    // reuse your helper to build the memory store
    return KernelHelper.GetMongoDBMemoryStore(
        cfg.Atlas.ClusterName, // database == clusterName today
        cfg.Atlas.SearchIndexName,
        cfg.TextEmbeddingModelName
    );
});

// 2. Kernel that uses the memory + OpenAI
builder.Services.AddSingleton<Kernel>(sp =>
{
    var cfg = sp.GetRequiredService<IOptions<ChatCompleteSettings>>().Value;
    var openAiKey =
        Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        ?? throw new InvalidOperationException("OPENAI_API_KEY missing");

    return KernelHelper.GetKernel(cfg);
});

builder.Services.AddSingleton<ChatComplete>(sp =>
{
    var kernel = sp.GetRequiredService<Kernel>();
    var memory = sp.GetRequiredService<ISemanticTextMemory>();
    var cfg = sp.GetRequiredService<IOptions<ChatCompleteSettings>>().Value;
    return new ChatComplete(memory, kernel, cfg.SystemPrompt, cfg.Temperature);
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
    mongoClient.GetDatabase(settings.Atlas.DatabaseName)
);
builder.Services.AddSingleton<IKnowledgeRepository, MongoKnowledgeRepository>();

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
        (HttpRequest _) =>
        {
            var response = new CreateKnowledgeResponseDto { Id = "demo" };
            return Results.Created($"/api/knowledge/{response.Id}", response);
        }
    )
    .Accepts<IFormFileCollection>("multipart/form-data")
    .AddEndpointFilter<ValidationFilter>()
    .WithOpenApi()
    .Produces<CreateKnowledgeResponseDto>(StatusCodes.Status201Created)
    .WithTags("Knowledge")
    .WithOpenApi();

// 3) POST /api/chat
api.MapPost(
        "/chat",
        async (ChatRequestDto dto, IChatService chat, CancellationToken ct) =>
        {
            var reply = await chat.GetReplyAsync(dto, ct);
            if (dto.StripMarkdown)
                reply = MarkdownStripper.ToPlain(reply);

            return Results.Ok(new ChatResponseDto { Reply = reply });
        }
    )
    .WithOpenApi()
    .Produces<ChatResponseDto>(StatusCodes.Status200OK)
    .WithTags("Chat");

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
