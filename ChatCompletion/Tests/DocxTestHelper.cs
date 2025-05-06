using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;

public static class DocxTestHelper
{
    public static string CreateSampleDocx(string directory)
    {
        string filePath = Path.Combine(directory, "Sample.docx");

        using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            var body = new Body();

            body.Append(CreateHeading("Introduction", 1));
            body.Append(CreateParagraph("This is a test document for unit testing."));
            body.Append(CreateHeading("Details", 2));
            body.Append(CreateParagraph("This section contains more detailed text about the topic."));

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return filePath;
    }

    private static Paragraph CreateHeading(string text, int level)
    {
        return new Paragraph(new Run(new Text(text)))
        {
            ParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = $"Heading{level}" })
        };
    }

    private static Paragraph CreateParagraph(string text)
    {
        return new Paragraph(new Run(new Text(text)));
    }
}
