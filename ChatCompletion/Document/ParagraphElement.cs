public class ParagraphElement : DocumentElementBase, IParagraphElement
{
    public string Text { get; set; }

    public override string ElementType => "paragraph";

    public ParagraphElement( string text)
    {
        Text = text;
    }
}
