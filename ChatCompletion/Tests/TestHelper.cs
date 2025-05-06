using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;
using Pdf = iText.Kernel.Pdf;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

public static class TestHelper
{
    public static string CreateSampleDocx(string directory)
    {
        string filePath = Path.Combine(directory, "Sample.docx");

        using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new W.Document();
            var body = new W.Body();

            body.Append(CreateHeading("Introduction", 1));
            body.Append(CreateParagraph("This is a test document for unit testing."));
            body.Append(CreateHeading("Details", 2));
            body.Append(CreateParagraph("This section contains more detailed text about the topic."));

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return filePath;
    }

 
    public static byte[] GenerateSamplePdf()
    {
        using var ms = new MemoryStream();
        var writer = new PdfWriter(ms);
        var pdf = new PdfDocument(writer);
        var doc = new iText.Layout.Document(pdf);

        doc.Add(new Paragraph("This is a test PDF document."));
        doc.Close();

        return ms.ToArray();
    }

    private static W.Paragraph CreateHeading(string text, int level)
    {
        var run = new W.Run(new W.Text(text));
        var paraProps = new W.ParagraphProperties(
            new W.ParagraphStyleId { Val = $"Heading{level}" }
        );

        return new W.Paragraph(paraProps, run);
    }

    private static W.Paragraph CreateParagraph(string text)
    {
        return new W.Paragraph(new W.Run(new W.Text(text)));
    }
}
