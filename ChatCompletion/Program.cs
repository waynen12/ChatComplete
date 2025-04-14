using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.SemanticKernel.Memory;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.KernelMemory;
using MongoDB.Driver;
using ChatCompletion;
using MongoDB.Driver.Core.Authentication;


#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

public class Program
{
    static string TextEmbeddingModelName = "text-embedding-ada-002";
    static string MongoDBAtlasConnectionString; 

    static MemoryBuilder memoryBuilder;
    static string DatabaseName = "EmbeddingTestCluster0";
    static string CollectionName = "tdi_knowledge";
    static string SearchIndexName = "default";
   

    public static async Task Main(string[] args)
    {

        var memory = KernelHelper.GetMongoDBMemoryStore(DatabaseName, SearchIndexName, TextEmbeddingModelName);
        Console.WriteLine(@"Press i to import or c to chat");
        var userInput = Console.ReadLine()?.ToLower();
        if (userInput == "i")
        {
            var knowledgeManager = new KnowledgeManager(memory);     

            await knowledgeManager.SaveKnowledgeDocumentsToMemory(
            "/home/wayne/repos/Semantic Kernel/ChatComplete/ChatCompletion/Docs/General_Telephony_Dashboard.md",
            "telephony_dashboard",
            CollectionName);

            await knowledgeManager.SaveKnowledgeDocumentsToMemory(
                "/home/wayne/repos/Semantic Kernel/ChatComplete/ChatCompletion/Docs/Telephony_Reports.md",
                "telephony_reports",
                CollectionName);

            await knowledgeManager.SaveKnowledgeDocumentsToMemory(
                "/home/wayne/repos/Semantic Kernel/ChatComplete/ChatCompletion/Docs/Telephony and Licence - Knowledge.md",
                "licence_dashboard",
                CollectionName);
        }
        else if (userInput== "c")
        {
            var prompt = $"You are a helpful assistant for users of a telecommunications analytics dashboard. Always base your answers on the context provided and keep answers clear and accurate.";
            var chatComplete = new ChatComplete(memory, prompt);
            await chatComplete.KnowledgeChatWithHistory(CollectionName);
        }            

    }
}

    