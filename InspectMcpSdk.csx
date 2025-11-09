#!/usr/bin/env dotnet-script

using System;
using System.Reflection;
using System.Linq;

var dllPath = "/home/wayne/.nuget/packages/modelcontextprotocol/0.4.0-preview.2/lib/net8.0/ModelContextProtocol.dll";
var assembly = Assembly.LoadFrom(dllPath);

Console.WriteLine("=== MCP SDK Assembly Information ===");
Console.WriteLine($"Version: {assembly.GetName().Version}");
Console.WriteLine();

// Find all types
var types = assembly.GetTypes();

// Look for extension methods related to resources
Console.WriteLine("=== Extension Methods with 'Resource' in name ===");
foreach (var type in types.Where(t => t.IsClass && t.IsSealed && t.IsAbstract)) // static classes
{
    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(m => m.Name.Contains("Resource", StringComparison.OrdinalIgnoreCase));

    foreach (var method in methods)
    {
        Console.WriteLine($"{type.FullName}.{method.Name}");
        var parameters = method.GetParameters();
        foreach (var param in parameters)
        {
            Console.WriteLine($"  - {param.ParameterType.Name} {param.Name}");
        }
        Console.WriteLine();
    }
}

// Look for IMcpServerBuilder extension methods
Console.WriteLine("=== IMcpServerBuilder Extension Methods ===");
foreach (var type in types.Where(t => t.IsClass && t.IsSealed && t.IsAbstract))
{
    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(m => m.GetParameters().FirstOrDefault()?.ParameterType.Name.Contains("IMcpServerBuilder") == true);

    foreach (var method in methods)
    {
        Console.WriteLine($"{type.FullName}.{method.Name}");
        var parameters = method.GetParameters();
        foreach (var param in parameters)
        {
            Console.WriteLine($"  - {param.ParameterType.Name} {param.Name}");
        }
        Console.WriteLine();
    }
}

// Look for resource-related attributes
Console.WriteLine("=== Resource-Related Attributes ===");
var attributes = types.Where(t => t.IsClass && typeof(Attribute).IsAssignableFrom(t) &&
    t.Name.Contains("Resource", StringComparison.OrdinalIgnoreCase));
foreach (var attr in attributes)
{
    Console.WriteLine($"{attr.FullName}");
    var props = attr.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    foreach (var prop in props)
    {
        Console.WriteLine($"  - {prop.PropertyType.Name} {prop.Name}");
    }
    Console.WriteLine();
}
