using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;


public class PDFKnowledgeSourceTest
{

    [Fact]
    public async Task PDFKnowledgeSource_Should_Parse_Text()
    {
        // Arrange
        var source = new PDFKnowledgeSource();
        var pdfBytes = TestHelper.GenerateSamplePdf();
        using var stream = new MemoryStream(pdfBytes);

        // Act
        var result = await source.ParseAsync(stream);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("This is a test PDF document", result.Document.Elements
            .OfType<ParagraphElement>()
            .Select(p => p.Text)
            .FirstOrDefault());
    }
}
