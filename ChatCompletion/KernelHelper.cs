using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;


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
    
}