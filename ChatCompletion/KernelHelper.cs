using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using MongoDB.Driver;
using MongoDB.Driver.Core.Authentication;
using HandlebarsDotNet.Helpers.BlockHelpers;


#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050


public static class KernelHelper
{
    public static Kernel GetKernel()
    {
        IKernelBuilder builder;
        builder = Kernel.CreateBuilder();
        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(openAiApiKey))
        {
            throw new InvalidOperationException("The OpenAI API key is not set in the environment variables.");
        }
        builder.AddOpenAIChatCompletion("gpt-4o", openAiApiKey);
        Kernel kernel = builder.Build();
        return kernel;
    }

    public static Kernel GetPluginKernelFromType<T>(string typeName)
    {
        IKernelBuilder builder;
        builder = Kernel.CreateBuilder();
        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(openAiApiKey))
        {
            throw new InvalidOperationException("The OpenAI API key is not set in the environment variables.");
        }
        builder.AddOpenAIChatCompletion("gpt-4o", openAiApiKey);
        builder.Services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Trace));
        builder.Plugins.AddFromType<T>(typeName);
        Kernel kernel = builder.Build();
        return kernel;
    }

    public static IKernelBuilder GetBuilder()
    {
        IKernelBuilder builder;
        builder = Kernel.CreateBuilder();
        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(openAiApiKey))
        {
            throw new InvalidOperationException("The OpenAI API key is not set in the environment variables.");
        }
        builder.AddOpenAIChatCompletion("gpt-4o", openAiApiKey);
        return builder;
    }


    public static ISemanticTextMemory GetMongoDBMemoryStore(string databaseName, string searchIndexName, string textEmbeddingModelName)
    {
        var mongoDBAtlasConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING") 
            ?? throw new InvalidOperationException("The MongoDB Atlas connection string is not set in the environment variables.");

        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(openAiApiKey))
        {
            throw new InvalidOperationException("The OpenAI API key is not set in the environment variables.");
        }

        var memoryBuilder= new MemoryBuilder();
        memoryBuilder.WithOpenAITextEmbeddingGeneration(
            textEmbeddingModelName,
            openAiApiKey);

        var mongoDBMemoryStore = new MongoDBMemoryStore(
            mongoDBAtlasConnectionString,
            databaseName,
            searchIndexName);
        memoryBuilder.WithMemoryStore(mongoDBMemoryStore);
        var memory = memoryBuilder.Build();

        return memory;
    }
    
}