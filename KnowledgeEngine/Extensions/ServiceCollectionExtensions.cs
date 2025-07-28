using ChatCompletion.Config;
using KnowledgeEngine.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using MongoDB.Driver;

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

        // Register MongoDB Vector Store
        services.AddSingleton<MongoVectorStore>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return new MongoVectorStore(database);
        });

        // Register OpenAI Embedding Service
        services.AddSingleton<ITextEmbeddingGenerationService>(provider =>
        {
            var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY missing");
            
            return new OpenAITextEmbeddingGenerationService(
                settings.TextEmbeddingModelName,
                openAiKey);
        });

        // Register AtlasIndexManager
        services.AddSingleton<AtlasIndexManager>(provider =>
        {
            // Note: AtlasIndexManager.CreateAsync returns Task<AtlasIndexManager?>, 
            // so we'll need to handle this differently in the actual usage
            throw new NotImplementedException("AtlasIndexManager requires async initialization");
        });

        // Register KnowledgeManager
        services.AddScoped<KnowledgeManager>();

        return services;
    }
}
