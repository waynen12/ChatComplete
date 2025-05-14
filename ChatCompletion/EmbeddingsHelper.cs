using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ConnectingApps.Refit.OpenAI;
using ConnectingApps.Refit.OpenAI.Embeddings;
using ConnectingApps.Refit.OpenAI.Embeddings.Request;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Refit;

public static class EmbeddingsHelper
{
    public static async Task<double[]> GetEmbeddings(string text)
    {
        IKernelBuilder builder;
        builder = Kernel.CreateBuilder();
        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(openAiApiKey))
        {
            
            throw new InvalidOperationException("The OpenAI API key is not set in the environment variables.");
        }
        var completionApi = RestService.For<IEmbedding>(new HttpClient {
            BaseAddress = new Uri("https://api.openai.com/"),
        }, OpenAiRefitSettings.RefitSettings);

        var response = await completionApi.GetEmbeddingAsync(new EmbeddingRequest {
            Input = text,
            Model = "text-embedding-3-small",
        }, $"Bearer {openAiApiKey}");
        Console.WriteLine($"Vector length {response.Content!.Data.First().Embedding.Length}");
        Console.WriteLine($"Vector {string.Join(",", response.Content.Data.First().Embedding.Take(100))}");

        return response.Content!.Data.First().Embedding;
    }
        
}