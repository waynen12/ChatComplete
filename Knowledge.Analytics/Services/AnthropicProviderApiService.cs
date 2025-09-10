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
            _logger.LogDebug("Attempting to get Anthropic account info");
            
            // Check if we have an admin API key (required for billing endpoints)
            var apiKey = GetApiKey();
            var isAdminKey = apiKey?.StartsWith("sk-ant-admin") == true;
            
            if (!isAdminKey)
            {
                _logger.LogInformation("Anthropic billing data requires Admin API key (starts with sk-ant-admin)");
                return new ProviderApiAccountInfo
                {
                    ProviderName = ProviderName,
                    AccountId = "anthropic-account",
                    Balance = null, // Regular API keys can't access billing
                    Currency = "USD",
                    PlanType = "API",
                    AdditionalInfo = new()
                    {
                        ["api_status"] = "connected",
                        ["is_connected"] = true,
                        ["note"] = "Billing data requires Admin API key (sk-ant-admin...)",
                        ["billing_access"] = false
                    }
                };
            }

            // For admin keys, we can test access with a minimal usage report request
            // Use a small date range to test connectivity without retrieving too much data
            var testStartDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");
            var testEndDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var testUrl = $"v1/organizations/usage_report/messages?starting_at={testStartDate}&ending_at={testEndDate}&bucket_width=1d&limit=1";
            var response = await _httpClient.GetAsync(testUrl, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return new ProviderApiAccountInfo
                {
                    ProviderName = ProviderName,
                    AccountId = "anthropic-admin-account",
                    Balance = null, // Anthropic uses prepaid credits, no balance endpoint
                    Currency = "USD",
                    PlanType = "Admin API",
                    AdditionalInfo = new()
                    {
                        ["api_status"] = "connected",
                        ["is_connected"] = true,
                        ["billing_access"] = true,
                        ["admin_key"] = true,
                        ["note"] = "Admin API key can access usage and cost data"
                    }
                };
            }
            else
            {
                _logger.LogWarning("Failed to access Anthropic usage endpoint: {StatusCode}", response.StatusCode);
                return new ProviderApiAccountInfo
                {
                    ProviderName = ProviderName,
                    AccountId = "anthropic-account",
                    Balance = null,
                    Currency = "USD",
                    PlanType = "API",
                    AdditionalInfo = new()
                    {
                        ["api_status"] = "error",
                        ["is_connected"] = false,
                        ["error"] = $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
                    }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Anthropic account information");
            return new ProviderApiAccountInfo
            {
                ProviderName = ProviderName,
                AccountId = "anthropic-account",
                Balance = null,
                Currency = "USD",
                PlanType = "API",
                AdditionalInfo = new()
                {
                    ["api_status"] = "error",
                    ["is_connected"] = false,
                    ["error"] = ex.Message
                }
            };
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
            var apiKey = GetApiKey();
            var isAdminKey = apiKey?.StartsWith("sk-ant-admin") == true;
            
            // Calculate date range for both admin and non-admin cases
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days);
            
            if (!isAdminKey)
            {
                _logger.LogInformation("Anthropic usage data requires Admin API key (starts with sk-ant-admin)");
                
                return new ProviderApiUsageInfo
                {
                    ProviderName = ProviderName,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalCost = 0m,
                    Currency = "USD",
                    TotalRequests = 0,
                    TotalTokens = 0,
                    ModelBreakdown = new List<ModelUsageInfo>()
                };
            }

            // Use Anthropic's Usage & Cost API endpoints with required parameters
            var startDateIso = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var endDateIso = endDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
            
            // Get usage data from messages endpoint with required parameters
            var usageUrl = $"v1/organizations/usage_report/messages?starting_at={startDateIso}&ending_at={endDateIso}&bucket_width=1d";
            var usageResponse = await _httpClient.GetAsync(usageUrl, cancellationToken);
            
            // Get cost data from cost report endpoint with required parameters
            var costUrl = $"v1/organizations/cost_report?starting_at={startDateIso}&ending_at={endDateIso}";
            var costResponse = await _httpClient.GetAsync(costUrl, cancellationToken);
            
            if (usageResponse.IsSuccessStatusCode && costResponse.IsSuccessStatusCode)
            {
                var usageContent = await usageResponse.Content.ReadAsStringAsync(cancellationToken);
                var costContent = await costResponse.Content.ReadAsStringAsync(cancellationToken);
                
                var usageData = JsonSerializer.Deserialize<AnthropicUsageResponse>(usageContent, JsonOptions);
                var costData = JsonSerializer.Deserialize<AnthropicCostResponse>(costContent, JsonOptions);
                
                // Aggregate usage data
                var allUsageResults = usageData?.Data?.SelectMany(d => d.Results) ?? new List<AnthropicUsageResult>();
                var totalInputTokens = allUsageResults.Sum(r => r.UncachedInputTokens + r.CacheReadInputTokens + 
                    (r.CacheCreation?.Ephemeral1hInputTokens ?? 0) + (r.CacheCreation?.Ephemeral5mInputTokens ?? 0));
                var totalOutputTokens = allUsageResults.Sum(r => r.OutputTokens);
                var totalWebSearchRequests = allUsageResults.Sum(r => r.ServerToolUse?.WebSearchRequests ?? 0);
                
                // Aggregate cost data
                var allCostResults = costData?.Data?.SelectMany(d => d.Results) ?? new List<AnthropicCostResult>();
                var totalCost = allCostResults.Sum(r => r.Amount);
                
                // Group by model for breakdown
                var modelGroups = allUsageResults.GroupBy(r => r.Model ?? "claude-unknown");
                var modelBreakdown = new List<ModelUsageInfo>();
                
                foreach (var modelGroup in modelGroups)
                {
                    var modelName = modelGroup.Key;
                    var modelUsageResults = modelGroup.ToList();
                    var modelCostResults = allCostResults.Where(c => c.Model == modelName).ToList();
                    
                    var modelInputTokens = modelUsageResults.Sum(r => r.UncachedInputTokens + r.CacheReadInputTokens + 
                        (r.CacheCreation?.Ephemeral1hInputTokens ?? 0) + (r.CacheCreation?.Ephemeral5mInputTokens ?? 0));
                    var modelOutputTokens = modelUsageResults.Sum(r => r.OutputTokens);
                    var modelCost = modelCostResults.Sum(r => r.Amount);
                    var modelRequests = modelUsageResults.Count;
                    
                    modelBreakdown.Add(new ModelUsageInfo
                    {
                        ModelName = modelName,
                        Requests = modelRequests,
                        InputTokens = modelInputTokens,
                        OutputTokens = modelOutputTokens,
                        Cost = modelCost
                    });
                }
                
                return new ProviderApiUsageInfo
                {
                    ProviderName = ProviderName,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalCost = totalCost,
                    Currency = "USD",
                    TotalRequests = allUsageResults.Count(),
                    TotalTokens = totalInputTokens + totalOutputTokens,
                    ModelBreakdown = modelBreakdown,
                    AdditionalInfo = new()
                    {
                        ["total_input_tokens"] = totalInputTokens,
                        ["total_output_tokens"] = totalOutputTokens,
                        ["web_search_requests"] = totalWebSearchRequests,
                        ["unique_models"] = modelGroups.Count(),
                        ["cost_breakdown_available"] = allCostResults.Any()
                    }
                };
            }
            else
            {
                _logger.LogWarning("Failed to get Anthropic usage/cost data: Usage={UsageStatus}, Cost={CostStatus}", 
                    usageResponse.StatusCode, costResponse.StatusCode);
                
                return new ProviderApiUsageInfo
                {
                    ProviderName = ProviderName,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalCost = 0m,
                    Currency = "USD",
                    TotalRequests = 0,
                    TotalTokens = 0,
                    ModelBreakdown = new List<ModelUsageInfo>()
                };
            }
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

// Anthropic API Response Models
internal record AnthropicUsageResponse
{
    public List<AnthropicUsageData> Data { get; init; } = new();
    public bool HasMore { get; init; }
    public string? NextPage { get; init; }
}

internal record AnthropicUsageData
{
    public DateTime StartingAt { get; init; }
    public DateTime EndingAt { get; init; }
    public List<AnthropicUsageResult> Results { get; init; } = new();
}

internal record AnthropicUsageResult
{
    public int UncachedInputTokens { get; init; }
    public AnthropicCacheCreation? CacheCreation { get; init; }
    public int CacheReadInputTokens { get; init; }
    public int OutputTokens { get; init; }
    public AnthropicServerToolUse? ServerToolUse { get; init; }
    public string? ApiKeyId { get; init; }
    public string? WorkspaceId { get; init; }
    public string? Model { get; init; }
    public string? ServiceTier { get; init; }
    public string? ContextWindow { get; init; }
}

internal record AnthropicCacheCreation
{
    public int Ephemeral1hInputTokens { get; init; }
    public int Ephemeral5mInputTokens { get; init; }
}

internal record AnthropicServerToolUse
{
    public int WebSearchRequests { get; init; }
}

internal record AnthropicCostResponse
{
    public List<AnthropicCostData> Data { get; init; } = new();
    public bool HasMore { get; init; }
    public string? NextPage { get; init; }
}

internal record AnthropicCostData
{
    public DateTime StartingAt { get; init; }
    public DateTime EndingAt { get; init; }
    public List<AnthropicCostResult> Results { get; init; } = new();
}

internal record AnthropicCostResult
{
    public string Currency { get; init; } = "USD";
    public decimal Amount { get; init; }
    public string? WorkspaceId { get; init; }
    public string? Description { get; init; }
    public string? CostType { get; init; }
    public string? ContextWindow { get; init; }
    public string? Model { get; init; }
    public string? ServiceTier { get; init; }
    public string? TokenType { get; init; }
}