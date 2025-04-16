public interface IHeadingElement : IDocumentElement
{
    int Level { get; }     // 1 = H1, 2 = H2, etc.
    string Text { get; }
}
