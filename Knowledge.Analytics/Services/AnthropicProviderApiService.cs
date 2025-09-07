using System.Text.Json;
using System.Text.Json.Serialization;
using Knowledge.Analytics.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Knowledge.Analytics.Services;

public class AnthropicProviderApiService : IProviderApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnthropicProviderApiService> _logger;

    public string ProviderName => "Anthropic";
    public bool IsConfigured => !string.IsNullOrEmpty(GetApiKey());

    public AnthropicProviderApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AnthropicProviderApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri("https://api.anthropic.com/");
        ConfigureHttpClient();
    }

    public async Task<ProviderApiAccountInfo?> GetAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Anthropic API key not configured");
            return null;
        }

        try
        {
            // Anthropic doesn't currently provide billing/account endpoints in their public API
            // This would need to be updated when Anthropic adds these features
            _logger.LogInformation("Anthropic account info endpoint not yet available in public API");
            
            return new ProviderApiAccountInfo
            {
                ProviderName = ProviderName,
                AdditionalInfo = new()
                {
                    ["status"] = "API configured but billing endpoint not available",
                    ["note"] = "Anthropic does not currently provide public billing APIs"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Anthropic account information");
            return null;
        }
    }

    public async Task<ProviderApiUsageInfo?> GetUsageInfoAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Anthropic API key not configured");
            return null;
        }

        try
        {
            // Anthropic doesn't currently provide usage/billing endpoints in their public API
            // This is a placeholder implementation that would need to be updated
            // when Anthropic provides these endpoints
            
            _logger.LogInformation("Anthropic usage tracking endpoint not yet available in public API");
            
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days);

            return new ProviderApiUsageInfo
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
                        ModelName = "claude-3-sonnet",
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
            _logger.LogError(ex, "Error retrieving Anthropic usage information");
            return null;
        }
    }

    private void ConfigureHttpClient()
    {
        var apiKey = GetApiKey();
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }
    }

    private string? GetApiKey()
    {
        return Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? 
               _configuration["ChatCompleteSettings:AnthropicApiKey"];
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}