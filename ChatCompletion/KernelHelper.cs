using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using ChatCompletion.Config;


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
        builder.AddOpenAIChatCompletion(SettingsProvider.Settings.OpenAIModel, openAiApiKey);
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
        builder.AddOpenAIChatCompletion(SettingsProvider.Settings.OpenAIModel, openAiApiKey);
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
        builder.AddOpenAIChatCompletion(SettingsProvider.Settings.OpenAIModel, openAiApiKey);
        return builder;
    }


    public static ISemanticTextMemory GetMongoDBMemoryStore(string clusterName, string searchIndexName, string textEmbeddingModelName)
    {
        var mongoDBAtlasConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING") 
            ?? throw new InvalidOperationException("The MongoDB Atlas connection string is not set in the environment variables.");

        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(openAiApiKey))
        {
            LoggerProvider.Logger.Error("The OpenAI API key is not set in the environment variables.");
            throw new InvalidOperationException("The OpenAI API key is not set in the environment variables.");
        }

        var memoryBuilder= new MemoryBuilder();
        memoryBuilder.WithOpenAITextEmbeddingGeneration(
            textEmbeddingModelName,
            openAiApiKey);

        var mongoDBMemoryStore = new MongoDBMemoryStore(
            mongoDBAtlasConnectionString,
            clusterName,
            searchIndexName);
        memoryBuilder.WithMemoryStore(mongoDBMemoryStore);
        var memory = memoryBuilder.Build();

        return memory;
    }
    
}