using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace KnowledgeManager.Tests.KnowledgeEngine;

/// <summary>
/// Tests to verify UUID generation compatibility between .NET and Python implementations.
/// Both systems must generate identical UUIDs for the same string keys.
/// </summary>
public class UuidCompatibilityTests
{
    private readonly ITestOutputHelper _output;

    public UuidCompatibilityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("Heliograph_Test_Document-p0000", "bc2e32a1-2825-12bf-bd43-c8291cf578e5")]
    [InlineData("Heliograph_Test_Document-p0001", "3c0e3ffd-3489-b1fb-e2ed-d6ccd4e5d94d")]
    [InlineData("test-doc-p0000", "cbf89f6e-ec4a-2c0b-adbc-61c7725b7adb")]
    public void CreateDeterministicGuid_ShouldMatchPythonImplementation(string input, string expectedUuid)
    {
        // Arrange & Act
        var actualGuid = CreateDeterministicGuid(input);
        var expectedGuid = Guid.Parse(expectedUuid);

        // Assert
        Assert.Equal(expectedGuid, actualGuid);
        
        _output.WriteLine($"✅ Key: '{input}' -> UUID: {actualGuid:D}");
        _output.WriteLine($"   Expected: {expectedGuid:D}");
        _output.WriteLine($"   Match: {actualGuid == expectedGuid}");
    }

    [Fact]
    public void CreateDeterministicGuid_ShouldBeConsistent()
    {
        // Arrange
        const string testKey = "consistent-test-key";
        
        // Act - Generate UUID multiple times
        var uuid1 = CreateDeterministicGuid(testKey);
        var uuid2 = CreateDeterministicGuid(testKey);
        var uuid3 = CreateDeterministicGuid(testKey);

        // Assert - All should be identical
        Assert.Equal(uuid1, uuid2);
        Assert.Equal(uuid2, uuid3);
        Assert.Equal(uuid1, uuid3);

        _output.WriteLine($"✅ Consistent UUID for '{testKey}': {uuid1:D}");
    }

    [Fact]
    public void CreateDeterministicGuid_ShouldProduceDifferentUuidsForDifferentKeys()
    {
        // Arrange
        var keys = new[]
        {
            "doc1-p0000",
            "doc1-p0001", 
            "doc2-p0000",
            "completely-different-key"
        };

        // Act
        var uuids = new Guid[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            uuids[i] = CreateDeterministicGuid(keys[i]);
            _output.WriteLine($"'{keys[i]}' -> {uuids[i]:D}");
        }

        // Assert - All UUIDs should be different
        for (int i = 0; i < uuids.Length; i++)
        {
            for (int j = i + 1; j < uuids.Length; j++)
            {
                Assert.NotEqual(uuids[i], uuids[j]);
            }
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("test-with-unicode-🚀")]
    [InlineData("very-long-key-with-lots-of-characters-that-should-still-work-fine")]
    public void CreateDeterministicGuid_ShouldHandleEdgeCases(string input)
    {
        // Act & Assert - Should not throw and should produce valid GUID
        var uuid = CreateDeterministicGuid(input);
        
        Assert.NotEqual(Guid.Empty, uuid);
        _output.WriteLine($"Edge case '{input}' -> {uuid:D}");
    }

    /// <summary>
    /// Creates a deterministic GUID from a string key using MD5 hash (matches Python implementation).
    /// This is the same method used in QdrantVectorStoreStrategy.
    /// </summary>
    private static Guid CreateDeterministicGuid(string input)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        
        // Convert to string and back to match Python uuid.UUID(bytes=...) behavior
        // Python's UUID constructor interprets bytes differently than .NET's Guid constructor
        var uuidString = $"{hash[0]:x2}{hash[1]:x2}{hash[2]:x2}{hash[3]:x2}-" +
                        $"{hash[4]:x2}{hash[5]:x2}-" +
                        $"{hash[6]:x2}{hash[7]:x2}-" +
                        $"{hash[8]:x2}{hash[9]:x2}-" +
                        $"{hash[10]:x2}{hash[11]:x2}{hash[12]:x2}{hash[13]:x2}{hash[14]:x2}{hash[15]:x2}";
        
        return Guid.Parse(uuidString);
    }
}