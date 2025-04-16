public class ListElement : DocumentElementBase, IListElement
{
    public bool IsOrdered { get; set; }
    public List<string> Items { get; set; }

    public override string ElementType =>  IsOrdered ? "ordered-list" : "unordered-list";

    public ListElement(bool isOrdered, List<string> items)
    {
        IsOrdered = isOrdered;
        Items = items ?? new List<string>();
    }
}
