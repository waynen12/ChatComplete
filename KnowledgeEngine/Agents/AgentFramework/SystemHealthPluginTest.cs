using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using KnowledgeEngine.Services;

namespace KnowledgeEngine.Agents.AgentFramework;

/// <summary>
/// Simple test class to verify SystemHealthPlugin tool registration works correctly.
/// Run this to confirm all 6 functions are discovered and registered as AF tools.
/// </summary>
public static class SystemHealthPluginTest
{
    public static void TestToolRegistration(IServiceProvider serviceProvider)
    {
        Console.WriteLine("\n🧪 ========== SystemHealthPlugin Tool Registration Test ==========\n");

        try
        {
            // Create a scope to resolve scoped services
            using var scope = serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            // Get dependencies from scoped provider
            var healthService = scopedProvider.GetRequiredService<ISystemHealthService>();
            var logger = scopedProvider.GetRequiredService<ILogger<SystemHealthPlugin>>();

            // Create plugin instance
            var plugin = new SystemHealthPlugin(healthService, logger);
            Console.WriteLine("✅ SystemHealthPlugin instance created");

            // Register tools using reflection
            var tools = AgentToolRegistration.CreateToolsFromPlugin(plugin);

            Console.WriteLine($"\n📊 Tool Registration Results:");
            Console.WriteLine($"   Total tools registered: {tools.Count}");
            Console.WriteLine($"   Expected: 6 functions");

            if (tools.Count == 6)
            {
                Console.WriteLine("   ✅ SUCCESS: All 6 functions registered!");
            }
            else
            {
                Console.WriteLine($"   ⚠️  WARNING: Expected 6 tools, got {tools.Count}");
            }

            Console.WriteLine($"\n📋 Registered Tools:");
            for (int i = 0; i < tools.Count; i++)
            {
                Console.WriteLine($"   - Tool #{i + 1}");
            }

            Console.WriteLine("\n✅ SystemHealthPlugin test complete!\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Test failed: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}\n");
        }
    }
}
