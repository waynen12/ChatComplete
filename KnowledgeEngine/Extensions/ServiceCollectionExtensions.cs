using ChatCompletion.Config;
using KnowledgeEngine.Models;
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Persistence.Conversations;
using KnowledgeEngine.Persistence.IndexManagers;
using KnowledgeEngine.Persistence.VectorStores;
using KnowledgeEngine.Persistence.Sqlite;
using KnowledgeEngine.Persistence.Sqlite.Repositories;
using KnowledgeEngine.Persistence.Sqlite.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using MongoDB.Driver;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace KnowledgeEngine.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKnowledgeServices(this IServiceCollection services, ChatCompleteSettings settings)
    {
        // Register Vector Store Strategy based on configuration
        var vectorStoreProvider = settings.VectorStore?.Provider?.ToLower() ?? "mongodb";
        
        if (vectorStoreProvider == "qdrant")
        {
            // Register QdrantSettings
            services.AddSingleton(settings.VectorStore.Qdrant);
            
            // Register Qdrant services
            services.AddSingleton<QdrantVectorStore>(provider =>
            {
                var qdrantSettings = settings.VectorStore.Qdrant;
                
                // Create QdrantClient using proper constructor parameters
                // The constructor expects hostname only (not full URL) plus separate port/https/apiKey parameters
                // It internally builds the URI using UriBuilder(protocol, host, port) to avoid gRPC HTTP/2 errors
                var qdrantClient = new QdrantClient(
                    host: qdrantSettings.Host,
                    port: qdrantSettings.Port,
                    https: qdrantSettings.UseHttps,
                    apiKey: qdrantSettings.ApiKey
                );
                
                return new QdrantVectorStore(qdrantClient, ownsClient: true);
            });
            
            // Register Qdrant Index Manager
            services.AddScoped<IIndexManager, QdrantIndexManager>();
            
            // Register Qdrant Vector Store Strategy
            services.AddScoped<IVectorStoreStrategy, QdrantVectorStoreStrategy>();
        }
        else
        {
            // Register MongoDB client and database
            services.AddSingleton<IMongoClient>(provider =>
            {
                var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
                    ?? throw new InvalidOperationException("MONGODB_CONNECTION_STRING missing");
                return new MongoClient(connectionString);
            });

            services.AddSingleton<IMongoDatabase>(provider =>
            {
                var client = provider.GetRequiredService<IMongoClient>();
                return client.GetDatabase(settings.Atlas.ClusterName);
            });
            
            // Register MongoAtlasSettings
            services.AddSingleton(settings.Atlas);
            
            // Register MongoDB services (default)
            services.AddSingleton<MongoVectorStore>(provider =>
            {
                var database = provider.GetRequiredService<IMongoDatabase>();
                return new MongoVectorStore(database);
            });
            
            // Register MongoDB Index Manager (cast from singleton AtlasIndexManager)
            services.AddScoped<IIndexManager>(provider => provider.GetRequiredService<AtlasIndexManager>());
            
            // Register MongoDB Vector Store Strategy
            services.AddScoped<IVectorStoreStrategy, MongoVectorStoreStrategy>();
        }

        // Register OpenAI Embedding Service
        services.AddSingleton<ITextEmbeddingGenerationService>(provider =>
        {
            var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY missing");
            
            return new OpenAITextEmbeddingGenerationService(
                settings.TextEmbeddingModelName,
                openAiKey);
        });

        // Register AtlasIndexManager only for MongoDB deployments
        if (vectorStoreProvider != "qdrant")
        {
            services.AddSingleton<AtlasIndexManager>(provider =>
            {
                var atlasHttpClient = AtlasHttpClientFactory.CreateHttpClient();
                var manager = new AtlasIndexManager(settings.Atlas, atlasHttpClient, ownsHttpClient: true);
                
                // Initialize asynchronously in background - this is a compromise for DI registration
                Task.Run(async () => await manager.InitializeAsync()).Wait();
                
                return manager;
            });
        }

        // Register Knowledge Repository based on VectorStore provider
        if (vectorStoreProvider == "qdrant")
        {
            // For Qdrant, use SQLite knowledge repository (no MongoDB dependency)
            services.AddScoped<IKnowledgeRepository, SqliteKnowledgeRepository>();
        }
        else
        {
            // MongoDB provider
            services.AddScoped<IKnowledgeRepository, MongoKnowledgeRepository>();
        }

        // Register KnowledgeManager
        services.AddScoped<KnowledgeManager>();

        return services;
    }

    /// <summary>
    /// Registers SQLite persistence services for zero-dependency deployment
    /// Uses configuration-based database path with smart defaults:
    /// - Container: /app/data/knowledge.db (for volume mounts)
    /// - Development: {AppDirectory}/data/knowledge.db (reliable and portable)
    /// </summary>
    public static IServiceCollection AddSqlitePersistence(this IServiceCollection services, ChatCompleteSettings settings)
    {
        // Use configured path or smart default
        string databasePath;
        if (!string.IsNullOrEmpty(settings.DatabasePath))
        {
            databasePath = settings.DatabasePath;
        }
        else
        {
            // Smart default based on environment
            var isContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            
            if (isContainer)
            {
                // Container environment - use /app/data for volume mounts
                databasePath = "/app/data/knowledge.db";
            }
            else
            {
                // Development/Production - use app directory + data subfolder
                var appDirectory = AppContext.BaseDirectory;
                databasePath = Path.Combine(appDirectory, "data", "knowledge.db");
            }
        }

        // Register SQLite database context
        services.AddScoped<SqliteDbContext>(provider => new SqliteDbContext(databasePath));

        // Register encryption service (static methods, but good to have for DI)
        services.AddSingleton<EncryptionService>();

        // Register SQLite repositories
        services.AddScoped<SqliteAppSettingsRepository>();
        services.AddScoped<SqliteKnowledgeRepository>();
        services.AddScoped<SqliteConversationRepository>();

        // Register SQLite services
        services.AddScoped<SqliteAppSettingsService>();

        // Replace conversation repository registration
        services.AddScoped<IConversationRepository, SqliteConversationRepository>();

        return services;
    }
}
