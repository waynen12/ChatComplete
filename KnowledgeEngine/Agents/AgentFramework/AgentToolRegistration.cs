using System.Reflection;
using Microsoft.Extensions.AI;

namespace KnowledgeEngine.Agents.AgentFramework;

/// <summary>
/// Helper class for registering Agent Framework tools using reflection.
/// Converts plugin classes into AITool instances for agent registration.
/// </summary>
public static class AgentToolRegistration
{
    /// <summary>
    /// Creates a list of AITools from a plugin instance using reflection.
    /// Discovers all public instance methods and converts them to tools.
    /// </summary>
    /// <typeparam name="T">The plugin type</typeparam>
    /// <param name="pluginInstance">Instance of the plugin with methods to register</param>
    /// <returns>List of AITool instances ready for agent registration</returns>
    public static List<AITool> CreateToolsFromPlugin<T>(T pluginInstance) where T : class
    {
        if (pluginInstance == null)
        {
            throw new ArgumentNullException(nameof(pluginInstance));
        }

        var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        var tools = new List<AITool>();

        foreach (var method in methods)
        {
            // Skip methods from Object base class
            if (method.DeclaringType == typeof(object))
            {
                continue;
            }

            try
            {
                var tool = AIFunctionFactory.Create(method, pluginInstance);
                tools.Add(tool);

                Console.WriteLine($"✅ Registered AF tool: {method.Name} from {typeof(T).Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Failed to register method {method.Name}: {ex.Message}");
            }
        }

        return tools;
    }

    /// <summary>
    /// Creates AITools from multiple plugin instances.
    /// </summary>
    /// <param name="plugins">Dictionary of plugin name to plugin instance</param>
    /// <returns>Combined list of AITool instances from all plugins</returns>
    public static List<AITool> CreateToolsFromPlugins(Dictionary<string, object> plugins)
    {
        var allTools = new List<AITool>();

        foreach (var (pluginName, pluginInstance) in plugins)
        {
            Console.WriteLine($"📦 Registering tools from plugin: {pluginName}");

            var pluginType = pluginInstance.GetType();
            var methods = pluginType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (var method in methods)
            {
                // Skip methods from Object base class
                if (method.DeclaringType == typeof(object))
                {
                    continue;
                }

                try
                {
                    var tool = AIFunctionFactory.Create(method, pluginInstance);
                    allTools.Add(tool);

                    Console.WriteLine($"✅ Registered AF tool: {method.Name} from {pluginName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Failed to register method {method.Name} from {pluginName}: {ex.Message}");
                }
            }
        }

        Console.WriteLine($"📊 Total tools registered: {allTools.Count}");
        return allTools;
    }
}
