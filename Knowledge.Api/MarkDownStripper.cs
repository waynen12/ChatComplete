// MarkdownStripper.cs
using Markdig;

static class MarkdownStripper
{
    static readonly MarkdownPipeline _pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public static string ToPlain(string md) =>
        Markdown.ToPlainText(md, _pipeline);
}
