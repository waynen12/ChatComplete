using System.Text.Json;
using System.Text.Json.Serialization;
using Knowledge.Analytics.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Knowledge.Analytics.Services;

public class OpenAIProviderApiService : IProviderApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIProviderApiService> _logger;

    public string ProviderName => "OpenAI";
    public bool IsConfigured => !string.IsNullOrEmpty(GetApiKey());

    public OpenAIProviderApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAIProviderApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        ConfigureHttpClient();
    }

    public async Task<ProviderAccountInfo?> GetAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("OpenAI API key not configured");
            return null;
        }

        try
        {
            // OpenAI doesn't have a direct account info endpoint, so we'll use organization details
            var response = await _httpClient.GetAsync("v1/dashboard/billing/subscription", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get OpenAI account info: {StatusCode} {Reason}", 
                    response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var subscription = JsonSerializer.Deserialize<OpenAISubscription>(content, JsonOptions);

            return new ProviderAccountInfo
            {
                ProviderName = ProviderName,
                AccountId = subscription?.Organization?.Id,
                Balance = subscription?.HardLimitUsd,
                Currency = "USD",
                PlanType = subscription?.PlanType,
                AdditionalInfo = new()
                {
                    ["max_balance"] = subscription?.MaxUsageUsd ?? 0m,
                    ["organization_name"] = subscription?.Organization?.Name ?? "",
                    ["is_delinquent"] = subscription?.AccessUntil.HasValue ?? false
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving OpenAI account information");
            return null;
        }
    }

    public async Task<ProviderUsageInfo?> GetUsageInfoAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("OpenAI API key not configured");
            return null;
        }

        try
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days);

            // OpenAI usage endpoint format: https://api.openai.com/v1/dashboard/billing/usage
            var url = $"v1/dashboard/billing/usage?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get OpenAI usage info: {StatusCode} {Reason}", 
                    response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var usage = JsonSerializer.Deserialize<OpenAIUsage>(content, JsonOptions);

            if (usage == null)
                return null;

            var modelBreakdown = usage.DailyUsage
                .SelectMany(d => d.LineItems)
                .GroupBy(item => item.Name)
                .Select(g => new ModelUsageInfo
                {
                    ModelName = g.Key,
                    Requests = g.Sum(x => x.Quantity ?? 0),
                    Cost = g.Sum(x => x.Cost) / 100m // OpenAI returns cost in cents
                })
                .ToList();

            return new ProviderUsageInfo
            {
                ProviderName = ProviderName,
                StartDate = startDate,
                EndDate = endDate,
                TotalCost = usage.TotalUsage / 100m, // Convert from cents
                Currency = "USD",
                TotalRequests = modelBreakdown.Sum(m => m.Requests),
                ModelBreakdown = modelBreakdown
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving OpenAI usage information");
            return null;
        }
    }

    private void ConfigureHttpClient()
    {
        var apiKey = GetApiKey();
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    private string? GetApiKey()
    {
        return Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
               _configuration["ChatCompleteSettings:OpenAiApiKey"];
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

// OpenAI API Response Models
internal record OpenAISubscription
{
    public string? PlanType { get; init; }
    public decimal? HardLimitUsd { get; init; }
    public decimal? MaxUsageUsd { get; init; }
    public DateTime? AccessUntil { get; init; }
    public OpenAIOrganization? Organization { get; init; }
}

internal record OpenAIOrganization
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

internal record OpenAIUsage
{
    public decimal TotalUsage { get; init; }
    public List<OpenAIDailyUsage> DailyUsage { get; init; } = new();
}

internal record OpenAIDailyUsage
{
    public DateTime Timestamp { get; init; }
    public List<OpenAILineItem> LineItems { get; init; } = new();
}

internal record OpenAILineItem
{
    public string Name { get; init; } = string.Empty;
    public decimal Cost { get; init; }
    public int? Quantity { get; init; }
}