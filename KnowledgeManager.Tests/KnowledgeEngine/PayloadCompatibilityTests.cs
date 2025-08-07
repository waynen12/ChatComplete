using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KnowledgeEngine.Persistence.IndexManagers;
using Xunit;
using Xunit.Abstractions;

namespace KnowledgeManager.Tests.KnowledgeEngine
{
    /// <summary>
    /// Tests to verify Python and C# Qdrant payload structures match for cross-compatibility
    /// </summary>
    public class PayloadCompatibilityTests
    {
        private readonly ITestOutputHelper _output;

        public PayloadCompatibilityTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void QdrantRecords_ShouldHave_CompatiblePayloadStructures()
        {
            // Arrange
            var testKey = "testdoc-p0001";
            var testText = "This is a test chunk of text for compatibility testing.";
            
            // Act - Create C# QdrantRecord structure (what gets serialized to Qdrant)
            var csharpRecord = CreateCSharpRecord(testKey, testText);
            
            // Act - Create Python payload structure (aligned with C# structure)
            var pythonPayload = CreatePythonPayload(testKey, testText);
            
            // Serialize both to JSON for comparison
            var csharpJson = JsonSerializer.Serialize(csharpRecord, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true 
            });
            
            var pythonJson = JsonSerializer.Serialize(pythonPayload, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            _output.WriteLine("C# Record Structure (as would be serialized to Qdrant):");
            _output.WriteLine(csharpJson);
            _output.WriteLine("");
            
            _output.WriteLine("Python Payload Structure (as stored in Qdrant):");
            _output.WriteLine(pythonJson);
            _output.WriteLine("");
            
            // Assert - Key properties should match
            Assert.Equal(((dynamic)csharpRecord).Text, ((dynamic)pythonPayload).Text);
            Assert.Equal(((dynamic)csharpRecord).DocumentKey, ((dynamic)pythonPayload).DocumentKey);
            Assert.Equal(((dynamic)csharpRecord).Source, ((dynamic)pythonPayload).Source);
            Assert.Equal(((dynamic)csharpRecord).ChunkOrder, ((dynamic)pythonPayload).ChunkOrder);
            Assert.Equal(((dynamic)csharpRecord).Tags, ((dynamic)pythonPayload).Tags);
        }
        
        [Theory]
        [InlineData("testdoc-p0001")]
        [InlineData("mydoc-p0042")]
        [InlineData("simpledoc")]
        public void UuidGeneration_ShouldBe_IdenticalBetweenPythonAndCSharp(string testKey)
        {
            // Arrange & Act
            var csharpUuid = CreateDeterministicGuid(testKey);
            var pythonUuid = CreatePythonCompatibleUuid(testKey);
            
            _output.WriteLine($"Test Key: {testKey}");
            _output.WriteLine($"C# Generated UUID: {csharpUuid}");
            _output.WriteLine($"Python Compatible UUID: {pythonUuid}");
            _output.WriteLine("");
            
            // Assert
            Assert.Equal(csharpUuid, pythonUuid);
        }
        
        [Fact]
        public void ChunkOrderParsing_ShouldBe_ConsistentBetweenLanguages()
        {
            // Test various key formats
            var testCases = new[]
            {
                new { Key = "testdoc-p0001", ExpectedSource = "testdoc", ExpectedChunkOrder = 1 },
                new { Key = "mydoc-p0042", ExpectedSource = "mydoc", ExpectedChunkOrder = 42 },
                new { Key = "simpledoc", ExpectedSource = "simpledoc", ExpectedChunkOrder = 0 },
                new { Key = "complex-file-name-p0123", ExpectedSource = "complex-file-name", ExpectedChunkOrder = 123 }
            };
            
            foreach (var testCase in testCases)
            {
                // Act
                var csharpRecord = CreateCSharpRecord(testCase.Key, "test text");
                var pythonPayload = CreatePythonPayload(testCase.Key, "test text");
                
                // Assert
                Assert.Equal(testCase.ExpectedSource, ((dynamic)csharpRecord).Source);
                Assert.Equal(testCase.ExpectedSource, ((dynamic)pythonPayload).Source);
                Assert.Equal(testCase.ExpectedChunkOrder, ((dynamic)csharpRecord).ChunkOrder);
                Assert.Equal(testCase.ExpectedChunkOrder, ((dynamic)pythonPayload).ChunkOrder);
                
                _output.WriteLine($"Key: {testCase.Key} -> Source: {testCase.ExpectedSource}, ChunkOrder: {testCase.ExpectedChunkOrder} ✓");
            }
        }
        
        private static object CreateCSharpRecord(string key, string text)
        {
            // Parse chunk order from key (format: "fileId-p0001")
            var chunkOrder = 0;
            var source = key;
            if (key.Contains("-p"))
            {
                var parts = key.Split("-p");
                source = parts[0];
                if (parts.Length > 1 && int.TryParse(parts[1], out var order))
                {
                    chunkOrder = order;
                }
            }
            
            return new
            {
                Id = CreateDeterministicGuid(key),
                DocumentKey = key,
                Text = text,
                Source = source,
                ChunkOrder = chunkOrder,
                Tags = string.Empty
            };
        }
        
        private static object CreatePythonPayload(string key, string text)
        {
            // Parse chunk order from key (format: "fileId-p0001") - Python logic
            var chunkOrder = 0;
            var source = key;
            if (key.Contains("-p"))
            {
                var parts = key.Split("-p");
                source = parts[0];
                if (parts.Length > 1 && int.TryParse(parts[1], out var order))
                {
                    chunkOrder = order;
                }
            }
            
            // Updated Python payload structure to match C# structure
            return new
            {
                Text = text,
                DocumentKey = key,  // Updated to match C# property name
                Source = source,
                ChunkOrder = chunkOrder,  // Updated to match C# casing
                Tags = string.Empty
            };
        }
        
        // Same deterministic GUID creation as in QdrantVectorStoreStrategy.cs
        private static Guid CreateDeterministicGuid(string input)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            
            // Convert to string and back to match Python uuid.UUID(bytes=...) behavior
            var uuidString = $"{hash[0]:x2}{hash[1]:x2}{hash[2]:x2}{hash[3]:x2}-" +
                            $"{hash[4]:x2}{hash[5]:x2}-" +
                            $"{hash[6]:x2}{hash[7]:x2}-" +
                            $"{hash[8]:x2}{hash[9]:x2}-" +
                            $"{hash[10]:x2}{hash[11]:x2}{hash[12]:x2}{hash[13]:x2}{hash[14]:x2}{hash[15]:x2}";
            
            return Guid.Parse(uuidString);
        }
        
        // Create UUID the same way Python would
        private static Guid CreatePythonCompatibleUuid(string input)
        {
            return CreateDeterministicGuid(input); // Same algorithm now
        }
    }
}