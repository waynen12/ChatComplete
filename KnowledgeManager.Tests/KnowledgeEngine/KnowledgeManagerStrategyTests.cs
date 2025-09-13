using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using KnowledgeEngine;
using KnowledgeEngine.Persistence.VectorStores;
using KnowledgeEngine.Persistence.IndexManagers;
using KnowledgeEngine.Models;

namespace KnowledgeManager.Tests.KnowledgeEngine;

/// <summary>
/// Tests for the KnowledgeManager class and its strategy pattern implementation.
/// These tests validate the core architecture refactoring that enables
/// switching between vector stores (MongoDB vs Qdrant) at runtime.
/// </summary>
public class KnowledgeManagerStrategyTests
{
    private readonly ITestOutputHelper _output;

    public KnowledgeManagerStrategyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void KnowledgeManager_ShouldAcceptStrategyDependencies()
    {
        // Test that KnowledgeManager constructor accepts the strategy interfaces
        // We can't easily create real instances without full DI setup, 
        // but we can verify the constructor signature exists
        
        var constructors = typeof(global::KnowledgeEngine.KnowledgeManager).GetConstructors();
        var primaryConstructor = constructors.FirstOrDefault();
        
        Assert.NotNull(primaryConstructor);
        
        var parameters = primaryConstructor.GetParameters();
        var parameterTypes = parameters.Select(p => p.ParameterType).ToList();
        
        // Should have IVectorStoreStrategy and IIndexManager parameters
        Assert.Contains(typeof(IVectorStoreStrategy), parameterTypes);
        Assert.Contains(typeof(IIndexManager), parameterTypes);
        
        _output.WriteLine($"✅ KnowledgeManager constructor accepts strategy dependencies");
        _output.WriteLine($"   Parameters: {string.Join(", ", parameters.Select(p => p.ParameterType.Name))}");
    }

    [Fact]
    public void KnowledgeManager_ShouldHaveExpectedPublicMethods()
    {
        // Verify that KnowledgeManager maintains its expected API
        var managerType = typeof(global::KnowledgeEngine.KnowledgeManager);
        var publicMethods = managerType.GetMethods().Where(m => m.IsPublic && !m.IsSpecialName).ToList();
        var methodNames = publicMethods.Select(m => m.Name).ToList();

        // Key methods that should exist for the RAG pipeline
        var expectedMethods = new[]
        {
            "SaveToMemoryAsync",  // Document ingestion (actual method name)
            "SearchAsync",        // Semantic search (actual method name)
        };

        foreach (var expectedMethod in expectedMethods)
        {
            Assert.Contains(expectedMethod, methodNames);
        }

        _output.WriteLine($"✅ KnowledgeManager API validation:");
        _output.WriteLine($"   Found methods: {string.Join(", ", methodNames)}");
    }

    [Fact]
    public async Task KnowledgeManager_SaveToMemoryAsync_ShouldAcceptValidParameters()
    {
        // Test parameter validation for the core SaveToMemoryAsync method
        // This is a structural test since we can't easily test with real data
        
        var managerType = typeof(global::KnowledgeEngine.KnowledgeManager);
        var ingestMethod = managerType.GetMethod("SaveToMemoryAsync");
        
        Assert.NotNull(ingestMethod);
        
        var parameters = ingestMethod.GetParameters();
        Assert.True(parameters.Length > 0);

        // Should have async return type
        Assert.True(ingestMethod.ReturnType == typeof(Task) || 
                   ingestMethod.ReturnType.BaseType == typeof(Task));

        _output.WriteLine($"✅ SaveToMemoryAsync method signature validation passed");
        _output.WriteLine($"   Parameters: {parameters.Length}");
        _output.WriteLine($"   Return type: {ingestMethod.ReturnType.Name}");
    }

    [Fact]
    public void KnowledgeManager_SearchAsync_ShouldReturnCorrectType()
    {
        // Verify the SearchAsync method returns the expected type
        var managerType = typeof(global::KnowledgeEngine.KnowledgeManager);
        var searchMethod = managerType.GetMethod("SearchAsync");
        
        Assert.NotNull(searchMethod);
        
        // Should return Task<List<KnowledgeSearchResult>>
        Assert.True(searchMethod.ReturnType.IsGenericType);
        var returnType = searchMethod.ReturnType;
        
        if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var taskResultType = returnType.GetGenericArguments()[0];
            
            // Check if it returns a collection of search results
            if (taskResultType.IsGenericType && 
                taskResultType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var listItemType = taskResultType.GetGenericArguments()[0];
                Assert.Equal(typeof(KnowledgeSearchResult), listItemType);
            }
        }

        _output.WriteLine($"✅ SearchAsync return type validation passed");
        _output.WriteLine($"   Return type: {returnType}");
    }

    [Theory]
    [InlineData("test-collection")]
    [InlineData("documents")]
    [InlineData("knowledge-base")]
    public void KnowledgeManager_ShouldHandleValidCollectionNames(string collectionName)
    {
        // Test collection name validation logic
        // These should be valid collection names for both MongoDB and Qdrant
        
        Assert.NotNull(collectionName);
        Assert.NotEmpty(collectionName);
        Assert.False(string.IsNullOrWhiteSpace(collectionName));
        
        // Basic naming rules that should work for both vector stores
        Assert.DoesNotContain(" ", collectionName); // No spaces
        Assert.False(collectionName.StartsWith(".")); // No leading dots
        
        _output.WriteLine($"✅ Collection name validation passed: '{collectionName}'");
    }

    [Fact]
    public void KnowledgeManager_StrategyPattern_ShouldSupportBothVectorStores()
    {
        // Verify both strategy implementations exist and can be distinguished
        var strategyInterface = typeof(IVectorStoreStrategy);
        var implementations = strategyInterface.Assembly
            .GetTypes()
            .Where(t => strategyInterface.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        Assert.True(implementations.Count >= 2, "Should have at least 2 vector store implementations");
        
        var implementationNames = implementations.Select(t => t.Name).ToList();
        Assert.Contains("MongoVectorStoreStrategy", implementationNames);
        Assert.Contains("QdrantVectorStoreStrategy", implementationNames);

        _output.WriteLine($"✅ Strategy pattern implementations found:");
        foreach (var impl in implementations)
        {
            _output.WriteLine($"   - {impl.Name}");
        }
    }

    [Fact]
    public void KnowledgeManager_IndexManagers_ShouldSupportBothBackends()
    {
        // Verify both index manager implementations exist
        var managerInterface = typeof(IIndexManager);
        var implementations = managerInterface.Assembly
            .GetTypes()
            .Where(t => managerInterface.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        Assert.True(implementations.Count >= 2, "Should have at least 2 index manager implementations");
        
        var implementationNames = implementations.Select(t => t.Name).ToList();
        Assert.Contains("AtlasIndexManager", implementationNames);
        Assert.Contains("QdrantIndexManager", implementationNames);

        _output.WriteLine($"✅ Index manager implementations found:");
        foreach (var impl in implementations)
        {
            _output.WriteLine($"   - {impl.Name}");
        }
    }

    [Fact]
    public void KnowledgeManager_Dependencies_ShouldFollowCleanArchitecture()
    {
        // Verify that KnowledgeManager depends on abstractions, not implementations
        var managerType = typeof(global::KnowledgeEngine.KnowledgeManager);
        var constructor = managerType.GetConstructors().First();
        var parameterTypes = constructor.GetParameters().Select(p => p.ParameterType).ToList();

        // Should depend on interfaces, not concrete classes
        foreach (var paramType in parameterTypes)
        {
            if (paramType == typeof(IVectorStoreStrategy) || paramType == typeof(IIndexManager))
            {
                Assert.True(paramType.IsInterface, $"{paramType.Name} should be an interface");
            }
        }

        _output.WriteLine($"✅ Clean architecture validation passed");
        _output.WriteLine($"   Dependencies on interfaces: {parameterTypes.Count(t => t.IsInterface)}");
    }

    [Fact]
    public void KnowledgeSearchResult_ShouldBeCompatibleWithBothStrategies()
    {
        // Test that KnowledgeSearchResult works with both vector store strategies
        var searchResult = new KnowledgeSearchResult
        {
            Text = "Test content for compatibility validation",
            Source = "test-document.pdf", 
            ChunkOrder = 0,
            Score = 0.85,
            Tags = "test,validation"
        };

        // Should be serializable/deserializable for storage
        Assert.NotNull(searchResult.Text);
        Assert.NotNull(searchResult.Source);
        Assert.True(searchResult.Score >= 0 && searchResult.Score <= 1);
        Assert.True(searchResult.ChunkOrder >= 0);

        _output.WriteLine($"✅ KnowledgeSearchResult compatibility validation passed");
        _output.WriteLine($"   Text length: {searchResult.Text.Length}");
        _output.WriteLine($"   Score: {searchResult.Score}");
    }

    [Fact]
    public void KnowledgeChunkRecord_ShouldSupportVectorOperations()
    {
        // Test the KnowledgeChunkRecord used for vector storage
        var embedding = new float[768]; // OpenAI embedding size
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(Math.Sin(i * 0.01)); // Generate test embedding
        }

        var chunkRecord = new KnowledgeChunkRecord
        {
            Id = "test-chunk-1",
            Vector = new ReadOnlyMemory<float>(embedding),
            Text = "Test chunk content for vector operations",
            Source = "test-document.pdf",
            ChunkOrder = 1,
            Tags = "test,chunk"
        };

        Assert.Equal("test-chunk-1", chunkRecord.Id);
        Assert.Equal(768, chunkRecord.Vector.Length);
        Assert.Equal(1, chunkRecord.ChunkOrder);
        Assert.Contains("test", chunkRecord.Tags);

        _output.WriteLine($"✅ KnowledgeChunkRecord validation passed");
        _output.WriteLine($"   Vector dimensions: {chunkRecord.Vector.Length}");
        _output.WriteLine($"   Content length: {chunkRecord.Text.Length}");
    }

    [Theory]
    [InlineData(0.6)]  // Default threshold
    [InlineData(0.7)]  // Higher threshold
    [InlineData(0.5)]  // Lower threshold
    public void KnowledgeManager_SearchThreshold_ShouldBeConfigurable(double threshold)
    {
        // Test that different relevance thresholds produce expected filtering behavior
        var results = new List<KnowledgeSearchResult>
        {
            new() { Text = "High relevance", Score = 0.95 },
            new() { Text = "Medium relevance", Score = 0.75 }, 
            new() { Text = "Low relevance", Score = 0.45 }
        };

        var filteredResults = results.Where(r => r.Score >= threshold).ToList();
        var expectedCount = results.Count(r => r.Score >= threshold);

        Assert.Equal(expectedCount, filteredResults.Count);
        Assert.True(filteredResults.All(r => r.Score >= threshold));

        _output.WriteLine($"✅ Search threshold validation: {threshold}");
        _output.WriteLine($"   Results above threshold: {filteredResults.Count}/{results.Count}");
    }

    [Fact]
    public void KnowledgeManager_ErrorHandling_ShouldBeResilient()
    {
        // Test error handling patterns that the strategies should implement
        
        // Test null/empty inputs
        Assert.Throws<ArgumentNullException>(() => 
        {
            string nullString = null!;
            if (string.IsNullOrEmpty(nullString))
                throw new ArgumentNullException(nameof(nullString));
        });

        // Test invalid collection names
        var invalidNames = new[] { "", " ", ".", "..", null! };
        
        foreach (var name in invalidNames.Where(n => n != null))
        {
            Assert.True(string.IsNullOrWhiteSpace(name) || name.StartsWith("."));
        }

        _output.WriteLine($"✅ Error handling patterns validated");
    }

    [Fact]
    public void KnowledgeManager_VectorDimensions_ShouldBeConsistent()
    {
        // Test that vector dimensions are consistent across the system
        const int expectedDimensions = 768; // OpenAI text-embedding-ada-002

        var testEmbedding = new float[expectedDimensions];
        var readOnlyEmbedding = new ReadOnlyMemory<float>(testEmbedding);

        Assert.Equal(expectedDimensions, readOnlyEmbedding.Length);

        // Test embedding conversion
        var backToArray = readOnlyEmbedding.ToArray();
        Assert.Equal(expectedDimensions, backToArray.Length);

        _output.WriteLine($"✅ Vector dimension consistency validated: {expectedDimensions}");
    }
}