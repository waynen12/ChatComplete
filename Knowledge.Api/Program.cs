// Knowledge.Api/Program.cs

using System.Text.Json.Serialization;
using ChatCompletion;
using ChatCompletion.Config;
using Microsoft.Extensions.AI;
using Knowledge.Analytics.Extensions;
using Knowledge.Api.Constants;
using Knowledge.Api.Endpoints;
using Knowledge.Api.Filters;
using Knowledge.Api.Options;
using Knowledge.Api.Services;
using Knowledge.Contracts;
using KnowledgeEngine;
using KnowledgeEngine.Agents.Plugins;
using KnowledgeEngine.Chat;
using KnowledgeEngine.Extensions;
using KnowledgeEngine.Logging; // whatever namespace holds LoggerProvider
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Persistence.Conversations;
using KnowledgeEngine.Persistence.IndexManagers;
using KnowledgeEngine.Persistence.Sqlite;
using KnowledgeEngine.Persistence.Sqlite.Repositories;
using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Serilog;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

LoggerProvider.ConfigureLogger(); // boots Log.Logger

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(LoggerProvider.Logger, dispose: true);

// ── Configuration binding ─────────────────────────────────────────────────────
builder.Services.Configure<ChatCompleteSettings>(
    builder.Configuration.GetSection(ApiConstants.ConfigSections.ChatCompleteSettings)
);

builder.Services.Configure<CorsOptions>(
    builder.Configuration.GetSection(ApiConstants.ConfigSections.Cors)
);

builder.Services.Configure<JsonOptions>(opts =>
{
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var settings = builder
    .Configuration.GetSection(ApiConstants.ConfigSections.ChatCompleteSettings)
    .Get<ChatCompleteSettings>();
if (settings == null)
{
    LoggerProvider.Logger.Error($"Missing {ApiConstants.ConfigSections.ChatCompleteSettings}");
    throw new InvalidOperationException(
        $"Missing {ApiConstants.ConfigSections.ChatCompleteSettings}"
    );
}
SettingsProvider.Initialize(settings);

// Register settings as singleton for dependency injection
builder.Services.AddSingleton(settings);

var openAiKey = Environment.GetEnvironmentVariable(ApiConstants.EnvironmentVariables.OpenAiApiKey);

// Configure embedding service based on enhanced provider selection
var activeProvider = settings.EmbeddingProviders.ActiveProvider.ToLower();
if (activeProvider == "ollama")
{
    // Add Ollama embedding service with configuration
    builder.Services.AddHttpClient<OllamaEmbeddingService>();
    builder.Services.AddScoped<IEmbeddingGenerator<string, Embedding<float>>>(serviceProvider =>
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(OllamaEmbeddingService));
        var ollamaConfig = settings.EmbeddingProviders.Ollama;
        return new OllamaEmbeddingService(httpClient, settings.OllamaBaseUrl, ollamaConfig.ModelName);
    });
}
else if (activeProvider == "openai")
{
    // OpenAI embedding service
    if (string.IsNullOrEmpty(openAiKey))
    {
        throw new InvalidOperationException(
            "OpenAI API key is required when using OpenAI embedding provider. Set OPENAI_API_KEY environment variable.");
    }
    var openAiConfig = settings.EmbeddingProviders.OpenAI;
    builder.Services.AddOpenAIEmbeddingGenerator(openAiConfig.ModelName, openAiKey);
}
else
{
    throw new InvalidOperationException($"Unsupported embedding provider: {activeProvider}");
}

// Add modern KernelFactory
builder.Services.AddSingleton<KernelFactory>();

// Add SQLite persistence for zero-dependency deployment
builder.Services.AddSqlitePersistence(settings);

// Add analytics services (uses existing DAL from AddSqlitePersistence)
builder.Services.AddAnalyticsServices(builder.Configuration);

// Add SignalR for real-time analytics updates
builder.Services.AddSignalR();

// Add conversation persistence (SQLite is now used for conversations in Qdrant mode)
var vectorStoreProvider = settings.VectorStore?.Provider?.ToLower() ?? "mongodb";
if (vectorStoreProvider == "qdrant")
{
    builder.Services.AddSqliteConversationPersistence();
}
else
{
    builder.Services.AddConversationPersistence();
}

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
    return new ChatComplete(knowledgeManager, cfg, sp);
});

// Register agent plugins for cross-knowledge search capabilities
builder.Services.AddScoped<CrossKnowledgeSearchPlugin>();
builder.Services.AddScoped<ModelRecommendationAgent>();
builder.Services.AddScoped<KnowledgeAnalyticsAgent>();

// ── CORS policy for the Vite front-end ────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        ApiConstants.CorsPolicies.DevFrontend,
        (policyBuilder) =>
        {
            var cors = builder
                .Configuration.GetSection(ApiConstants.ConfigSections.Cors)
                .Get<CorsOptions>();
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
                .AllowCredentials() // Required for SignalR
                .SetPreflightMaxAge(TimeSpan.FromHours(cors.MaxAgeHours));
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

// Register chat service based on vector store provider
if (vectorStoreProvider == "qdrant")
{
    builder.Services.AddScoped<IChatService, SqliteChatService>();
}
else
{
    builder.Services.AddScoped<IChatService, MongoChatService>();
}

// Register Ollama API service
builder.Services.AddHttpClient<IOllamaApiService, OllamaApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(180); // Increased for tool calling
    client.DefaultRequestHeaders.Add("User-Agent", "AIKnowledgeManager/1.0");
});

// Register Ollama download service for real-time progress tracking
builder.Services.AddScoped<OllamaDownloadService>();

// Register analytics update service for real-time WebSocket updates
builder.Services.Configure<Knowledge.Api.Services.AnalyticsUpdateOptions>(
    builder.Configuration.GetSection("Analytics:RealTimeUpdates")
);
builder.Services.AddHostedService<Knowledge.Api.Services.AnalyticsUpdateService>();

var app = builder.Build();

// Initialize SQLite database for Qdrant deployments
if (vectorStoreProvider == "qdrant")
{
    using var scope = app.Services.CreateScope();
    var sqliteContext = scope.ServiceProvider.GetRequiredService<SqliteDbContext>();
    var appSettingsRepo = scope.ServiceProvider.GetRequiredService<SqliteAppSettingsRepository>();

    // Ensure database is created and initialized
    _ = await sqliteContext.GetConnectionAsync();
    await appSettingsRepo.InitializeDefaultsAsync();
}

// ─── API route group ─────────────────────────────────────────────────────────
var api = app.MapGroup(ApiConstants.Routes.Api).WithOpenApi();

// ─── Endpoint mappings ───────────────────────────────────────────────────────
api.MapGroup(ApiConstants.Routes.Knowledge).MapKnowledgeEndpoints();
api.MapGroup(ApiConstants.Routes.Chat).MapChatEndpoints();
api.MapGroup(ApiConstants.Routes.Ollama).MapOllamaEndpoints();
api.MapGroup(ApiConstants.Routes.Analytics).MapAnalyticsEndpoints();
api.MapGroup("").MapHealthEndpoints(); // Health endpoints at root level

// Map SignalR hub for real-time analytics
app.MapHub<Knowledge.Api.Hubs.AnalyticsHub>("/api/analytics/hub");

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
app.UseCors(ApiConstants.CorsPolicies.DevFrontend); // CORS must appear before MapXXX

// if (app.Environment.IsDevelopment())
// {
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    // ---- Customisations start here ----
    options.RoutePrefix = "docs"; // UI will be at /docs
    options.SwaggerEndpoint(ApiConstants.Paths.SwaggerEndpoint, "Knowledge API v1");
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
    options.SpecUrl = ApiConstants.Paths.SwaggerEndpoint;
    options.DocumentTitle = "Knowledge API – ReDoc";
});

//}

// ── Static file serving for React frontend (for container deployment) ────────
if (
    app.Environment.IsProduction()
    || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
)
{
    // Serve static files from wwwroot (React build output)
    app.UseStaticFiles();

    // Configure default files (index.html)
    app.UseDefaultFiles();
}

// ── Client-side routing fallback ─────────────────────────────────────────────
// This must come after API routes but before app.Run()
// Fallback to index.html for any non-API routes (React client-side routing)
if (
    app.Environment.IsProduction()
    || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
)
{
    app.MapFallbackToFile("index.html");
}

app.Run();
