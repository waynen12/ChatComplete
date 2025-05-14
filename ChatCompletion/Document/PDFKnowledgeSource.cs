using System.Text.RegularExpressions;
using Amazon.Runtime.Internal.Util;
using UglyToad.PdfPig;

public class PDFKnowledgeSource : IKnowledgeSource
{
    public string SupportedFileExtension => ".pdf";

    public Task<KnowledgeParseResult> ParseAsync(Stream fileStream)
    {
        try
        {
            var doc = new Document
            {
                Source = "Uploaded PDF",
                Tags = new List<string>()
            };

            using var pdf = PdfDocument.Open(fileStream);

            foreach (var page in pdf.GetPages())
            {
                var text = page.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Optionally split text by paragraphs
                    var paragraphs = Regex.Split(text, @"\r?\n\s*\r?\n");

                    foreach (var paragraph in paragraphs)
                    {
                        var cleaned = paragraph.Trim();
                        if (!string.IsNullOrWhiteSpace(cleaned))
                            doc.Elements.Add(new ParagraphElement(cleaned));
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(doc.Title) && doc.Elements.OfType<ParagraphElement>().Any())
            {
                doc.Title = doc.Elements.OfType<ParagraphElement>().First().Text;
            }

            return Task.FromResult(KnowledgeParseResult.Ok(doc));    
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error($"Error parsing PDF", ex);
            return Task.FromResult(KnowledgeParseResult.Fail($"Error parsing PDF: {ex.Message}"));
        }
        
    }
}
