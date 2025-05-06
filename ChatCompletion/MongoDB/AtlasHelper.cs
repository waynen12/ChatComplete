using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class AtlasHelper
{
    public static string? GetProjectIdByNameAsync(string projectName, HttpClient httpClient)
    {
     //   string url = $"{MongoConstants}/groups";
        var response = httpClient.GetAsync(MongoConstants.AtlasProjectUrl).Result;

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to retrieve project list. Status: {response.StatusCode}");
            string error = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine(error);
            return null;
        }

        var content = response.Content.ReadAsStringAsync().Result;
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