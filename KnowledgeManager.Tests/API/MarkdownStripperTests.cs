using Markdig;

namespace KnowledgeManager.Tests.API;

/// <summary>
/// Tests for markdown to plain text conversion
/// Note: MarkdownStripper is internal, so we test via reflection or create a public wrapper
/// </summary>
public class MarkdownStripperTests
{
    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    private static string ToPlain(string md) =>
        Markdown.ToPlainText(md, Pipeline).Trim();

    [Fact]
    public void ToPlain_WithSimpleText_ShouldReturnUnchanged()
    {
        // Arrange
        var input = "This is plain text";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Equal("This is plain text", result);
    }

    [Fact]
    public void ToPlain_WithBoldText_ShouldRemoveFormatting()
    {
        // Arrange
        var input = "This is **bold** text";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Equal("This is bold text", result);
    }

    [Fact]
    public void ToPlain_WithItalicText_ShouldRemoveFormatting()
    {
        // Arrange
        var input = "This is *italic* text";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Equal("This is italic text", result);
    }

    [Fact]
    public void ToPlain_WithCodeInline_ShouldPreserveText()
    {
        // Arrange
        var input = "Use the `console.log()` function";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("console.log()", result);
    }

    [Fact]
    public void ToPlain_WithCodeBlock_ShouldPreserveCode()
    {
        // Arrange
        var input = @"Here is some code:

```javascript
function test() {
    return 42;
}
```";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("function test()", result);
        Assert.Contains("return 42;", result);
    }

    [Fact]
    public void ToPlain_WithHeadings_ShouldPreserveText()
    {
        // Arrange
        var input = @"# Heading 1
## Heading 2
### Heading 3";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("Heading 1", result);
        Assert.Contains("Heading 2", result);
        Assert.Contains("Heading 3", result);
    }

    [Fact]
    public void ToPlain_WithLinks_ShouldPreserveLinkText()
    {
        // Arrange
        var input = "Check out [this link](https://example.com)";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("this link", result);
        Assert.DoesNotContain("https://example.com", result);
    }

    [Fact]
    public void ToPlain_WithLists_ShouldPreserveItems()
    {
        // Arrange
        var input = @"- Item 1
- Item 2
- Item 3";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("Item 1", result);
        Assert.Contains("Item 2", result);
        Assert.Contains("Item 3", result);
    }

    [Fact]
    public void ToPlain_WithOrderedList_ShouldPreserveItems()
    {
        // Arrange
        var input = @"1. First
2. Second
3. Third";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("First", result);
        Assert.Contains("Second", result);
        Assert.Contains("Third", result);
    }

    [Fact]
    public void ToPlain_WithBlockquote_ShouldPreserveText()
    {
        // Arrange
        var input = "> This is a quote";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("This is a quote", result);
    }

    [Fact]
    public void ToPlain_WithTable_ShouldPreserveContent()
    {
        // Arrange
        var input = @"| Column 1 | Column 2 |
|----------|----------|
| Value 1  | Value 2  |";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("Column 1", result);
        Assert.Contains("Column 2", result);
        Assert.Contains("Value 1", result);
        Assert.Contains("Value 2", result);
    }

    [Fact]
    public void ToPlain_WithHorizontalRule_ShouldRemoveFormatting()
    {
        // Arrange
        var input = @"Above
---
Below";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("Above", result);
        Assert.Contains("Below", result);
    }

    [Fact]
    public void ToPlain_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToPlain_WithMixedFormatting_ShouldRemoveAllFormatting()
    {
        // Arrange
        var input = "**Bold**, *italic*, and `code` all together";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("Bold", result);
        Assert.Contains("italic", result);
        Assert.Contains("code", result);
        Assert.DoesNotContain("**", result);
        Assert.DoesNotContain("*", result);
    }

    [Fact]
    public void ToPlain_WithStrikethrough_ShouldPreserveText()
    {
        // Arrange
        var input = "This is ~~strikethrough~~ text";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("strikethrough", result);
    }

    [Fact]
    public void ToPlain_WithTaskList_ShouldPreserveItems()
    {
        // Arrange
        var input = @"- [x] Completed task
- [ ] Incomplete task";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("Completed task", result);
        Assert.Contains("Incomplete task", result);
    }

    [Fact]
    public void ToPlain_WithNestedLists_ShouldPreserveAllItems()
    {
        // Arrange
        var input = @"- Parent 1
  - Child 1
  - Child 2
- Parent 2";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("Parent 1", result);
        Assert.Contains("Child 1", result);
        Assert.Contains("Child 2", result);
        Assert.Contains("Parent 2", result);
    }

    [Fact]
    public void ToPlain_WithHtmlTags_ShouldRemoveTags()
    {
        // Arrange
        var input = "This has <strong>HTML</strong> tags";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("HTML", result);
        Assert.DoesNotContain("<strong>", result);
        Assert.DoesNotContain("</strong>", result);
    }

    [Fact]
    public void ToPlain_WithMultipleParagraphs_ShouldPreserveContent()
    {
        // Arrange
        var input = @"First paragraph.

Second paragraph.

Third paragraph.";

        // Act
        var result = ToPlain(input);

        // Assert
        Assert.Contains("First paragraph", result);
        Assert.Contains("Second paragraph", result);
        Assert.Contains("Third paragraph", result);
    }
}
