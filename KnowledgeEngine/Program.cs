using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ChatCompletion;
using ChatCompletion.Config;
using DocumentFormat.OpenXml.Wordprocessing;
using KnowledgeEngine.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using MongoDB.Driver;
using MongoDB.Driver.Core.Authentication;
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

        var memory = KernelHelper.GetMongoDBMemoryStore(
            SettingsProvider.Settings.Atlas.ClusterName,
            SettingsProvider.Settings.Atlas.SearchIndexName,
            SettingsProvider.Settings.TextEmbeddingModelName
        );
        Console.WriteLine(@"Press i to import or c to chat");
        var userInput = Console.ReadLine()?.ToLower();
        if (userInput == "i")
        {
            // DocxToDocumentConverter docxToDocumentConverter = new DocxToDocumentConverter();
            // var document = docxToDocumentConverter.Convert("/home/wayne/repos/Semantic Kernel/ChatComplete/ChatCompletion/Docs/LicenceDashboard_Test.docx", "licence_dashboard");
            // DocumentToTextConverter.Convert(document);

            var indexManager = await AtlasIndexManager.CreateAsync(CollectionName);
            // *** Check if creation was successful ***
            if (indexManager == null)
            {
                Console.WriteLine("Failed to initialize Atlas Index Manager. Aborting import.");
                return; // or handle error appropriately
            }
            var knowledgeManager = new KnowledgeManager(memory, indexManager);

            await knowledgeManager.SaveToMemoryAsync(
                Path.Combine(
                    SettingsProvider.Settings.FilePath,
                    "System_Inventory_Specification.md"
                ),
                CollectionName
            );

            // await knowledgeManager.SaveToMemoryAsync(
            // Path.Combine(SettingsProvider.Settings.FilePath,"Deployment_Script_TS.md"),
            // CollectionName);

            // await knowledgeManager.SaveToMemoryAsync(
            //     Path.Combine(SettingsProvider.Settings.FilePath, "New_System_Installation_Guide.md"),
            //     CollectionName);
        }
        else if (userInput == "c")
        {
            var prompt =
                $"You are a helpful assistant for users on a web portal. Always base your answers on the context provided and keep answers clear and accurate. Never provide awnsers that are not based on the context. If you don't know the answer, say 'I don't know'. Do not make up answers. Do not engage in Roleplay'.";
            var chatComplete = new ChatComplete(memory, prompt);
            await chatComplete.KnowledgeChatWithHistory(CollectionName);
        }
    }
}
