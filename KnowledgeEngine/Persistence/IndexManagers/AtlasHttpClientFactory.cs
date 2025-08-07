using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace KnowledgeEngine.Persistence.IndexManagers;

public class AtlasHttpClientFactory
{
    public static HttpClient CreateHttpClient()
    {
        var publicKey = Environment.GetEnvironmentVariable("MONGODB_PUBLIC_API_KEY")
            ?? throw new InvalidOperationException("MONGODB_PUBLIC_API_KEY environment variable is not set");
        
        var privateKey = Environment.GetEnvironmentVariable("MONGODB_API_KEY")
            ?? throw new InvalidOperationException("MONGODB_API_KEY environment variable is not set");

        var handler = new HttpClientHandler
        {
            Credentials = new NetworkCredential(publicKey, privateKey)
        };

        var httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        return httpClient;
    }
}