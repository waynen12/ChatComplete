public interface ITableElement : IDocumentElement
{
    List<string> Headers { get; }           // Optional
    List<List<string>> Rows { get; }
}
