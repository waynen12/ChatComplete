// MarkdownStripper.cs
using Markdig;

static class MarkdownStripper
{
    static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public static string ToPlain(string md) =>
        Markdown.ToPlainText(md, Pipeline);
}
