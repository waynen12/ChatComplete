using System.Text.RegularExpressions;

public class MarkdownToDocumentConverter
{
    public Document Convert(string markdown, string sourceName, List<string>? tags = null)
    {
        var doc = new Document
        {
            Source = sourceName,
            Tags = tags ?? new List<string>()
        };

        var lines = markdown.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            if (trimmed.StartsWith("#"))
            {
                int level = trimmed.TakeWhile(c => c == '#').Count();
                string text = trimmed.Substring(level).Trim();
                doc.Elements.Add(new HeadingElement(level, text));
                if (string.IsNullOrWhiteSpace(doc.Title))
                    doc.Title = text;
            }
            else if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
            {
                var lastList = doc.Elements.LastOrDefault() as ListElement;
                if (lastList != null && !lastList.IsOrdered)
                {
                    lastList.Items.Add(trimmed.Substring(2).Trim());
                }
                else
                {
                    doc.Elements.Add(new ListElement(false, new List<string> { trimmed.Substring(2).Trim() }));
                }
            }
            else if (Regex.IsMatch(trimmed, @"^\d+\.\s"))
            {
                var lastList = doc.Elements.LastOrDefault() as ListElement;
                if (lastList != null && lastList.IsOrdered)
                {
                    lastList.Items.Add(Regex.Replace(trimmed, @"^\d+\.\s", ""));
                }
                else
                {
                    doc.Elements.Add(new ListElement(true, new List<string> { Regex.Replace(trimmed, @"^\d+\.\s", "") }));
                }
            }
            else
            {
                doc.Elements.Add(new ParagraphElement(trimmed));
            }
        }

        return doc;
    }
}
