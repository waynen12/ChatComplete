
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChatCompletion.Config;
using KnowledgeEngine.Logging;

public class AtlasIndexManager
{
    private readonly string _clusterName;
    private readonly string _databaseName;
    private readonly string _collectionName;
    private readonly string _baseUrl = SettingsProvider.Settings.Atlas.BaseUrl;
    private readonly string _indexSubUrl = "/groups/{0}/clusters/{1}/fts/indexes/{2}/{3}";
    private readonly string _commandSubUrl = "/groups/{0}/clusters/{1}/fts/indexes";

    private string _indexUrl;

    private string _commandUrl;

    private readonly HttpClient _httpClient;
    private string _projectId = string.Empty; // Store projectId if needed later

    private AtlasIndexManager(string collectionName, HttpClient httpClient, string projectId)
    {
        _clusterName = SettingsProvider.Settings.Atlas.ClusterName;
        _databaseName = SettingsProvider.Settings.Atlas.ClusterName;
        _collectionName = collectionName;
        _httpClient = httpClient; // Use the HttpClient passed in
        _projectId = projectId;

        // Set URLs now that we have the projectId
        _indexUrl = $"{_baseUrl}{string.Format(_indexSubUrl, _projectId, _clusterName, _databaseName, _collectionName)}";
        _commandUrl = $"{_baseUrl}{string.Format(_commandSubUrl, _projectId, _clusterName)}";
    }

    // Public static factory method for asynchronous creation
    public static async Task<AtlasIndexManager?> CreateAsync(string collectionName)
    {
        // --- Moved logic from original constructor ---
        var publicKey = Environment.GetEnvironmentVariable("MONGODB_PUBLIC_API_KEY") ?? "";
        var privateKey = Environment.GetEnvironmentVariable("MONGODB_API_KEY") ?? "";

        if (string.IsNullOrEmpty(publicKey))
        {
            Console.WriteLine("Error: The MongoDB public API key is not set in the environment variables.");
            LoggerProvider.Logger.Error("The MongoDB public API key is not set in the environment variables.");
            throw new InvalidOperationException("The MongoDB public API key is not set in the environment variables.");            
        }
        if (string.IsNullOrEmpty(privateKey))
        {
             Console.WriteLine("Error: The MongoDB private API key is not set in the environment variables.");
             LoggerProvider.Logger.Error("The MongoDB private API key is not set in the environment variables.");
             throw new InvalidOperationException("The MongoDB private API key is not set in the environment variables.");
        }

        var handler = new HttpClientHandler
        {
            Credentials = new NetworkCredential(publicKey, privateKey)
        };

        var httpClient = new HttpClient(handler); // Create HttpClient here
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        // --- End of moved logic ---

        // *** Perform the async operation ***
        string? projectId = await AtlasHelper.GetProjectIdByNameAsync(SettingsProvider.Settings.Atlas.ProjectName, httpClient);

        if (string.IsNullOrEmpty(projectId))
        {
            Console.WriteLine($"Error: Could not retrieve Project ID for project '{SettingsProvider.Settings.Atlas.ProjectName}'. Cannot create AtlasIndexManager.");
            LoggerProvider.Logger.Error($"Could not retrieve Project ID for project '{SettingsProvider.Settings.Atlas.ProjectName}'. Cannot create AtlasIndexManager.");
            httpClient.Dispose(); // Dispose HttpClient if creation fails
            return null; // Indicate failure
        }

        // Call the private constructor with the HttpClient and projectId
        return new AtlasIndexManager(collectionName, httpClient, projectId);
    }


    public async Task<bool> IndexExistsAsync()
    {
        var response = await _httpClient.GetAsync(_indexUrl);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to retrieve indexes. Status: {response.StatusCode}");
            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine(error);
            LoggerProvider.Logger.Error($"Failed to retrieve indexes. Status: {response.StatusCode}");
            LoggerProvider.Logger.Error(error);
            return false;
        }

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        foreach (var index in doc.RootElement.EnumerateArray())
        {
            if (index.GetProperty("collectionName").GetString() == _collectionName &&
                index.GetProperty("database").GetString() == _databaseName &&
                index.GetProperty("name").GetString() == SettingsProvider.Settings.Atlas.SearchIndexName)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<string?> GetIndexIdAsync()
    {
        var response = await _httpClient.GetAsync(_indexUrl);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to retrieve indexes. Status: {response.StatusCode}");
            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine(error);
            LoggerProvider.Logger.Error($"Failed to retrieve indexes. Status: {response.StatusCode}");
            LoggerProvider.Logger.Error(error);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        foreach (var index in doc.RootElement.EnumerateArray())
        {
            if (index.GetProperty("collectionName").GetString() == _collectionName &&
                index.GetProperty("database").GetString() == _databaseName &&
                index.GetProperty("name").GetString() == SettingsProvider.Settings.Atlas.SearchIndexName)
            {
                return index.GetProperty("indexID").GetString();
            }
        }

        return null;
    }

    public async Task<string?> GetProjectIdByNameAsync(string projectName)
    {
        string url = $"{_baseUrl}/groups";
        var response = await _httpClient.GetAsync(MongoConstants.AtlasProjectUrl);

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


    public async Task CreateVectorSearchIndexAsync(string vectorField, int numDimensions, string similarityFunction)
    {
        if (await IndexExistsAsync())
        {
            Console.WriteLine($"Index '{SettingsProvider.Settings.Atlas.SearchIndexName}' already exists. Skipping creation.");
            LoggerProvider.Logger.Information($"Index '{SettingsProvider.Settings.Atlas.SearchIndexName}' already exists. Skipping creation.");
            return;
        }

       var body = new
        {
            collectionName = _collectionName,
            database = _databaseName,
            name = SettingsProvider.Settings.Atlas.SearchIndexName,
            mappings = new
            {
                dynamic = false,
                fields = new Dictionary<string, object>
                {
                    [vectorField] = new
                    {
                        type = "knnVector",  // <<< IMPORTANT: knnVector, NOT vector
                        dimensions = numDimensions,
                        similarity = similarityFunction
                    }
                }
            }
        };


        string json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(_commandUrl, content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Successfully created index '{SettingsProvider.Settings.Atlas.SearchIndexName}'.");
            LoggerProvider.Logger.Information($"Successfully created index '{SettingsProvider.Settings.Atlas.SearchIndexName}'.");
        }
        else
        {
            Console.WriteLine($"Failed to create index '{SettingsProvider.Settings.Atlas.SearchIndexName}'. Status: {response.StatusCode}");
            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine(error);
            LoggerProvider.Logger.Error($"Failed to create index '{SettingsProvider.Settings.Atlas.SearchIndexName}'. Status: {response.StatusCode}");
            LoggerProvider.Logger.Error(error);
        }
    }

    
    public async Task CreateIndexAsync()
    {
        if (await IndexExistsAsync())
        {
            Console.WriteLine($"Index '{SettingsProvider.Settings.Atlas.SearchIndexName}' already exists. Skipping creation.");
            LoggerProvider.Logger.Information($"Index '{SettingsProvider.Settings.Atlas.SearchIndexName}' already exists. Skipping creation.");
            return;
        }
        await CreateVectorSearchIndexAsync("embedding", 1536, "cosine");
    }
    
    public async Task DeleteIndexAsync()
    {
        if (!await IndexExistsAsync())
        {
            Console.WriteLine($"Index '{SettingsProvider.Settings.Atlas.SearchIndexName}' does not exist. Cannot delete.");
            LoggerProvider.Logger.Information($"Index '{SettingsProvider.Settings.Atlas.SearchIndexName}' does not exist. Cannot delete.");
            return;
        }

        string? indexId = await GetIndexIdAsync();
        if (indexId == null)
        {
            Console.WriteLine($"Failed to retrieve index ID for '{SettingsProvider.Settings.Atlas.SearchIndexName}'. Cannot delete.");
            LoggerProvider.Logger.Error($"Failed to retrieve index ID for '{SettingsProvider.Settings.Atlas.SearchIndexName}'. Cannot delete.");
            return;
        }
       // string deleteBaseUrl = $"{_baseUrl}/groups/{_projectId}/clusters/{_clusterName}/fts/indexes";
        string deleteUrl = $"{_commandUrl}/{indexId}";
        var response = await _httpClient.DeleteAsync(deleteUrl);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Successfully deleted index '{SettingsProvider.Settings.Atlas.SearchIndexName}'.");
            LoggerProvider.Logger.Information($"Successfully deleted index '{SettingsProvider.Settings.Atlas.SearchIndexName}'.");
        }
        else
        {
            Console.WriteLine($"Failed to delete index '{SettingsProvider.Settings.Atlas.SearchIndexName}'. Status: {response.StatusCode}");
            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine(error);
            LoggerProvider.Logger.Error($"Failed to delete index '{SettingsProvider.Settings.Atlas.SearchIndexName}'. Status: {response.StatusCode}");
            LoggerProvider.Logger.Error(error);
        }
    }

}
