using System.IO;
using System.Threading.Tasks;
using Xunit;

public class DocxKnowledgeSourceTests
{
    public DocxKnowledgeSourceTests()
    {
        // Ensure the test directory exists
        var testDirectory = Path.Combine("TestData");
        if (!Directory.Exists(testDirectory))
        {
            Directory.CreateDirectory(testDirectory);
        }

        // Create a sample DOCX file for testing
        TestHelper.CreateSampleDocx(testDirectory);
    }
    [Fact]
    public async Task ParseAsync_WithValidDocx_ReturnsSuccess()
    {
        var source = new DocxKnowledgeSource();
        var path = Path.Combine("TestData", "Sample.docx");

        await using var stream = File.OpenRead(path);
        var result = await source.ParseAsync(stream);

        Assert.True(result.Success);
        Assert.NotNull(result.Document);
        Assert.NotEmpty(result.Document!.Elements);
    }

    [Fact]
    public async Task ParseAsync_WithInvalidStream_ReturnsFailure()
    {
        var source = new DocxKnowledgeSource();
        await using var stream = new MemoryStream(); // Empty or invalid .docx

        var result = await source.ParseAsync(stream);

        Assert.False(result.Success);
        Assert.Null(result.Document);
        Assert.Contains("Error parsing DOCX", result.Error);
    }
}
