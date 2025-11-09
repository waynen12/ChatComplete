using Knowledge.Mcp.Resources;
using Knowledge.Mcp.Resources.Models;
using Xunit;

namespace Knowledge.Mcp.Tests.Resources;

/// <summary>
/// Unit tests for ResourceUriParser.
/// Validates URI parsing, routing, and security validation.
/// </summary>
public class ResourceUriParserTests
{
    private readonly ResourceUriParser _parser;

    public ResourceUriParserTests()
    {
        _parser = new ResourceUriParser();
    }

    #region Valid Knowledge URIs

    [Fact]
    public void Parse_CollectionListUri_ReturnsCorrectType()
    {
        // Arrange
        var uri = "resource://knowledge/collections";

        // Act
        var result = _parser.Parse(uri);

        // Assert
        Assert.Equal(ResourceType.CollectionList, result.Type);
        Assert.Null(result.CollectionId);
        Assert.Null(result.DocumentId);
        Assert.Equal(uri, result.OriginalUri);
    }

    [Fact]
    public void Parse_DocumentListUri_ReturnsCorrectType()
    {
        // Arrange
        var uri = "resource://knowledge/docker-guides/documents";

        // Act
        var result = _parser.Parse(uri);

        // Assert
        Assert.Equal(ResourceType.DocumentList, result.Type);
        Assert.Equal("docker-guides", result.CollectionId);
        Assert.Null(result.DocumentId);
        Assert.Equal(uri, result.OriginalUri);
    }

    [Fact]
    public void Parse_DocumentUri_ReturnsCorrectType()
    {
        // Arrange
        var uri = "resource://knowledge/docker-guides/document/ssl-setup";

        // Act
        var result = _parser.Parse(uri);

        // Assert
        Assert.Equal(ResourceType.Document, result.Type);
        Assert.Equal("docker-guides", result.CollectionId);
        Assert.Equal("ssl-setup", result.DocumentId);
        Assert.Equal(uri, result.OriginalUri);
    }

    [Fact]
    public void Parse_CollectionStatsUri_ReturnsCorrectType()
    {
        // Arrange
        var uri = "resource://knowledge/docker-guides/stats";

        // Act
        var result = _parser.Parse(uri);

        // Assert
        Assert.Equal(ResourceType.CollectionStats, result.Type);
        Assert.Equal("docker-guides", result.CollectionId);
        Assert.Null(result.DocumentId);
        Assert.Equal(uri, result.OriginalUri);
    }

    [Theory]
    [InlineData("docker-guides")]
    [InlineData("Heliograph_Test_Document")]
    [InlineData("kubernetes-docs")]
    [InlineData("csharp-fundamentals")]
    public void Parse_DocumentListUri_WithVariousCollectionIds_ParsesCorrectly(string collectionId)
    {
        // Arrange
        var uri = $"resource://knowledge/{collectionId}/documents";

        // Act
        var result = _parser.Parse(uri);

        // Assert
        Assert.Equal(ResourceType.DocumentList, result.Type);
        Assert.Equal(collectionId, result.CollectionId);
    }

    [Theory]
    [InlineData("docker-guides", "ssl-setup")]
    [InlineData("Heliograph_Test_Document", "chapter1")]
    [InlineData("kubernetes-docs", "deployment-guide")]
    public void Parse_DocumentUri_WithVariousIds_ParsesCorrectly(string collectionId, string documentId)
    {
        // Arrange
        var uri = $"resource://knowledge/{collectionId}/document/{documentId}";

        // Act
        var result = _parser.Parse(uri);

        // Assert
        Assert.Equal(ResourceType.Document, result.Type);
        Assert.Equal(collectionId, result.CollectionId);
        Assert.Equal(documentId, result.DocumentId);
    }

    #endregion

    #region Valid System URIs

    [Fact]
    public void Parse_SystemHealthUri_ReturnsCorrectType()
    {
        // Arrange
        var uri = "resource://system/health";

        // Act
        var result = _parser.Parse(uri);

        // Assert
        Assert.Equal(ResourceType.SystemHealth, result.Type);
        Assert.Null(result.CollectionId);
        Assert.Null(result.DocumentId);
        Assert.Equal(uri, result.OriginalUri);
    }

    [Fact]
    public void Parse_SystemModelsUri_ReturnsCorrectType()
    {
        // Arrange
        var uri = "resource://system/models";

        // Act
        var result = _parser.Parse(uri);

        // Assert
        Assert.Equal(ResourceType.ModelList, result.Type);
        Assert.Null(result.CollectionId);
        Assert.Null(result.DocumentId);
        Assert.Equal(uri, result.OriginalUri);
    }

    #endregion

    #region Invalid URI Tests

    [Fact]
    public void Parse_NullUri_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _parser.Parse(null!));
        Assert.Contains("cannot be null or empty", ex.Message);
    }

    [Fact]
    public void Parse_EmptyUri_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _parser.Parse(""));
        Assert.Contains("cannot be null or empty", ex.Message);
    }

    [Fact]
    public void Parse_WhitespaceUri_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _parser.Parse("   "));
        Assert.Contains("cannot be null or empty", ex.Message);
    }

    [Theory]
    [InlineData("http://knowledge/collections")]
    [InlineData("https://system/health")]
    [InlineData("file://knowledge/collections")]
    [InlineData("ftp://system/models")]
    public void Parse_WrongScheme_ThrowsArgumentException(string invalidUri)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _parser.Parse(invalidUri));
        Assert.Contains("must start with 'resource://'", ex.Message);
    }

    [Fact]
    public void Parse_MissingPath_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _parser.Parse("resource://"));
        Assert.Contains("Path cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData("resource://unknown/collections")]
    [InlineData("resource://invalid/health")]
    [InlineData("resource://data/models")]
    public void Parse_UnknownDomain_ThrowsArgumentException(string invalidUri)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _parser.Parse(invalidUri));
        Assert.Contains("Unknown resource domain", ex.Message);
        Assert.Contains("Valid domains are 'knowledge' and 'system'", ex.Message);
    }

    [Theory]
    [InlineData("resource://knowledge/invalid-pattern")]
    [InlineData("resource://knowledge/docker-guides")]
    [InlineData("resource://knowledge/docker-guides/unknown-resource")]
    [InlineData("resource://knowledge/docker-guides/document/ssl-setup/extra")]
    public void Parse_UnknownKnowledgePattern_ThrowsArgumentException(string invalidUri)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _parser.Parse(invalidUri));
        Assert.Contains("Unknown knowledge resource pattern", ex.Message);
    }

    [Theory]
    [InlineData("resource://system/unknown")]
    [InlineData("resource://system/health/extra")]
    [InlineData("resource://system/models/details")]
    public void Parse_UnknownSystemPattern_ThrowsArgumentException(string invalidUri)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _parser.Parse(invalidUri));
        Assert.Contains("Unknown system resource pattern", ex.Message);
    }

    #endregion

    #region Collection ID Validation Tests

    [Theory]
    [InlineData("docker-guides")]
    [InlineData("Heliograph_Test_Document")]
    [InlineData("kubernetes-docs")]
    [InlineData("csharp.fundamentals")]
    [InlineData("docs_v2.0")]
    [InlineData("collection-name-with-many-hyphens")]
    public void IsValidCollectionId_ValidIds_ReturnsTrue(string collectionId)
    {
        // Act
        var result = ResourceUriParser.IsValidCollectionId(collectionId);

        // Assert
        Assert.True(result, $"Expected '{collectionId}' to be valid");
    }

    [Fact]
    public void IsValidCollectionId_Null_ReturnsFalse()
    {
        // Act
        var result = ResourceUriParser.IsValidCollectionId(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidCollectionId_Empty_ReturnsFalse()
    {
        // Act
        var result = ResourceUriParser.IsValidCollectionId("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidCollectionId_Whitespace_ReturnsFalse()
    {
        // Act
        var result = ResourceUriParser.IsValidCollectionId("   ");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("..\\windows\\system32")]
    [InlineData("collection/../../../secrets")]
    [InlineData("collection..bad")]  // Should be valid (.. not at boundary)
    public void IsValidCollectionId_PathTraversal_ReturnsFalse(string maliciousId)
    {
        // Act
        var result = ResourceUriParser.IsValidCollectionId(maliciousId);

        // Assert
        // Note: "collection..bad" should actually be valid since ".." is not standalone
        // Only IDs containing ".." anywhere are rejected (conservative security)
        Assert.False(result, $"Expected '{maliciousId}' to be rejected");
    }

    [Theory]
    [InlineData("collection/subdir")]
    [InlineData("collection\\subdir")]
    [InlineData("col/lection")]
    [InlineData("col\\lection")]
    public void IsValidCollectionId_ContainsSlash_ReturnsFalse(string invalidId)
    {
        // Act
        var result = ResourceUriParser.IsValidCollectionId(invalidId);

        // Assert
        Assert.False(result, $"Expected '{invalidId}' to be rejected (contains slash)");
    }

    [Fact]
    public void IsValidCollectionId_TooLong_ReturnsFalse()
    {
        // Arrange
        var longId = new string('a', 257); // 1 char over limit

        // Act
        var result = ResourceUriParser.IsValidCollectionId(longId);

        // Assert
        Assert.False(result, "Expected 257-character ID to be rejected");
    }

    [Fact]
    public void IsValidCollectionId_ExactlyMaxLength_ReturnsTrue()
    {
        // Arrange
        var maxLengthId = new string('a', 256); // Exactly at limit

        // Act
        var result = ResourceUriParser.IsValidCollectionId(maxLengthId);

        // Assert
        Assert.True(result, "Expected 256-character ID to be valid");
    }

    #endregion

    #region Document ID Validation Tests

    [Theory]
    [InlineData("ssl-setup")]
    [InlineData("chapter1")]
    [InlineData("deployment-guide")]
    [InlineData("readme.md")]
    [InlineData("config_v2.0")]
    public void IsValidDocumentId_ValidIds_ReturnsTrue(string documentId)
    {
        // Act
        var result = ResourceUriParser.IsValidDocumentId(documentId);

        // Assert
        Assert.True(result, $"Expected '{documentId}' to be valid");
    }

    [Fact]
    public void IsValidDocumentId_Null_ReturnsFalse()
    {
        // Act
        var result = ResourceUriParser.IsValidDocumentId(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidDocumentId_Empty_ReturnsFalse()
    {
        // Act
        var result = ResourceUriParser.IsValidDocumentId("");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("../secret-doc")]
    [InlineData("..\\private-file")]
    [InlineData("doc/../../../etc/passwd")]
    public void IsValidDocumentId_PathTraversal_ReturnsFalse(string maliciousId)
    {
        // Act
        var result = ResourceUriParser.IsValidDocumentId(maliciousId);

        // Assert
        Assert.False(result, $"Expected '{maliciousId}' to be rejected");
    }

    [Theory]
    [InlineData("document/subdir")]
    [InlineData("document\\subdir")]
    public void IsValidDocumentId_ContainsSlash_ReturnsFalse(string invalidId)
    {
        // Act
        var result = ResourceUriParser.IsValidDocumentId(invalidId);

        // Assert
        Assert.False(result, $"Expected '{invalidId}' to be rejected (contains slash)");
    }

    [Fact]
    public void IsValidDocumentId_TooLong_ReturnsFalse()
    {
        // Arrange
        var longId = new string('d', 257);

        // Act
        var result = ResourceUriParser.IsValidDocumentId(longId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Case Sensitivity Tests

    [Theory]
    [InlineData("resource://KNOWLEDGE/collections")]
    [InlineData("RESOURCE://knowledge/collections")]
    [InlineData("Resource://Knowledge/Collections")]
    public void Parse_CaseInsensitiveScheme_ParsesCorrectly(string uri)
    {
        // Act
        var result = _parser.Parse(uri);

        // Assert
        Assert.Equal(ResourceType.CollectionList, result.Type);
    }

    [Fact]
    public void Parse_CollectionId_PreservesCase()
    {
        // Arrange
        var uri = "resource://knowledge/Docker-Guides/documents";

        // Act
        var result = _parser.Parse(uri);

        // Assert
        Assert.Equal("Docker-Guides", result.CollectionId); // Case preserved
    }

    [Fact]
    public void Parse_DocumentId_PreservesCase()
    {
        // Arrange
        var uri = "resource://knowledge/guides/document/SSL-Setup";

        // Act
        var result = _parser.Parse(uri);

        // Assert
        Assert.Equal("SSL-Setup", result.DocumentId); // Case preserved
    }

    #endregion
}
