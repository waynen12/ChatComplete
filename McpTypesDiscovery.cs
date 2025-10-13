using ModelContextProtocol.Protocol;
using System.Reflection;

// Discover all types related to ResourceTemplate
var mcpAssembly = typeof(ResourceContents).Assembly;
var allTypes = mcpAssembly.GetTypes();

Console.WriteLine("=== Types containing 'Template' ===");
foreach (var type in allTypes.Where(t => t.Name.Contains("Template")))
{
    Console.WriteLine($"\n{type.FullName}");
    if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            Console.WriteLine($"  {prop.Name}: {prop.PropertyType.Name}");
        }
    }
}

Console.WriteLine("\n\n=== Types containing 'ListResourceTemplates' ===");
foreach (var type in allTypes.Where(t => t.Name.Contains("ListResourceTemplates")))
{
    Console.WriteLine($"\n{type.FullName}");
    if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            Console.WriteLine($"  {prop.Name}: {prop.PropertyType.Name}");
        }
    }
}
