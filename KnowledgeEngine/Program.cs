using ChatCompletion.Config;
using KnowledgeEngine.Logging;
using KnowledgeEngine.Persistence.IndexManagers;
using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.Extensions.Configuration;

using Serilog;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

public class Program
{
    static string CollectionName = "TrackerSpec";
    static ILogger _logger = null!;

    public static async Task Main(string[] args)
    {
        var test = Directory.GetCurrentDirectory();

        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "KnowledgeEngine"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var settings = config.Get<ChatCompleteSettings>();
        if (settings == null)
        {
            Console.WriteLine("Failed to load settings from appsettings.json");
            return;
        }
        SettingsProvider.Initialize(settings); // Register globally

        // Initialize the logger
        LoggerProvider.ConfigureLogger();
        _logger = LoggerProvider.Logger;

        _logger.Information("Starting ChatComplete application...");

        // TODO: Implement new memory store approach
        // var memory = KernelHelper.GetMongoDBMemoryStore(
        //     SettingsProvider.Settings.Atlas.ClusterName,
        //     SettingsProvider.Settings.Atlas.SearchIndexName,
        //     SettingsProvider.Settings.TextEmbeddingModelName
        // );
        Console.WriteLine(@"Press i to import or c to chat");
        var userInput = Console.ReadLine()?.ToLower();
        if (userInput == "i")
        {
            var indexManager = await AtlasIndexManager.CreateAsync(CollectionName);
            // *** Check if creation was successful ***
            if (indexManager == null)
            {
                Console.WriteLine("Failed to initialize Atlas Index Manager. Aborting import.");
                return; // or handle error appropriately
            }
            // For now, create a simple KnowledgeManager with minimal dependencies
            var mongoConn = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
                ?? throw new InvalidOperationException("MONGODB_CONNECTION_STRING missing");
            var db = new MongoDB.Driver.MongoClient(mongoConn).GetDatabase(SettingsProvider.Settings.Atlas.ClusterName);
            
            // Create MongoDB vector store strategy
            var vectorStoreStrategy = new MongoVectorStoreStrategy(db, SettingsProvider.Settings.Atlas);
            
            // Create a placeholder embedding service (will be implemented later)
            Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>? embeddingService = null;
            
            var knowledgeManager = new KnowledgeEngine.KnowledgeManager(vectorStoreStrategy, embeddingService!, indexManager);

            await knowledgeManager.SaveToMemoryAsync(
                Path.Combine(
                    SettingsProvider.Settings.FilePath,
                    "System_Inventory_Specification.md"
                ),
                CollectionName
            );
        }
    }
}
