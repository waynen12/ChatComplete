public class TableElement : DocumentElementBase, ITableElement
{
    public List<string> Headers { get; set; }
    public List<List<string>> Rows { get; set; }

    public override string ElementType => "table";

    public TableElement(List<string> headers, List<List<string>> rows)
    {
        Headers = headers ?? new List<string>();
        Rows = rows ?? new List<List<string>>();
    }
}
