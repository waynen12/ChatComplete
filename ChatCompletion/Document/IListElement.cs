public interface IListElement : IDocumentElement
{
    bool IsOrdered { get; }
    List<string> Items { get; }
}
