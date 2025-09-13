// Direct test script to debug Ollama embedding issues
// This bypasses the web UI and tests the core embedding system directly

using System;
using System.Threading.Tasks;
using ChatCompletion.Config;
using KnowledgeEngine;
using KnowledgeEngine.Extensions;
using KnowledgeEngine.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

public class EmbeddingTest
{
    public static async Task Main(string[] args)
    {
        // Setup logging
        LoggerProvider.Initialize(LogLevel.Information);
        
        // Build configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
            
        var settings = config.GetSection("ChatCompleteSettings").Get<ChatCompleteSettings>();
        if (settings == null)
        {
            Console.WriteLine("❌ Failed to load ChatCompleteSettings");
            return;
        }
        
        Console.WriteLine($"🔧 EmbeddingProvider: {settings.EmbeddingProvider}");
        Console.WriteLine($"🔧 OllamaEmbeddingModel: {settings.OllamaEmbeddingModel}");
        Console.WriteLine($"🔧 OllamaBaseUrl: {settings.OllamaBaseUrl}");
        
        // Setup DI container
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddSingleton(settings);
        services.AddHttpClient();
        
        // Add the same embedding service configuration as the main app
        if (settings.EmbeddingProvider?.ToLower() == "ollama")
        {
            services.AddHttpClient<Knowledge.Api.Services.OllamaEmbeddingService>();
            services.AddScoped<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(nameof(Knowledge.Api.Services.OllamaEmbeddingService));
                return new Knowledge.Api.Services.OllamaEmbeddingService(httpClient, settings.OllamaBaseUrl, settings.OllamaEmbeddingModel);
            });
        }
        else
        {
            Console.WriteLine("❌ This test only works with Ollama embedding provider");
            return;
        }
        
        var serviceProvider = services.BuildServiceProvider();
        var embeddingService = serviceProvider.GetRequiredService<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>();
        
        // Test 1: Generate embedding for simple text
        Console.WriteLine("\n🧪 Test 1: Generate embedding for simple text");
        try
        {
            var testText = "System Inventory Specification - This document describes the architecture and implementation approach for a system inventory management system.";
            Console.WriteLine($"📝 Test text: {testText}");
            
            var result = await embeddingService.GenerateAsync(new[] { testText });
            var embedding = result.First();
            
            Console.WriteLine($"✅ Embedding generated successfully!");
            Console.WriteLine($"📊 Vector dimensions: {embedding.Vector.Length}");
            Console.WriteLine($"📊 First 5 values: [{string.Join(", ", embedding.Vector.Take(5).Select(f => f.ToString("F3")))}]");
            Console.WriteLine($"📊 Service type: {embeddingService.GetType().Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test 1 failed: {ex.Message}");
            Console.WriteLine($"🔍 Exception details: {ex}");
            return;
        }
        
        // Test 2: Multiple texts
        Console.WriteLine("\n🧪 Test 2: Generate embeddings for multiple texts");
        try
        {
            var texts = new[]
            {
                "Introduction to system architecture",
                "Database design and implementation", 
                "Frontend React components"
            };
            
            foreach (var text in texts)
            {
                var result = await embeddingService.GenerateAsync(new[] { text });
                var embedding = result.First();
                Console.WriteLine($"✅ '{text}' → {embedding.Vector.Length} dimensions");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test 2 failed: {ex.Message}");
        }
        
        Console.WriteLine("\n🎉 Embedding tests completed!");
    }
}