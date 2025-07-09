using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using KnowledgeEngine.Logging;

public static class AtlasHelper
{
    public static async Task<string?> GetProjectIdByNameAsync(string projectName, HttpClient httpClient)
    {
        var response = await httpClient.GetAsync(MongoConstants.AtlasProjectUrl);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to retrieve project list. Status: {response.StatusCode}");
            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine(error);
            LoggerProvider.Logger.Error($"Failed to retrieve project list. Status: {response.StatusCode}");
            LoggerProvider.Logger.Error(error);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        foreach (var project in doc.RootElement.GetProperty("results").EnumerateArray())
        {
            if (project.GetProperty("name").GetString()?.Equals(projectName, StringComparison.OrdinalIgnoreCase) == true)
            {
                return project.GetProperty("id").GetString(); // This is the Group ID (aka Project ID)
            }
        }

        Console.WriteLine($"Project '{projectName}' not found.");
        return null;
    }
    
}