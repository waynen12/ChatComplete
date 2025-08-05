using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using KnowledgeEngine.Logging;

namespace KnowledgeEngine.Tests
{
    public class PDFParsingIntegrationTest
    {
        private readonly ITestOutputHelper _output;

        public PDFParsingIntegrationTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task PDF_Should_Detect_Headings_From_System_Inventory_Spec()
        {
            // Arrange
            LoggerProvider.ConfigureLogger();
            var source = new PDFKnowledgeSource();
            var pdfPath = "System_Inventory_Specification.pdf";
            
            // Skip test if file doesn't exist
            if (!File.Exists(pdfPath))
            {
                _output.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
                _output.WriteLine($"PDF file not found at: {pdfPath}");
                return; // Skip test gracefully
            }

            // Act
            using var fileStream = File.OpenRead(pdfPath);
            var result = await source.ParseAsync(fileStream);

            // Assert
            Assert.True(result.Success, $"PDF parsing failed: {result.Error}");
            Assert.NotNull(result.Document);
            
            var doc = result.Document;
            var headings = doc.Elements.OfType<IHeadingElement>().ToList();
            var paragraphs = doc.Elements.OfType<ParagraphElement>().ToList();
            
            // We should have found at least some headings
            Assert.True(headings.Count > 0, $"Expected headings but found {headings.Count}");
            
            // Check that we found the main title (should be deduplicated now)
            var titleHeadings = headings.Where(h => h.Text.Contains("System Inventory Specification")).ToList();
            Assert.True(titleHeadings.Count == 1, $"Expected exactly 1 title heading, found {titleHeadings.Count}");
            
            // Check that we found section headings (be flexible about exact format)
            Assert.Contains(headings, h => h.Text.Contains("Introduction") || h.Text.Contains("INTRODUCTION"));
            Assert.Contains(headings, h => h.Text.Contains("Charter") || h.Text.Contains("CHARTER"));
            
            // Log results for debugging
            _output.WriteLine($"✅ PDF parsed successfully!");
            _output.WriteLine($"📋 Title: {doc.Title}");
            _output.WriteLine($"📑 Total elements: {doc.Elements.Count}");
            _output.WriteLine($"📝 Headings found: {headings.Count}");
            _output.WriteLine($"📄 Paragraphs found: {paragraphs.Count}");
            
            _output.WriteLine("\n🎯 First 5 headings:");
            foreach (var heading in headings.Take(5))
            {
                _output.WriteLine($"  H{heading.Level}: {heading.Text.Substring(0, Math.Min(80, heading.Text.Length))}");
            }
        }
    }
}