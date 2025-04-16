public class CodeBlockElement : DocumentElementBase, ICodeBlockElement
{
    public string Language { get; set; }
    public string Code { get; set; }

    public override string ElementType => "code";

    public CodeBlockElement(string language, string code)
    {
        Language = language;
        Code = code;
    }
}
