// Test script to verify embedding provider configuration switching
using System;
using System.IO;
using ChatCompletion.Config;
using Microsoft.Extensions.Configuration;
using KnowledgeEngine.Logging;
using Microsoft.Extensions.Logging;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🧪 Testing Enhanced Embedding Provider Configuration...\n");
        
        // Initialize logging
        LoggerProvider.Initialize(LogLevel.Information);
        
        try
        {
            // Load configuration from appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath("/home/wayne/repos/ChatComplete/Knowledge.Api")
                .AddJsonFile("appsettings.json")
                .Build();
                
            var settings = config.GetSection("ChatCompleteSettings").Get<ChatCompleteSettings>();
            if (settings == null)
            {
                Console.WriteLine("❌ Failed to load ChatCompleteSettings");
                return;
            }
            
            Console.WriteLine("📋 Current Configuration:");
            Console.WriteLine($"   Active Provider: {settings.EmbeddingProviders.ActiveProvider}");
            Console.WriteLine();
            
            // Test 1: Get active provider configuration
            Console.WriteLine("🔧 Test 1: Active Provider Configuration");
            try
            {
                var activeProvider = settings.EmbeddingProviders.GetActiveProvider();
                Console.WriteLine($"✅ Active Provider: {settings.EmbeddingProviders.ActiveProvider}");
                Console.WriteLine($"   Model: {activeProvider.ModelName}");
                Console.WriteLine($"   Dimensions: {activeProvider.Dimensions}");
                Console.WriteLine($"   Min Relevance Score: {activeProvider.MinRelevanceScore}");
                Console.WriteLine($"   Max Tokens: {activeProvider.MaxTokens}");
                Console.WriteLine($"   Description: {activeProvider.Description}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to get active provider: {ex.Message}");
            }
            Console.WriteLine();
            
            // Test 2: Test all provider configurations
            Console.WriteLine("🔧 Test 2: All Provider Configurations");
            var providers = new[] { "OpenAI", "Ollama", "Alternative" };
            
            foreach (var providerName in providers)
            {
                try
                {
                    var providerConfig = settings.EmbeddingProviders.GetProvider(providerName);
                    Console.WriteLine($"✅ {providerName} Provider:");
                    Console.WriteLine($"   Model: {providerConfig.ModelName}");
                    Console.WriteLine($"   Dimensions: {providerConfig.Dimensions}");
                    Console.WriteLine($"   Min Relevance Score: {providerConfig.MinRelevanceScore}");
                    Console.WriteLine($"   Max Tokens: {providerConfig.MaxTokens}");
                    Console.WriteLine($"   Description: {providerConfig.Description}");
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to get {providerName} provider: {ex.Message}");
                }
            }
            
            // Test 3: Test dimension validation
            Console.WriteLine("🔧 Test 3: Dimension Validation");
            var testCases = new[]
            {
                ("test-collection-openai", 1536, "OpenAI compatible"),
                ("test-collection-ollama", 768, "Ollama compatible"), 
                ("test-collection-alternative", 384, "Alternative compatible"),
                ("test-collection-mismatch", 999, "Invalid dimensions")
            };
            
            foreach (var (collectionName, dimensions, description) in testCases)
            {
                try
                {
                    // Simulate dimension validation
                    var activeProvider = settings.EmbeddingProviders.GetActiveProvider();
                    if (activeProvider.Dimensions == dimensions)
                    {
                        Console.WriteLine($"✅ {description}: {collectionName} ({dimensions}D) - COMPATIBLE");
                    }
                    else
                    {
                        Console.WriteLine($"❌ {description}: {collectionName} ({dimensions}D) - INCOMPATIBLE (expected {activeProvider.Dimensions}D)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Validation error for {collectionName}: {ex.Message}");
                }
            }
            Console.WriteLine();
            
            // Test 4: Test backward compatibility properties
            Console.WriteLine("🔧 Test 4: Backward Compatibility");
            try
            {
                Console.WriteLine($"✅ EmbeddingProvider: {settings.EmbeddingProvider}");
                Console.WriteLine($"✅ TextEmbeddingModelName: {settings.TextEmbeddingModelName}");
                Console.WriteLine($"✅ OllamaEmbeddingModel: {settings.OllamaEmbeddingModel}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Backward compatibility test failed: {ex.Message}");
            }
            Console.WriteLine();
            
            // Test 5: Simulate provider switching (just configuration test)
            Console.WriteLine("🔧 Test 5: Provider Switching Simulation");
            var originalProvider = settings.EmbeddingProviders.ActiveProvider;
            try
            {
                foreach (var newProvider in new[] { "OpenAI", "Ollama", "Alternative" })
                {
                    settings.EmbeddingProviders.ActiveProvider = newProvider;
                    var config = settings.EmbeddingProviders.GetActiveProvider();
                    Console.WriteLine($"✅ Switched to {newProvider}: {config.ModelName} ({config.Dimensions}D, threshold: {config.MinRelevanceScore})");
                }
                
                // Restore original
                settings.EmbeddingProviders.ActiveProvider = originalProvider;
                Console.WriteLine($"✅ Restored to original provider: {originalProvider}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Provider switching failed: {ex.Message}");
            }
            
            Console.WriteLine("\n🎉 Enhanced Embedding Provider Configuration Test Completed!");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed with exception: {ex.Message}");
            Console.WriteLine($"Full exception: {ex}");
        }
    }
}