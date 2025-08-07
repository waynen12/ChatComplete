using ChatCompletion.Config;
using KnowledgeEngine.Models;
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Persistence.IndexManagers;
using KnowledgeEngine.Persistence.VectorStores;
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

        // Register AtlasIndexManager as singleton with proper initialization
        services.AddSingleton<AtlasIndexManager>(provider =>
        {
            var atlasHttpClient = AtlasHttpClientFactory.CreateHttpClient();
            var manager = new AtlasIndexManager(settings.Atlas, atlasHttpClient, ownsHttpClient: true);
            
            // Initialize asynchronously in background - this is a compromise for DI registration
            Task.Run(async () => await manager.InitializeAsync()).Wait();
            
            return manager;
        });

        // Register Knowledge Repository based on VectorStore provider
        if (vectorStoreProvider == "qdrant")
        {
            // For Qdrant, we need a QdrantKnowledgeRepository implementation
            // For now, still use MongoDB for metadata operations but this should be extended
            services.AddScoped<IKnowledgeRepository, MongoKnowledgeRepository>();
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
}
