using Knowledge.Analytics.Services;
using Knowledge.Contracts;
using Knowledge.Data;
using Knowledge.Mcp.Resources;
using Knowledge.Mcp.Resources.Models;
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Knowledge.Mcp.Tests.Resources;

/// <summary>
/// Unit tests for KnowledgeResourceProvider handlers.
/// Tests all resource read operations and list functionality.
/// </summary>
public class KnowledgeResourceProviderTests
{
    private readonly Mock<IKnowledgeRepository> _mockRepository;
    private readonly Mock<IUsageTrackingService> _mockUsageTracking;
    private readonly Mock<ISystemHealthService> _mockSystemHealth;
    private readonly Mock<ILogger<KnowledgeResourceProvider>> _mockLogger;
    private readonly KnowledgeResourceProvider _provider;

    public KnowledgeResourceProviderTests()
    {
        _mockRepository = new Mock<IKnowledgeRepository>();
        _mockUsageTracking = new Mock<IUsageTrackingService>();
        _mockSystemHealth = new Mock<ISystemHealthService>();
        _mockLogger = new Mock<ILogger<KnowledgeResourceProvider>>();

        _provider = new KnowledgeResourceProvider(
            _mockRepository.Object,
            _mockUsageTracking.Object,
            _mockSystemHealth.Object,
            _mockLogger.Object
        );
    }

    #region ReadCollectionListAsync Tests

    [Fact]
    public async Task ReadCollectionListAsync_ReturnsAllCollections()
    {
        // Arrange
        var collections = new List<KnowledgeSummaryDto>
        {
            new() { Id = "docker-guides", Name = "Docker Guides", DocumentCount = 5 },
            new() { Id = "kubernetes-docs", Name = "Kubernetes Docs", DocumentCount = 12 },
            new() { Id = "csharp-fundamentals", Name = "C# Fundamentals", DocumentCount = 8 }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        // Act
        var result = await _provider.ReadResourceAsync("resource://knowledge/collections");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        var content = result.Contents[0];
        Assert.Equal("resource://knowledge/collections", content.Uri);
        Assert.Equal("application/json", content.MimeType);
        Assert.Contains("docker-guides", content.Text);
        Assert.Contains("kubernetes-docs", content.Text);
        Assert.Contains("csharp-fundamentals", content.Text);
    }

    [Fact]
    public async Task ReadCollectionListAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeSummaryDto>());

        // Act
        var result = await _provider.ReadResourceAsync("resource://knowledge/collections");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        Assert.Contains("[]", result.Contents[0].Text); // Empty collections array
    }

    [Fact]
    public async Task ReadCollectionListAsync_IncludesDocumentCounts()
    {
        // Arrange
        var collections = new List<KnowledgeSummaryDto>
        {
            new() { Id = "test-collection", Name = "Test Collection", DocumentCount = 42 }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        // Act
        var result = await _provider.ReadResourceAsync("resource://knowledge/collections");

        // Assert
        Assert.Contains("42", result.Contents[0].Text); // Document count present
    }

    #endregion

    #region ReadDocumentListAsync Tests

    [Fact]
    public async Task ReadDocumentListAsync_ValidCollection_ReturnsDocuments()
    {
        // Arrange
        var collectionId = "docker-guides";
        var documents = new List<DocumentMetadataDto>
        {
            new()
            {
                DocumentId = "doc1",
                OriginalFileName = "ssl-setup.md",
                FileType = ".md",
                ChunkCount = 10,
                FileSize = 5000,
                UploadedAt = DateTime.UtcNow
            },
            new()
            {
                DocumentId = "doc2",
                OriginalFileName = "deployment.md",
                FileType = ".md",
                ChunkCount = 15,
                FileSize = 7500,
                UploadedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _provider.ReadResourceAsync($"resource://knowledge/{collectionId}/documents");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        var content = result.Contents[0];
        Assert.Equal($"resource://knowledge/{collectionId}/documents", content.Uri);
        Assert.Equal("application/json", content.MimeType);
        Assert.Contains("ssl-setup.md", content.Text);
        Assert.Contains("deployment.md", content.Text);
        Assert.Contains("doc1", content.Text);
        Assert.Contains("doc2", content.Text);
    }

    [Fact]
    public async Task ReadDocumentListAsync_InvalidCollectionId_ThrowsArgumentException()
    {
        // Arrange
        var invalidId = "../etc/passwd"; // Path traversal attempt

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _provider.ReadResourceAsync($"resource://knowledge/{invalidId}/documents")
        );
    }

    [Fact]
    public async Task ReadDocumentListAsync_NonExistentCollection_ThrowsKeyNotFoundException()
    {
        // Arrange
        var collectionId = "non-existent";

        _mockRepository
            .Setup(r => r.ExistsAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _provider.ReadResourceAsync($"resource://knowledge/{collectionId}/documents")
        );
    }

    [Fact]
    public async Task ReadDocumentListAsync_EmptyCollection_ReturnsEmptyList()
    {
        // Arrange
        var collectionId = "empty-collection";

        _mockRepository
            .Setup(r => r.ExistsAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentMetadataDto>());

        // Act
        var result = await _provider.ReadResourceAsync($"resource://knowledge/{collectionId}/documents");

        // Assert
        Assert.Contains("[]", result.Contents[0].Text); // Empty documents array
    }

    [Fact]
    public async Task ReadDocumentListAsync_IncludesChunkCounts()
    {
        // Arrange
        var collectionId = "test-collection";
        var documents = new List<DocumentMetadataDto>
        {
            new()
            {
                DocumentId = "doc1",
                OriginalFileName = "test.md",
                FileType = ".md",
                ChunkCount = 25,
                FileSize = 10000,
                UploadedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _provider.ReadResourceAsync($"resource://knowledge/{collectionId}/documents");

        // Assert
        Assert.Contains("25", result.Contents[0].Text); // Chunk count present
    }

    #endregion

    #region ReadDocumentAsync Tests

    [Fact]
    public async Task ReadDocumentAsync_ValidDocument_ReturnsFullText()
    {
        // Arrange
        var collectionId = "docker-guides";
        var documentId = "ssl-setup";
        var chunks = new List<DocumentChunkDto>
        {
            new() { ChunkId = "c1", ChunkText = "# SSL Setup Guide\n\n", ChunkOrder = 0, TokenCount = 10, CharacterCount = 20 },
            new() { ChunkId = "c2", ChunkText = "This guide shows...\n", ChunkOrder = 1, TokenCount = 15, CharacterCount = 25 },
            new() { ChunkId = "c3", ChunkText = "## Prerequisites\n", ChunkOrder = 2, TokenCount = 8, CharacterCount = 18 }
        };

        var documents = new List<DocumentMetadataDto>
        {
            new()
            {
                DocumentId = documentId,
                OriginalFileName = "ssl-setup.md",
                FileType = ".md",
                ChunkCount = 3,
                FileSize = 63,
                UploadedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetDocumentChunksAsync(collectionId, documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _provider.ReadResourceAsync($"resource://knowledge/{collectionId}/document/{documentId}");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Contents);
        var content = result.Contents[0];
        Assert.Equal($"resource://knowledge/{collectionId}/document/{documentId}", content.Uri);
        Assert.Equal("text/markdown", content.MimeType); // Detected from .md filename
        Assert.Contains("# SSL Setup Guide", content.Text);
        Assert.Contains("This guide shows", content.Text);
        Assert.Contains("## Prerequisites", content.Text);
    }

    [Fact]
    public async Task ReadDocumentAsync_ChunksOrderedCorrectly()
    {
        // Arrange
        var collectionId = "test";
        var documentId = "test-doc";
        var chunks = new List<DocumentChunkDto>
        {
            new() { ChunkId = "c3", ChunkText = "Third", ChunkOrder = 2, TokenCount = 5, CharacterCount = 5 },
            new() { ChunkId = "c1", ChunkText = "First", ChunkOrder = 0, TokenCount = 5, CharacterCount = 5 },
            new() { ChunkId = "c2", ChunkText = "Second", ChunkOrder = 1, TokenCount = 5, CharacterCount = 6 }
        };

        var documents = new List<DocumentMetadataDto>
        {
            new()
            {
                DocumentId = documentId,
                OriginalFileName = "test.txt",
                FileType = ".txt",
                ChunkCount = 3,
                FileSize = 16,
                UploadedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetDocumentChunksAsync(collectionId, documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _provider.ReadResourceAsync($"resource://knowledge/{collectionId}/document/{documentId}");

        // Assert
        Assert.Equal("FirstSecondThird", result.Contents[0].Text); // Correct order
    }

    [Fact]
    public async Task ReadDocumentAsync_MarkdownFile_DetectsMimeType()
    {
        // Arrange
        var collectionId = "test";
        var documentId = "doc";
        var chunks = new List<DocumentChunkDto>
        {
            new() { ChunkId = "c1", ChunkText = "Content", ChunkOrder = 0, TokenCount = 5, CharacterCount = 7 }
        };

        var documents = new List<DocumentMetadataDto>
        {
            new()
            {
                DocumentId = documentId,
                OriginalFileName = "README.markdown", // .markdown extension
                FileType = ".markdown",
                ChunkCount = 1,
                FileSize = 7,
                UploadedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetDocumentChunksAsync(collectionId, documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _provider.ReadResourceAsync($"resource://knowledge/{collectionId}/document/{documentId}");

        // Assert
        Assert.Equal("text/markdown", result.Contents[0].MimeType);
    }

    [Fact]
    public async Task ReadDocumentAsync_JsonFile_DetectsMimeType()
    {
        // Arrange
        var collectionId = "test";
        var documentId = "doc";
        var chunks = new List<DocumentChunkDto>
        {
            new() { ChunkId = "c1", ChunkText = "{\"key\":\"value\"}", ChunkOrder = 0, TokenCount = 5, CharacterCount = 15 }
        };

        var documents = new List<DocumentMetadataDto>
        {
            new()
            {
                DocumentId = documentId,
                OriginalFileName = "config.json",
                FileType = ".json",
                ChunkCount = 1,
                FileSize = 15,
                UploadedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetDocumentChunksAsync(collectionId, documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _provider.ReadResourceAsync($"resource://knowledge/{collectionId}/document/{documentId}");

        // Assert
        Assert.Equal("application/json", result.Contents[0].MimeType);
    }

    [Fact]
    public async Task ReadDocumentAsync_PlainTextFile_DetectsMimeType()
    {
        // Arrange
        var collectionId = "test";
        var documentId = "doc";
        var chunks = new List<DocumentChunkDto>
        {
            new() { ChunkId = "c1", ChunkText = "Plain text", ChunkOrder = 0, TokenCount = 5, CharacterCount = 10 }
        };

        var documents = new List<DocumentMetadataDto>
        {
            new()
            {
                DocumentId = documentId,
                OriginalFileName = "notes.txt",
                FileType = ".txt",
                ChunkCount = 1,
                FileSize = 10,
                UploadedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetDocumentChunksAsync(collectionId, documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _provider.ReadResourceAsync($"resource://knowledge/{collectionId}/document/{documentId}");

        // Assert
        Assert.Equal("text/plain", result.Contents[0].MimeType);
    }

    [Fact]
    public async Task ReadDocumentAsync_InvalidCollectionId_ThrowsArgumentException()
    {
        // Arrange
        var invalidId = "../secrets";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _provider.ReadResourceAsync($"resource://knowledge/{invalidId}/document/doc1")
        );
    }

    [Fact]
    public async Task ReadDocumentAsync_InvalidDocumentId_ThrowsArgumentException()
    {
        // Arrange
        var invalidId = "../../passwd";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _provider.ReadResourceAsync($"resource://knowledge/collection1/document/{invalidId}")
        );
    }

    [Fact]
    public async Task ReadDocumentAsync_NonExistentDocument_ThrowsKeyNotFoundException()
    {
        // Arrange
        var collectionId = "test";
        var documentId = "non-existent";

        _mockRepository
            .Setup(r => r.ExistsAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetDocumentChunksAsync(collectionId, documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentChunkDto>()); // No chunks = document not found

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _provider.ReadResourceAsync($"resource://knowledge/{collectionId}/document/{documentId}")
        );
    }

    [Fact]
    public async Task ReadDocumentAsync_SingleChunk_Works()
    {
        // Arrange
        var collectionId = "test";
        var documentId = "short-doc";
        var chunks = new List<DocumentChunkDto>
        {
            new() { ChunkId = "c1", ChunkText = "Short document content", ChunkOrder = 0, TokenCount = 10, CharacterCount = 22 }
        };

        var documents = new List<DocumentMetadataDto>
        {
            new()
            {
                DocumentId = documentId,
                OriginalFileName = "short.txt",
                FileType = ".txt",
                ChunkCount = 1,
                FileSize = 22,
                UploadedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetDocumentChunksAsync(collectionId, documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _provider.ReadResourceAsync($"resource://knowledge/{collectionId}/document/{documentId}");

        // Assert
        Assert.Equal("Short document content", result.Contents[0].Text);
    }

    [Fact]
    public async Task ReadDocumentAsync_ManyChunks_ReconstructsCorrectly()
    {
        // Arrange
        var collectionId = "test";
        var documentId = "long-doc";
        var chunks = Enumerable.Range(0, 50).Select(i => new DocumentChunkDto
        {
            ChunkId = $"c{i}",
            ChunkText = $"Chunk{i} ",
            ChunkOrder = i,
            TokenCount = 5,
            CharacterCount = 8
        }).ToList();

        var documents = new List<DocumentMetadataDto>
        {
            new()
            {
                DocumentId = documentId,
                OriginalFileName = "long.txt",
                FileType = ".txt",
                ChunkCount = 50,
                FileSize = 400,
                UploadedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetDocumentChunksAsync(collectionId, documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _provider.ReadResourceAsync($"resource://knowledge/{collectionId}/document/{documentId}");

        // Assert
        Assert.Contains("Chunk0", result.Contents[0].Text);
        Assert.Contains("Chunk49", result.Contents[0].Text);
        Assert.Contains("Chunk25", result.Contents[0].Text);
    }

    #endregion

    #region ListResourcesAsync Tests

    [Fact]
    public async Task ListResourcesAsync_IncludesStaticResources()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeSummaryDto>());

        // Act
        var result = await _provider.ListResourcesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Resources, r => r.Uri == "resource://knowledge/collections");
        Assert.Contains(result.Resources, r => r.Uri == "resource://system/health");
        Assert.Contains(result.Resources, r => r.Uri == "resource://system/models");
    }

    [Fact]
    public async Task ListResourcesAsync_IncludesCollectionResources()
    {
        // Arrange
        var collections = new List<KnowledgeSummaryDto>
        {
            new() { Id = "docker-guides", Name = "Docker Guides", DocumentCount = 5 },
            new() { Id = "k8s-docs", Name = "Kubernetes", DocumentCount = 10 }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentMetadataDto>());

        // Act
        var result = await _provider.ListResourcesAsync();

        // Assert
        // Each collection should have documents and stats resources
        Assert.Contains(result.Resources, r => r.Uri == "resource://knowledge/docker-guides/documents");
        Assert.Contains(result.Resources, r => r.Uri == "resource://knowledge/docker-guides/stats");
        Assert.Contains(result.Resources, r => r.Uri == "resource://knowledge/k8s-docs/documents");
        Assert.Contains(result.Resources, r => r.Uri == "resource://knowledge/k8s-docs/stats");
    }

    [Fact]
    public async Task ListResourcesAsync_IncludesDocumentResources()
    {
        // Arrange
        var collections = new List<KnowledgeSummaryDto>
        {
            new() { Id = "test-collection", Name = "Test", DocumentCount = 2 }
        };

        var documents = new List<DocumentMetadataDto>
        {
            new()
            {
                DocumentId = "doc1",
                OriginalFileName = "file1.md",
                FileType = ".md",
                ChunkCount = 5,
                FileSize = 1000,
                UploadedAt = DateTime.UtcNow
            },
            new()
            {
                DocumentId = "doc2",
                OriginalFileName = "file2.txt",
                FileType = ".txt",
                ChunkCount = 3,
                FileSize = 500,
                UploadedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync("test-collection", It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _provider.ListResourcesAsync();

        // Assert
        Assert.Contains(result.Resources, r => r.Uri == "resource://knowledge/test-collection/document/doc1");
        Assert.Contains(result.Resources, r => r.Uri == "resource://knowledge/test-collection/document/doc2");
    }

    [Fact]
    public async Task ListResourcesAsync_AllUrisAreValid()
    {
        // Arrange
        var collections = new List<KnowledgeSummaryDto>
        {
            new() { Id = "collection1", Name = "Collection 1", DocumentCount = 1 }
        };

        var documents = new List<DocumentMetadataDto>
        {
            new()
            {
                DocumentId = "doc1",
                OriginalFileName = "test.md",
                FileType = ".md",
                ChunkCount = 1,
                FileSize = 100,
                UploadedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _provider.ListResourcesAsync();

        // Assert
        var parser = new ResourceUriParser();
        foreach (var resource in result.Resources)
        {
            // Should not throw
            var parsed = parser.Parse(resource.Uri);
            Assert.NotNull(parsed);
        }
    }

    [Fact]
    public async Task ListResourcesAsync_AllMetadataHasRequiredFields()
    {
        // Arrange
        var collections = new List<KnowledgeSummaryDto>
        {
            new() { Id = "test", Name = "Test", DocumentCount = 0 }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentMetadataDto>());

        // Act
        var result = await _provider.ListResourcesAsync();

        // Assert
        foreach (var resource in result.Resources)
        {
            Assert.NotNull(resource.Uri);
            Assert.NotEmpty(resource.Uri);
            Assert.NotNull(resource.Name);
            Assert.NotEmpty(resource.Name);
            Assert.NotNull(resource.MimeType);
            Assert.NotEmpty(resource.MimeType);
            // Description can be null/empty
        }
    }

    [Fact]
    public async Task ListResourcesAsync_NoDuplicateUris()
    {
        // Arrange
        var collections = new List<KnowledgeSummaryDto>
        {
            new() { Id = "collection1", Name = "Collection 1", DocumentCount = 1 }
        };

        var documents = new List<DocumentMetadataDto>
        {
            new()
            {
                DocumentId = "doc1",
                OriginalFileName = "test.md",
                FileType = ".md",
                ChunkCount = 1,
                FileSize = 100,
                UploadedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        _mockRepository
            .Setup(r => r.GetDocumentsByCollectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _provider.ListResourcesAsync();

        // Assert
        var uris = result.Resources.Select(r => r.Uri).ToList();
        var uniqueUris = uris.Distinct().ToList();
        Assert.Equal(uris.Count, uniqueUris.Count); // No duplicates
    }

    [Fact]
    public async Task ListResourcesAsync_PaginationCursorIsNull()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeSummaryDto>());

        // Act
        var result = await _provider.ListResourcesAsync();

        // Assert
        Assert.Null(result.NextCursor); // No pagination implemented yet
    }

    #endregion
}
