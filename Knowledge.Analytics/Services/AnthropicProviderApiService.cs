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

            // For admin keys, we can access the usage/cost endpoints
            // Note: Anthropic doesn't have a direct balance endpoint, but we can get cost data
            var response = await _httpClient.GetAsync("v1/organizations/usage_report/messages", cancellationToken);
            
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

            // Use Anthropic's Usage & Cost API endpoints
            
            // Get usage data from messages endpoint
            var usageResponse = await _httpClient.GetAsync("v1/organizations/usage_report/messages", cancellationToken);
            
            // Get cost data from cost report endpoint
            var costResponse = await _httpClient.GetAsync("v1/organizations/cost_report", cancellationToken);
            
            if (usageResponse.IsSuccessStatusCode && costResponse.IsSuccessStatusCode)
            {
                var usageContent = await usageResponse.Content.ReadAsStringAsync(cancellationToken);
                var costContent = await costResponse.Content.ReadAsStringAsync(cancellationToken);
                
                var usageData = JsonSerializer.Deserialize<AnthropicUsageResponse>(usageContent, JsonOptions);
                var costData = JsonSerializer.Deserialize<AnthropicCostResponse>(costContent, JsonOptions);
                
                // Process the data (this is simplified - real implementation would parse the JSON properly)
                var totalCost = costData?.TotalCost ?? 0m;
                var totalRequests = usageData?.TotalRequests ?? 0;
                var totalTokens = (usageData?.TotalInputTokens ?? 0) + (usageData?.TotalOutputTokens ?? 0);
                
                return new ProviderApiUsageInfo
                {
                    ProviderName = ProviderName,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalCost = totalCost,
                    Currency = "USD",
                    TotalRequests = totalRequests,
                    TotalTokens = totalTokens,
                    ModelBreakdown = usageData?.ModelBreakdown?.Select(m => new ModelUsageInfo
                    {
                        ModelName = m.ModelName ?? "claude-3-sonnet",
                        Requests = m.Requests,
                        InputTokens = m.InputTokens,
                        OutputTokens = m.OutputTokens,
                        Cost = m.Cost
                    }).ToList() ?? new List<ModelUsageInfo>()
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
    public int TotalRequests { get; init; }
    public int TotalInputTokens { get; init; }
    public int TotalOutputTokens { get; init; }
    public List<AnthropicModelUsage> ModelBreakdown { get; init; } = new();
}

internal record AnthropicCostResponse
{
    public decimal TotalCost { get; init; }
}

internal record AnthropicModelUsage
{
    public string? ModelName { get; init; }
    public int Requests { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public decimal Cost { get; init; }
}