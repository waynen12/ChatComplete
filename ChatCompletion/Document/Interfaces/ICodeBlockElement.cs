public interface ICodeBlockElement : IDocumentElement
{
    string Language { get; }
    string Code { get; }
}
