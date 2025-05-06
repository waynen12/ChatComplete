using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;


public class KnowledgeSourceResolverTests
{
    private readonly KnowledgeSourceResolver _resolver;

    public KnowledgeSourceResolverTests()
    {
        var factory = new KnowledgeSourceFactory();
        _resolver = new KnowledgeSourceResolver(factory);
    }

    [Fact]
    public async Task ParseAsync_ShouldParseTxtFileSuccessfully()
    {
        // Arrange
        var sampleText = "This is a line.\nAnd another one.";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(sampleText));

        // Act
        var result = await _resolver.ParseAsync(stream, "example.txt");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Document);
        Assert.Equal(2, result.Document.Elements.Count); // two paragraphs
    }

    [Fact]
    public async Task ParseAsync_ShouldFailForUnsupportedExtension()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("Some content"));

        // Act
        var result = await _resolver.ParseAsync(stream, "unknown.xyz");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No knowledge source registered", result.Error);
    }

    [Fact]
    public async Task ParseAsync_ShouldFailForMissingExtension()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("Some content"));

        // Act
        var result = await _resolver.ParseAsync(stream, "file");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("no extension", result.Error.ToLower());
    }
}
