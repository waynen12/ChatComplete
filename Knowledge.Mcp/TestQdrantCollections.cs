using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ChatCompletion.Config;
using KnowledgeEngine.Extensions;

namespace Knowledge.Mcp;

/// <summary>
/// Simple test program to debug Qdrant collection listing issue
/// </summary>
public static class TestQdrantCollections
{
    public static async Task<int> RunTest(string[] args)
    {
        Console.WriteLine("Testing Qdrant collection listing...");

        try
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var chatCompleteSettings = configuration.GetSection("ChatCompleteSettings").Get<ChatCompleteSettings>()
                ?? throw new InvalidOperationException("ChatCompleteSettings configuration is missing");

            Console.WriteLine($"Qdrant Host: {chatCompleteSettings.VectorStore.Qdrant.Host}");
            Console.WriteLine($"Qdrant Port: {chatCompleteSettings.VectorStore.Qdrant.Port}");

            // Create Qdrant client directly - try both configurations
            Console.WriteLine("Attempting to create Qdrant client...");
            
            // First try: gRPC client (default for Semantic Kernel)
            var qdrantClient = new Qdrant.Client.QdrantClient(
                host: chatCompleteSettings.VectorStore.Qdrant.Host,
                port: chatCompleteSettings.VectorStore.Qdrant.Port,
                https: chatCompleteSettings.VectorStore.Qdrant.UseHttps,
                apiKey: chatCompleteSettings.VectorStore.Qdrant.ApiKey
            );
            
            Console.WriteLine($"Created Qdrant client - Host: {chatCompleteSettings.VectorStore.Qdrant.Host}, Port: {chatCompleteSettings.VectorStore.Qdrant.Port}, HTTPS: {chatCompleteSettings.VectorStore.Qdrant.UseHttps}");

            var qdrantVectorStore = new QdrantVectorStore(qdrantClient, ownsClient: true);

            Console.WriteLine("Testing direct QdrantVectorStore.ListCollectionNamesAsync()...");
            var collectionNames = new List<string>();
            await foreach (var name in qdrantVectorStore.ListCollectionNamesAsync())
            {
                collectionNames.Add(name);
                Console.WriteLine($"Found collection: {name}");
            }

            Console.WriteLine($"\nTotal collections found: {collectionNames.Count}");

            // Now test the strategy wrapper
            Console.WriteLine("\nTesting QdrantVectorStoreStrategy.ListCollectionsAsync()...");

            var services = new ServiceCollection();
            services.AddSingleton(chatCompleteSettings.VectorStore.Qdrant);
            services.AddSingleton(qdrantVectorStore);
            services.AddSingleton(chatCompleteSettings);
            
            // Add a mock index manager for the strategy
            services.AddSingleton<KnowledgeEngine.Persistence.IndexManagers.IIndexManager, MockIndexManager>();
            services.AddSingleton<IVectorStoreStrategy, QdrantVectorStoreStrategy>();

            var serviceProvider = services.BuildServiceProvider();
            var strategy = serviceProvider.GetRequiredService<IVectorStoreStrategy>();

            var strategyCollections = await strategy.ListCollectionsAsync();
            Console.WriteLine($"Strategy found {strategyCollections.Count} collections:");
            foreach (var collection in strategyCollections)
            {
                Console.WriteLine($"  - {collection}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return 1;
        }
    }
}

/// <summary>
/// Mock index manager for testing purposes
/// </summary>
public class MockIndexManager : KnowledgeEngine.Persistence.IndexManagers.IIndexManager
{
    public Task CreateIndexAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task DeleteIndexAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> IndexExistsAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task WaitForIndexReadyAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<string?> GetIndexIdAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(collectionName);
    }
}