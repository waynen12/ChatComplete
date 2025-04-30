
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class AtlasIndexManager
{
    private readonly string _projectId;
    private readonly string _clusterName;
    private readonly string _databaseName;
    private readonly string _collectionName;
    private readonly string _publicKey;
    private readonly string _privateKey;
    private readonly string _baseUrl = "https://cloud.mongodb.com/api/atlas/v1.0";
    private readonly string _indexSubUrl = "/groups/{0}/clusters/{1}/fts/indexes/{2}/{3}";
    private readonly string _commandSubUrl = "/groups/{0}/clusters/{1}/fts/indexes";

    private string _indexUrl;

    private string _commandUrl;

    private readonly HttpClient _httpClient;

    public AtlasIndexManager(string projectId, string clusterName, string databaseName, string collectionName)
{
    _projectId = projectId;
    _clusterName = clusterName;
    _databaseName = databaseName;
    _collectionName = collectionName;
    _publicKey = Environment.GetEnvironmentVariable("MONGODB_PUBLIC_API_KEY") ?? "";
    _privateKey = Environment.GetEnvironmentVariable("MONGODB_API_KEY") ?? "";

    if (string.IsNullOrEmpty(_publicKey))
    {
        throw new InvalidOperationException("The MongoDB public API key is not set in the environment variables.");
    }
    if (string.IsNullOrEmpty(_privateKey))
    {
        throw new InvalidOperationException("The MongoDB private API key is not set in the environment variables.");
    }

    var handler = new HttpClientHandler
    {
        Credentials = new NetworkCredential(_publicKey, _privateKey)
    };

    _httpClient = new HttpClient(handler);
    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    _indexUrl = $"{_baseUrl}{string.Format(_indexSubUrl, _projectId, _clusterName, _databaseName, _collectionName)}";
    _commandUrl = $"{_baseUrl}{string.Format(_commandSubUrl, _projectId, _clusterName)}";
}


    public async Task<bool> IndexExistsAsync(string indexName)
    {
        var response = await _httpClient.GetAsync(_indexUrl);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to retrieve indexes. Status: {response.StatusCode}");
            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine(error);
            return false;
        }

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        foreach (var index in doc.RootElement.EnumerateArray())
        {
            if (index.GetProperty("collectionName").GetString() == _collectionName &&
                index.GetProperty("database").GetString() == _databaseName &&
                index.GetProperty("name").GetString() == indexName)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<string?> GetIndexIdAsync(string indexName)
    {
        var response = await _httpClient.GetAsync(_indexUrl);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to retrieve indexes. Status: {response.StatusCode}");
            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine(error);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        foreach (var index in doc.RootElement.EnumerateArray())
        {
            if (index.GetProperty("collectionName").GetString() == _collectionName &&
                index.GetProperty("database").GetString() == _databaseName &&
                index.GetProperty("name").GetString() == indexName)
            {
                return index.GetProperty("indexID").GetString();
            }
        }

        return null;
    }

    public async Task CreateVectorSearchIndexAsync(string indexName, string vectorField, int numDimensions, string similarityFunction)
    {
        if (await IndexExistsAsync(indexName))
        {
            Console.WriteLine($"Index '{indexName}' already exists. Skipping creation.");
            return;
        }

       var body = new
        {
            collectionName = _collectionName,
            database = _databaseName,
            name = indexName,
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
            Console.WriteLine($"Successfully created index '{indexName}'.");
        }
        else
        {
            Console.WriteLine($"Failed to create index '{indexName}'. Status: {response.StatusCode}");
            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine(error);
        }
    }

    
    public async Task CreateIndexAsync(string indexName)
    {
        if (await IndexExistsAsync(indexName))
        {
            await DeleteIndexAsync(indexName);
        }
        await CreateVectorSearchIndexAsync(indexName, "embedding", 1536, "cosine");
    }
    
    public async Task DeleteIndexAsync(string indexName)
    {
        if (!await IndexExistsAsync(indexName))
        {
            Console.WriteLine($"Index '{indexName}' does not exist. Cannot delete.");
            return;
        }

        string? indexId = await GetIndexIdAsync(indexName);
        if (indexId == null)
        {
            Console.WriteLine($"Failed to retrieve index ID for '{indexName}'. Cannot delete.");
            return;
        }
       // string deleteBaseUrl = $"{_baseUrl}/groups/{_projectId}/clusters/{_clusterName}/fts/indexes";
        string deleteUrl = $"{_commandUrl}/{indexId}";
        var response = await _httpClient.DeleteAsync(deleteUrl);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Successfully deleted index '{indexName}'.");
        }
        else
        {
            Console.WriteLine($"Failed to delete index '{indexName}'. Status: {response.StatusCode}");
            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine(error);
        }
    }

}
