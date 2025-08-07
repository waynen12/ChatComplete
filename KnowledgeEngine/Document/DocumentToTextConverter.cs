using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChatCompletion.Config;
using KnowledgeEngine.Document;

public static class DocumentToTextConverter
{
    public static string Convert(Document doc, ChatCompleteSettings? settings = null)
    {
        var builder = new StringBuilder();

        foreach (var element in doc.Elements)
        {
            switch (element)
            {
                case IHeadingElement heading:
                    builder.AppendLine($"{new string('#', heading.Level)} {EscapeMarkdown(heading.Text)}");
                    break;

                case IParagraphElement paragraph:
                    builder.AppendLine(EscapeMarkdown(paragraph.Text));
                    break;

                case IListElement list:
                    var items = list.Items ?? new List<string>();
                    for (int i = 0; i < items.Count; i++)
                    {
                        var prefix = list.IsOrdered ? $"{i + 1}. " : "- ";
                        builder.AppendLine($"{prefix}{EscapeMarkdown(items[i])}");
                    }
                    break;

                case ITableElement table:
                    var headers = table.Headers ?? new List<string>();
                    var rows = table.Rows ?? new List<List<string>>();

                    builder.AppendLine("| " + string.Join(" | ", headers.Select(EscapeMarkdown)) + " |");
                    builder.AppendLine("| " + string.Join(" | ", headers.Select(_ => "---")) + " |");

                    foreach (var row in rows)
                    {
                        builder.AppendLine("| " + string.Join(" | ", row.Select(EscapeMarkdown)) + " |");
                    }
                    break;

                case IQuoteElement quote:
                    builder.AppendLine($"> {EscapeMarkdown(quote.Text)}");
                    break;

                case ICodeBlockElement code:
                    builder.AppendLine($"```{code.Language}");
                    var guardedCode = CodeFenceGuard.GuardCodeFence(code.Code, code.Language, settings);
                    builder.AppendLine(guardedCode);
                    builder.AppendLine("```");
                    break;

                case IImageElement image:
                    builder.AppendLine($"![{EscapeMarkdown(image.AltText)}]({EscapeMarkdown(image.ImagePath)})");
                    break;

                default:
                    builder.AppendLine(); // Fallback spacing
                    break;
            }

            builder.AppendLine(); // Add spacing between elements
        }

        return builder.ToString().Trim();
    }

    private static string EscapeMarkdown(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return input
            .Replace("|", @"\|")
            .Replace("*", @"\*")
            .Replace("_", @"\_")
            .Replace("`", @"\`")
            .Replace("#", @"\#");
    }
}