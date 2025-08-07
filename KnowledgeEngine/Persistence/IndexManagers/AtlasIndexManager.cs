
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChatCompletion.Config;
using KnowledgeEngine.Logging;
using KnowledgeEngine.MongoDB;
using Microsoft.Extensions.Options;
using Serilog;

namespace KnowledgeEngine.Persistence.IndexManagers;

public class AtlasIndexManager : IIndexManager, IDisposable
{
    private readonly MongoAtlasSettings _atlasSettings;
    private readonly HttpClient _httpClient;
    private string _projectId;
    private readonly bool _ownsHttpClient;
    
    private readonly string _indexSubUrl = "/groups/{0}/clusters/{1}/fts/indexes/{2}/{3}";
    private readonly string _commandSubUrl = "/groups/{0}/clusters/{1}/fts/indexes";

    public AtlasIndexManager(IOptions<MongoAtlasSettings> atlasSettings, HttpClient httpClient)
        : this(atlasSettings.Value, httpClient, false)
    {
    }

    public AtlasIndexManager(MongoAtlasSettings atlasSettings, HttpClient httpClient, bool ownsHttpClient = false)
    {
        _atlasSettings = atlasSettings ?? throw new ArgumentNullException(nameof(atlasSettings));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsHttpClient = ownsHttpClient;
        _projectId = string.Empty; // Will be set via initialization
    }

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(_projectId))
        {
            var projectId = await AtlasHelper.GetProjectIdByNameAsync(_atlasSettings.ProjectName, _httpClient);
            if (string.IsNullOrEmpty(projectId))
            {
                throw new InvalidOperationException($"Could not retrieve Project ID for project '{_atlasSettings.ProjectName}'");
            }
            _projectId = projectId;
        }
    }


    public async Task<bool> IndexExistsAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        var indexUrl = BuildIndexUrl(collectionName);
        var response = await _httpClient.GetAsync(indexUrl, cancellationToken);

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
            if (index.GetProperty("collectionName").GetString() == collectionName &&
                index.GetProperty("database").GetString() == _atlasSettings.DatabaseName &&
                index.GetProperty("name").GetString() == _atlasSettings.SearchIndexName)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<string?> GetIndexIdAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        var indexUrl = BuildIndexUrl(collectionName);
        var response = await _httpClient.GetAsync(indexUrl, cancellationToken);

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
            if (index.GetProperty("collectionName").GetString() == collectionName &&
                index.GetProperty("database").GetString() == _atlasSettings.DatabaseName &&
                index.GetProperty("name").GetString() == _atlasSettings.SearchIndexName)
            {
                return index.GetProperty("indexID").GetString();
            }
        }

        return null;
    }



    public async Task CreateVectorSearchIndexAsync(string vectorField, int numDimensions, string similarityFunction, string collectionName, CancellationToken cancellationToken = default)
    {
        if (await IndexExistsAsync(collectionName, cancellationToken))
        {
            LoggerProvider.Logger.Information($"Index '{_atlasSettings.SearchIndexName}' already exists for collection '{collectionName}'. Skipping creation.");
            return;
        }

       var body = new
        {
            collectionName = collectionName,
            database = _atlasSettings.DatabaseName,
            name = _atlasSettings.SearchIndexName,
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
        var commandUrl = BuildCommandUrl();
        var response = await _httpClient.PostAsync(commandUrl, content, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            LoggerProvider.Logger.Information($"Successfully created index '{_atlasSettings.SearchIndexName}'.");
        }
        else
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            LoggerProvider.Logger.Error($"Failed to create index '{_atlasSettings.SearchIndexName}'. Status: {response.StatusCode}. Error: {error}");
            throw new InvalidOperationException($"Failed to create Atlas search index: {response.StatusCode}");
        }
    }

    
    public async Task CreateIndexAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        if (await IndexExistsAsync(collectionName, cancellationToken))
        {
            LoggerProvider.Logger.Information($"Index '{_atlasSettings.SearchIndexName}' already exists for collection '{collectionName}'. Skipping creation.");
            return;
        }
        await CreateVectorSearchIndexAsync(_atlasSettings.VectorField, _atlasSettings.NumDimensions, _atlasSettings.SimilarityFunction, collectionName, cancellationToken);
    }
    
    public async Task DeleteIndexAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        if (!await IndexExistsAsync(collectionName, cancellationToken))
        {
            LoggerProvider.Logger.Information($"Index '{_atlasSettings.SearchIndexName}' does not exist for collection '{collectionName}'. Cannot delete.");
            return;
        }

        string? indexId = await GetIndexIdAsync(collectionName, cancellationToken);
        if (indexId == null)
        {
            LoggerProvider.Logger.Error($"Failed to retrieve index ID for '{_atlasSettings.SearchIndexName}'. Cannot delete.");
            throw new InvalidOperationException($"Failed to retrieve index ID for '{_atlasSettings.SearchIndexName}'");
        }
        var commandUrl = BuildCommandUrl();
        string deleteUrl = $"{commandUrl}/{indexId}";
        var response = await _httpClient.DeleteAsync(deleteUrl, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            LoggerProvider.Logger.Information($"Successfully deleted index '{_atlasSettings.SearchIndexName}'.");
        }
        else
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            LoggerProvider.Logger.Error($"Failed to delete index '{_atlasSettings.SearchIndexName}'. Status: {response.StatusCode}. Error: {error}");
            throw new InvalidOperationException($"Failed to delete Atlas search index: {response.StatusCode}");
        }
    }

    private string BuildIndexUrl(string collectionName)
    {
        return $"{_atlasSettings.BaseUrl}{string.Format(_indexSubUrl, _projectId, _atlasSettings.ClusterName, _atlasSettings.DatabaseName, collectionName)}";
    }

    private string BuildCommandUrl()
    {
        return $"{_atlasSettings.BaseUrl}{string.Format(_commandSubUrl, _projectId, _atlasSettings.ClusterName)}";
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient?.Dispose();
        }
    }
}
