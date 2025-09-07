using System.Text.Json;
using System.Text.Json.Serialization;
using Knowledge.Analytics.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Knowledge.Analytics.Services;

public class GoogleAIProviderApiService : IProviderApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleAIProviderApiService> _logger;

    public string ProviderName => "Google AI";
    public bool IsConfigured => !string.IsNullOrEmpty(GetApiKey());

    public GoogleAIProviderApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GoogleAIProviderApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        ConfigureHttpClient();
    }

    public async Task<ProviderAccountInfo?> GetAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Google AI API key not configured");
            return null;
        }

        try
        {
            // Google AI Studio/Gemini API doesn't provide billing endpoints for individual API keys
            // This would require Google Cloud Console API integration
            _logger.LogInformation("Google AI billing information requires Cloud Console access");
            
            return new ProviderAccountInfo
            {
                ProviderName = ProviderName,
                AdditionalInfo = new()
                {
                    ["status"] = "API configured",
                    ["note"] = "Billing information available through Google Cloud Console",
                    ["quota_type"] = "Per-minute requests"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Google AI account information");
            return null;
        }
    }

    public async Task<ProviderUsageInfo?> GetUsageInfoAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Google AI API key not configured");
            return null;
        }

        try
        {
            // Google AI Studio API doesn't provide usage analytics endpoints
            // Usage data would need to come from Google Cloud Console APIs
            _logger.LogInformation("Google AI usage data requires Cloud Console API integration");
            
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days);

            // For now, return a placeholder response indicating the limitation
            return new ProviderUsageInfo
            {
                ProviderName = ProviderName,
                StartDate = startDate,
                EndDate = endDate,
                TotalCost = 0m,
                Currency = "USD",
                TotalRequests = 0,
                TotalTokens = 0,
                ModelBreakdown = new List<ModelUsageInfo>
                {
                    new()
                    {
                        ModelName = "gemini-pro",
                        Requests = 0,
                        InputTokens = 0,
                        OutputTokens = 0,
                        Cost = 0m
                    },
                    new()
                    {
                        ModelName = "gemini-pro-vision",
                        Requests = 0,
                        InputTokens = 0,
                        OutputTokens = 0,
                        Cost = 0m
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Google AI usage information");
            return null;
        }
    }

    private void ConfigureHttpClient()
    {
        var apiKey = GetApiKey();
        if (!string.IsNullOrEmpty(apiKey))
        {
            // Google AI uses API key as a query parameter, not header
            // We'll add it to requests dynamically
        }
    }

    private string? GetApiKey()
    {
        return Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? 
               _configuration["ChatCompleteSettings:GoogleApiKey"] ??
               _configuration["ChatCompleteSettings:GeminiApiKey"];
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}