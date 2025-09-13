// Test script to upload a document and test search using the actual application code
using System;
using System.IO;
using System.Threading.Tasks;
using ChatCompletion.Config;
using Knowledge.Api.Services;
using KnowledgeEngine;
using KnowledgeEngine.Extensions;
using KnowledgeEngine.Logging;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🧪 Testing document upload and search with Ollama embeddings...\n");

        // Initialize logging
        LoggerProvider.Initialize(LogLevel.Information);

        try
        {
            // Load configuration
            var config = new ConfigurationBuilder()
                .SetBasePath("/home/wayne/repos/ChatComplete/Knowledge.Api")
                .AddJsonFile("appsettings.json")
                .Build();

            var settings = config.GetSection("ChatCompleteSettings").Get<ChatCompleteSettings>();
            if (settings == null)
            {
                Console.WriteLine("❌ Failed to load ChatCompleteSettings");
                return;
            }

            Console.WriteLine($"🔧 Configuration loaded:");
            Console.WriteLine($"   EmbeddingProvider: {settings.EmbeddingProvider}");
            Console.WriteLine($"   OllamaEmbeddingModel: {settings.OllamaEmbeddingModel}");
            Console.WriteLine(
                $"   VectorStore Provider: {settings.VectorStore?.Provider ?? "Not set"}"
            );
            Console.WriteLine();

            // Initialize settings provider
            SettingsProvider.Initialize(settings);

            // Setup services like the main application
            var services = new ServiceCollection();
            services.AddLogging(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Information)
            );
            services.AddSingleton(settings);
            services.AddHttpClient();

            // Add embedding service (same as main app)
            if (settings.EmbeddingProvider?.ToLower() == "ollama")
            {
                services.AddHttpClient<OllamaEmbeddingService>();
                services.AddScoped<IEmbeddingGenerator<string, Embedding<float>>>(serviceProvider =>
                {
                    var httpClientFactory =
                        serviceProvider.GetRequiredService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient(nameof(OllamaEmbeddingService));
                    return new OllamaEmbeddingService(
                        httpClient,
                        settings.OllamaBaseUrl,
                        settings.OllamaEmbeddingModel
                    );
                });
            }
            else
            {
                Console.WriteLine("❌ This test requires EmbeddingProvider = 'Ollama'");
                return;
            }

            // Add vector store and knowledge engine services
            services.AddSqlitePersistence(settings);

            var serviceProvider = services.BuildServiceProvider();

            // Get the knowledge manager
            var knowledgeManager = serviceProvider.GetRequiredService<KnowledgeManager>();
            var embeddingService = serviceProvider.GetRequiredService<
                IEmbeddingGenerator<string, Embedding<float>>
            >();

            Console.WriteLine($"✅ Services initialized successfully");
            Console.WriteLine($"   Embedding service: {embeddingService.GetType().Name}");
            Console.WriteLine($"   Knowledge manager: {knowledgeManager.GetType().Name}");
            Console.WriteLine();

            // Test 1: Upload a document
            Console.WriteLine("📄 Test 1: Uploading test document...");
            var testDocPath =
                "/home/wayne/repos/ChatComplete/KnowledgeEngine/Docs/System_Inventory_Specification.md";
            var collectionName = "test-system-inventory";

            try
            {
                await knowledgeManager.SaveToMemoryAsync(testDocPath, collectionName);
                Console.WriteLine(
                    $"✅ Document uploaded successfully to collection '{collectionName}'"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Document upload failed: {ex.Message}");
                Console.WriteLine($"   Full exception: {ex}");
                return;
            }

            Console.WriteLine();

            // Test 2: List available collections
            Console.WriteLine("📋 Test 2: Listing available collections...");
            try
            {
                var collections = await knowledgeManager.GetAvailableCollectionsAsync();
                Console.WriteLine($"✅ Found {collections.Count} collections:");
                foreach (var collection in collections)
                {
                    Console.WriteLine($"   - {collection}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to list collections: {ex.Message}");
            }

            Console.WriteLine();

            // Test 3: Search the uploaded document
            Console.WriteLine("🔍 Test 3: Searching uploaded document...");
            var testQueries = new[]
            {
                "system inventory architecture",
                "database design MySQL",
                "React frontend components",
                "implementation approach",
            };

            foreach (var query in testQueries)
            {
                Console.WriteLine($"\n🔍 Searching for: '{query}'");
                try
                {
                    var results = await knowledgeManager.SearchAsync(
                        collectionName,
                        query,
                        limit: 3,
                        minRelevanceScore: 0.1
                    );

                    Console.WriteLine($"📊 Results: {results.Count} found");
                    for (int i = 0; i < results.Count && i < 2; i++)
                    {
                        var result = results[i];
                        Console.WriteLine(
                            $"   [{i + 1}] Score: {result.Score:F3} | Text: {result.Text.Substring(0, Math.Min(80, result.Text.Length))}..."
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Search failed: {ex.Message}");
                }
            }

            Console.WriteLine("\n🎉 Test completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed with exception: {ex.Message}");
            Console.WriteLine($"Full exception: {ex}");
        }
    }
}
