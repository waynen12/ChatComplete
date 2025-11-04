using System.Diagnostics.CodeAnalysis;
using ChatCompletion.Config;
using Knowledge.Contracts.Types;
using KnowledgeEngine.Logging;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.AI;                        
using Microsoft.SemanticKernel.Connectors.MongoDB;     
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using MongoDB.Driver;


#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050
/// <summary>
/// Provides helper methods for working with kernels.
/// </summary>
[Obsolete]
//"This class is now obsolete and will be removed in a future release. Please use the new KernelFactory class instead."
public static class KernelHelper
{
    [Experimental("SKEXP0070")]
    public static Kernel GetKernel(ChatCompleteSettings settings, AiProvider provider)
    {
        SettingsProvider.Initialize(settings);
        Kernel kernel;
        switch (provider)
        {
            case AiProvider.OpenAi:
                 default:
                kernel = GetOpenAiKernel();
            break;
            case AiProvider.Google:
                kernel = GetGoogleKernel();
                break;
            case AiProvider.Ollama:
                kernel =GetOllamaKernel();
                break;
            case AiProvider.Anthropic:
                kernel = GetAnthropicKernel();
                break;
        }
        return kernel;
    }

    public static Kernel GetOpenAiKernel()
    {
        IKernelBuilder builder;
        builder = Kernel.CreateBuilder();
        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(openAiApiKey))
        {
            throw new InvalidOperationException(
                "The OpenAI API key is not set in the environment variables."
            );
        }
        builder.AddOpenAIChatCompletion(SettingsProvider.Settings.OpenAIModel, openAiApiKey);
        var kernel = builder.Build();
        return kernel;
    }
    
    [Experimental("SKEXP0070")]
    public static Kernel GetGoogleKernel()
    {
        IKernelBuilder builder;
        builder = Kernel.CreateBuilder();
        var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrEmpty(geminiApiKey))
        {
            throw new InvalidOperationException(
                "The Gemini API key is not set in the environment variables."
            );
        }
        builder.AddGoogleAIGeminiChatCompletion(SettingsProvider.Settings.GoogleModel, geminiApiKey);
        Kernel kernel = builder.Build();
        return kernel;
    }
    
    [Experimental("SKEXP0070")]
    public static Kernel GetAnthropicKernel()
    {
        IKernelBuilder builder;
        builder = Kernel.CreateBuilder();
        var anthropicApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

        if (string.IsNullOrEmpty(anthropicApiKey))
        {
            throw new InvalidOperationException(
                "The Gemini API key is not set in the environment variables."
            );
        }
        builder.AddAnthropicChatCompletion(SettingsProvider.Settings.AnthropicModel, anthropicApiKey);
        Kernel kernel = builder.Build();
        return kernel;
    }
    
    [Experimental("SKEXP0070")]
    public static Kernel GetOllamaKernel()
    {
        IKernelBuilder builder;
        builder = Kernel.CreateBuilder();
        var ollamaApiKey = Environment.GetEnvironmentVariable("OLLAMA_API_KEY");
        var client = new HttpClient();
        var uri = new Uri(SettingsProvider.Settings.OllamaBaseUrl);
        builder.AddOpenAIChatCompletion(
            modelId : SettingsProvider.Settings.OllamaModel,               // "gemma3:12b"
            apiKey: ollamaApiKey,
            endpoint : uri,
            serviceId: null);           // "http://localhost:11434"


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
            throw new InvalidOperationException(
                "The OpenAI API key is not set in the environment variables."
            );
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
            throw new InvalidOperationException(
                "The OpenAI API key is not set in the environment variables."
            );
        }
        builder.AddOpenAIChatCompletion(SettingsProvider.Settings.OpenAIModel, openAiApiKey);
        return builder;
    }
    
    [Experimental("SKEXP0070")]
    public static IKernelBuilder GetGoogleBuilder()
    {
        IKernelBuilder builder;
        builder = Kernel.CreateBuilder();
        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(openAiApiKey))
        {
            throw new InvalidOperationException(
                "The OpenAI API key is not set in the environment variables."
            );
        }
        builder.AddGoogleAIGeminiChatCompletion(SettingsProvider.Settings.OpenAIModel, openAiApiKey);
        return builder;
    }

    public static ISemanticTextMemory GetMongoDBMemoryStore(
        string clusterName,
        string searchIndexName,          // ← still unused; keep for future
        string textEmbeddingModelName)
    {
        // TODO: Implement new vector store approach
        // For now, return a placeholder that will be replaced
        throw new NotImplementedException("MongoDB memory store needs to be reimplemented with new vector store APIs");
    }
}
