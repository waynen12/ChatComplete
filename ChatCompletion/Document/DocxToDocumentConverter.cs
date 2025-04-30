using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using System.Collections.Generic;
using System.Linq;

public class DocxToDocumentConverter
{
    public Document Convert(string filePath, string sourceName, List<string>? tags = null)
    {
        var document = new Document
        {
            Source = sourceName,
            Tags = tags ?? new List<string>()
        };

        using var wordDoc = WordprocessingDocument.Open(filePath, false);
        var body = wordDoc?.MainDocumentPart?.Document.Body;

        if (body != null)
        {
            foreach (var element in body.Elements())
            {
                if (element is Paragraph para)
                {
                    var text = ExtractText((OpenXmlElement)para);

                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    if (IsHeading(para, out int level))
                    {
                        document.Elements.Add(new HeadingElement(level, text));
                        if (string.IsNullOrWhiteSpace(document.Title))
                            document.Title = text; // first heading becomes title
                    }
                    else if (IsListItem(para, out bool ordered))
                    {
                        var lastList = document.Elements.LastOrDefault() as ListElement;
                        if (lastList != null && lastList.IsOrdered == ordered)
                        {
                            lastList.Items.Add(text);
                        }
                        else
                        {
                            document.Elements.Add(new ListElement(ordered, new List<string> { text }));
                        }
                    }
                    else
                    {
                        document.Elements.Add(new ParagraphElement(text));
                    }
                }
                else if (element is Table table)
                {
                    var rows = new List<List<string>>();
                    List<string>? headers = null;

                    foreach (var row in table.Elements<TableRow>())
                    {
                        var cells = row.Elements<TableCell>()
                                    .Select(cell => ExtractText(cell))
                                    .ToList();

                        if (headers == null)
                            headers = cells;
                        else
                            rows.Add(cells);
                    }
                    if (headers != null && rows.Count > 0)
                    {                        
                        document.Elements.Add(new TableElement(headers, rows));
                    }
                }
            }
        }

        return document;
    }

    private string ExtractText(OpenXmlElement element)
    {
        return string.Join("",
            element.Descendants<Text>()
                   .Select(t => t.Text))
            .Trim();
    }

    private bool IsHeading(Paragraph para, out int level)
    {
        level = 0;
        var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

        if (styleId != null && styleId.StartsWith("Heading"))
        {
            if (int.TryParse(styleId.Substring("Heading".Length), out level))
                return true;
        }

        return false;
    }

    private bool IsListItem(Paragraph para, out bool ordered)
    {
        ordered = false;

        var numberingProps = para.ParagraphProperties?.NumberingProperties;
        if (numberingProps != null)
        {
            // Simplified assumption: if it's numbered, it's ordered.
            ordered = numberingProps.NumberingId != null;
            return true;
        }

        return false;
    }
}