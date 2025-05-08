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

    static string DatabaseName = "EmbeddingTestCluster0";
    static string CollectionName = "Tracker";
    static string SearchIndexName = "default";
   

    public static async Task Main(string[] args)
    {

        var memory = KernelHelper.GetMongoDBMemoryStore(DatabaseName, SearchIndexName, TextEmbeddingModelName);
        Console.WriteLine(@"Press i to import or c to chat");
        var userInput = Console.ReadLine()?.ToLower();
        if (userInput == "i")
        {
            // DocxToDocumentConverter docxToDocumentConverter = new DocxToDocumentConverter();
            // var document = docxToDocumentConverter.Convert("/home/wayne/repos/Semantic Kernel/ChatComplete/ChatCompletion/Docs/LicenceDashboard_Test.docx", "licence_dashboard");
            // DocumentToTextConverter.Convert(document);

            var indexManager = await AtlasIndexManager.CreateAsync("Project 0", DatabaseName, DatabaseName, CollectionName);
             // *** Check if creation was successful ***
            if (indexManager == null)
            {
                 Console.WriteLine("Failed to initialize Atlas Index Manager. Aborting import.");
                 return; // or handle error appropriately
            }
            var knowledgeManager = new KnowledgeManager(memory, indexManager);     

            await knowledgeManager.SaveToMemoryAsync(
                "/home/wayne/repos/Semantic Kernel/ChatComplete/ChatCompletion/Docs/Deployment_Script_QA.md",
                CollectionName);

            await knowledgeManager.SaveToMemoryAsync(
            "/home/wayne/repos/Semantic Kernel/ChatComplete/ChatCompletion/Docs/Deployment_Script_TS.md",
            CollectionName);


            await knowledgeManager.SaveToMemoryAsync(
                "/home/wayne/repos/Semantic Kernel/ChatComplete/ChatCompletion/Docs/New_System_Installation_Guide.md",
                CollectionName);

            await knowledgeManager.CreateIndexAsync(CollectionName);

        }
        else if (userInput== "c")
        {
            var prompt = $"You are a helpful assistant for users on a web portal. Always base your answers on the context provided and keep answers clear and accurate. Never provide awnsers that are not based on the context. If you don't know the answer, say 'I don't know'. Do not make up answers. Do not engage in Roleplay'.";
            var chatComplete = new ChatComplete(memory, prompt);
            await chatComplete.KnowledgeChatWithHistory(CollectionName);
        }            

    }
}

    