public class HeadingElement : DocumentElementBase, IHeadingElement
{
    public int Level { get; set; }
    public string Text { get; set; }

    public override string ElementType => "heading";

    public HeadingElement(int level, string text)
    {
        Level = level;
        Text = text;
    }
}
