public class QuoteElement : DocumentElementBase, IQuoteElement
{
    public string Text { get; set; }

    public override string ElementType => "quote";

    public QuoteElement(string text)
    {
        Text = text;
    }
}
