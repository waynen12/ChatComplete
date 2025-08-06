using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using KnowledgeEngine.Logging;

namespace KnowledgeManager.Tests.KnowledgeEngine;

/// <summary>
/// Regression tests for PDF parsing fixes to prevent regressions in:
/// - Title deduplication (fixed issue with repeated titles)
/// - Improved heading detection from actual content
/// - Proper handling of PDF structure elements
/// </summary>
public class PDFParsingRegressionTests
{
    private readonly ITestOutputHelper _output;

    public PDFParsingRegressionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task PDF_ShouldNotRepeatTitlesMultipleTimes()
    {
        // Arrange
        LoggerProvider.ConfigureLogger();
        var source = new PDFKnowledgeSource();
        var testPdfPath = "System_Inventory_Specification.pdf";
        
        // Skip if file doesn't exist
        if (!File.Exists(testPdfPath))
        {
            _output.WriteLine($"PDF file not found at: {testPdfPath}, skipping regression test");
            return;
        }

        // Act
        using var fileStream = File.OpenRead(testPdfPath);
        var result = await source.ParseAsync(fileStream);

        // Assert
        Assert.True(result.Success, $"PDF parsing failed: {result.Error}");
        Assert.NotNull(result.Document);

        var doc = result.Document;
        var headings = doc.Elements.OfType<IHeadingElement>().ToList();
        
        // Regression Test: Ensure title appears only once (not repeated)
        var titleHeadings = headings.Where(h => h.Text.Contains("System Inventory Specification")).ToList();
        Assert.True(titleHeadings.Count <= 1, 
            $"REGRESSION: Title should appear only once, found {titleHeadings.Count} instances. " +
            $"This was a known issue that was fixed.");

        _output.WriteLine($"✅ Title deduplication working: {titleHeadings.Count} title heading(s) found");
    }

    [Fact]
    public async Task PDF_ShouldDetectActualContentHeadings()
    {
        // Arrange
        LoggerProvider.ConfigureLogger();
        var source = new PDFKnowledgeSource();
        var testPdfPath = "System_Inventory_Specification.pdf";
        
        if (!File.Exists(testPdfPath))
        {
            _output.WriteLine($"PDF file not found at: {testPdfPath}, skipping regression test");
            return;
        }

        // Act
        using var fileStream = File.OpenRead(testPdfPath);
        var result = await source.ParseAsync(fileStream);

        // Assert
        Assert.True(result.Success, $"PDF parsing failed: {result.Error}");
        Assert.NotNull(result.Document);

        var doc = result.Document;
        var headings = doc.Elements.OfType<IHeadingElement>().ToList();
        
        // Regression Test: Should find actual section headings (not just title)
        Assert.True(headings.Count > 1, 
            $"REGRESSION: Should detect multiple headings beyond just title. Found {headings.Count} headings. " +
            $"Previous bug was not detecting content headings properly.");

        // Verify specific known sections exist in the PDF
        var headingTexts = headings.Select(h => h.Text.ToUpperInvariant()).ToList();
        
        // These sections should be detected based on the PDF structure
        var expectedSections = new[] { "INTRODUCTION", "CHARTER" };
        var foundSections = expectedSections.Where(section => 
            headingTexts.Any(heading => heading.Contains(section))).ToList();

        Assert.True(foundSections.Any(), 
            $"REGRESSION: Should detect known sections like Introduction or Charter. " +
            $"Found headings: [{string.Join(", ", headingTexts.Take(10))}...]");

        _output.WriteLine($"✅ Content heading detection working: {headings.Count} total headings");
        _output.WriteLine($"✅ Found expected sections: [{string.Join(", ", foundSections)}]");
    }

    [Fact]
    public async Task PDF_ShouldProduceConsistentResults()
    {
        // Arrange
        LoggerProvider.ConfigureLogger();
        var source = new PDFKnowledgeSource();
        var testPdfPath = "System_Inventory_Specification.pdf";
        
        if (!File.Exists(testPdfPath))
        {
            _output.WriteLine($"PDF file not found at: {testPdfPath}, skipping regression test");
            return;
        }

        // Act - Parse the same PDF twice
        using var fileStream1 = File.OpenRead(testPdfPath);
        var result1 = await source.ParseAsync(fileStream1);

        using var fileStream2 = File.OpenRead(testPdfPath);
        var result2 = await source.ParseAsync(fileStream2);

        // Assert
        Assert.True(result1.Success && result2.Success, "Both parsing attempts should succeed");
        Assert.NotNull(result1.Document);
        Assert.NotNull(result2.Document);

        var headings1 = result1.Document.Elements.OfType<IHeadingElement>().ToList();
        var headings2 = result2.Document.Elements.OfType<IHeadingElement>().ToList();
        
        var paragraphs1 = result1.Document.Elements.OfType<ParagraphElement>().ToList();
        var paragraphs2 = result2.Document.Elements.OfType<ParagraphElement>().ToList();

        // Regression Test: Results should be consistent across multiple parses
        Assert.Equal(headings1.Count, headings2.Count);
        Assert.Equal(paragraphs1.Count, paragraphs2.Count);
        Assert.Equal(result1.Document.Title, result2.Document.Title);

        _output.WriteLine($"✅ Consistent results: {headings1.Count} headings, {paragraphs1.Count} paragraphs");
    }

    [Fact]
    public async Task PDF_ShouldHandleEdgeCases()
    {
        // Arrange
        LoggerProvider.ConfigureLogger();
        var source = new PDFKnowledgeSource();

        // Test with empty stream
        using var emptyStream = new MemoryStream();
        var emptyResult = await source.ParseAsync(emptyStream);

        // Test with invalid PDF data  
        using var invalidStream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02, 0x03 });
        var invalidResult = await source.ParseAsync(invalidStream);

        // Assert
        // These should fail gracefully, not crash
        Assert.False(emptyResult.Success);
        Assert.NotNull(emptyResult.Error);
        
        Assert.False(invalidResult.Success);
        Assert.NotNull(invalidResult.Error);

        _output.WriteLine($"✅ Edge case handling: Empty stream error: {emptyResult.Error}");
        _output.WriteLine($"✅ Edge case handling: Invalid stream error: {invalidResult.Error}");
    }

    [Fact]
    public void PDF_ShouldHaveCorrectElementHierarchy()
    {
        // This test validates the document structure is built correctly
        var document = new Document { Source = "test-pdf" };
        
        // Add elements in a typical PDF structure
        document.Elements.Add(new HeadingElement(1, "Main Title"));
        document.Elements.Add(new ParagraphElement("Introduction paragraph"));
        document.Elements.Add(new HeadingElement(2, "Section 1"));
        document.Elements.Add(new ParagraphElement("Section content"));

        // Assert
        var headings = document.Elements.OfType<IHeadingElement>().ToList();
        var paragraphs = document.Elements.OfType<ParagraphElement>().ToList();

        Assert.Equal(2, headings.Count);
        Assert.Equal(2, paragraphs.Count);
        
        // Verify hierarchy levels
        Assert.Equal(1, headings[0].Level);
        Assert.Equal(2, headings[1].Level);

        _output.WriteLine($"✅ Document hierarchy: H1={headings.Count(h => h.Level == 1)}, H2={headings.Count(h => h.Level == 2)}");
    }

    [Fact]
    public async Task PDF_PerformanceRegression_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        LoggerProvider.ConfigureLogger();
        var source = new PDFKnowledgeSource();
        var testPdfPath = "System_Inventory_Specification.pdf";
        
        if (!File.Exists(testPdfPath))
        {
            _output.WriteLine($"PDF file not found at: {testPdfPath}, skipping performance test");
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        using var fileStream = File.OpenRead(testPdfPath);
        var result = await source.ParseAsync(fileStream);

        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"PDF parsing failed: {result.Error}");
        
        // Regression Test: Should complete within reasonable time (10 seconds max)
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
            $"REGRESSION: PDF parsing took too long: {stopwatch.ElapsedMilliseconds}ms. " +
            $"This might indicate a performance regression.");

        _output.WriteLine($"✅ Performance: PDF parsed in {stopwatch.ElapsedMilliseconds}ms");
    }
}