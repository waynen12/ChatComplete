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
        ILogger<OpenAIProviderApiService> logger
    )
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        ConfigureHttpClient();
    }

    public async Task<ProviderApiAccountInfo?> GetAccountInfoAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("OpenAI API key not configured");
            return null;
        }

        try
        {
            _logger.LogDebug(
                "Attempting to connect to OpenAI API at {BaseAddress}",
                _httpClient.BaseAddress
            );

            // Use the models endpoint to verify API key is working and get organization info
            var response = await _httpClient.GetAsync("v1/models", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get OpenAI account info: {StatusCode} {Reason}",
                    response.StatusCode,
                    response.ReasonPhrase
                );
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var modelsResponse = JsonSerializer.Deserialize<OpenAIModelsResponse>(
                content,
                JsonOptions
            );

            // Since we can't get billing info with regular API keys, provide basic account info
            return new ProviderApiAccountInfo
            {
                ProviderName = ProviderName,
                AccountId = "openai-account", // Generic ID since we can't access organization details
                Balance = null, // Cannot access billing information with regular API keys
                Currency = "USD",
                PlanType = "API", // Generic plan type
                AdditionalInfo = new()
                {
                    ["models_available"] = modelsResponse?.Data.Count ?? 0,
                    ["api_status"] = "connected",
                    ["is_connected"] = true,
                    ["note"] = "Balance information requires billing API access",
                },
            };
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(
                httpEx,
                "HTTP error when connecting to OpenAI API. This might be a network or SSL issue."
            );
            return new ProviderApiAccountInfo
            {
                ProviderName = ProviderName,
                AccountId = "openai-account",
                Balance = null,
                Currency = "USD",
                PlanType = "API",
                AdditionalInfo = new()
                {
                    ["error"] = "SSL/Network connection failed",
                    ["detailed_error"] = httpEx.Message,
                    ["api_status"] = "connection_error",
                    ["is_connected"] = false,
                    ["troubleshooting"] = "Check network connectivity and SSL configuration",
                },
            };
        }
        catch (TaskCanceledException taskEx) when (taskEx.InnerException is TimeoutException)
        {
            _logger.LogError(taskEx, "Timeout when connecting to OpenAI API");
            return new ProviderApiAccountInfo
            {
                ProviderName = ProviderName,
                AccountId = "openai-account",
                Balance = null,
                Currency = "USD",
                PlanType = "API",
                AdditionalInfo = new()
                {
                    ["error"] = "Request timeout",
                    ["api_status"] = "timeout",
                    ["is_connected"] = false,
                },
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving OpenAI account information");
            return new ProviderApiAccountInfo
            {
                ProviderName = ProviderName,
                AccountId = "openai-account",
                Balance = null,
                Currency = "USD",
                PlanType = "API",
                AdditionalInfo = new()
                {
                    ["error"] = ex.Message,
                    ["api_status"] = "error",
                    ["is_connected"] = false,
                },
            };
        }
    }

    public Task<ProviderApiUsageInfo?> GetUsageInfoAsync(
        int days = 30,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("OpenAI API key not configured");
            return Task.FromResult<ProviderApiUsageInfo?>(null);
        }

        try
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days);

            // OpenAI's billing endpoints require special access, so we'll simulate usage data
            // In a real scenario, you could track usage from your own application logs
            _logger.LogInformation(
                "OpenAI usage data not available - billing API requires special access"
            );

            // Return a placeholder response indicating billing API limitations
            var providerApiUsageInfo = new ProviderApiUsageInfo
            {
                ProviderName = ProviderName,
                StartDate = startDate,
                EndDate = endDate,
                TotalCost = 0m, // Cannot access billing information
                Currency = "USD",
                TotalRequests = 0,
                TotalTokens = 0,
                ModelBreakdown = new List<ModelUsageInfo>(),
            };
            return Task.FromResult(providerApiUsageInfo ?? null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving OpenAI usage information");
            return Task.FromResult<ProviderApiUsageInfo?>(null);
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
        return Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? _configuration["ChatCompleteSettings:OpenAiApiKey"];
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

// OpenAI API Response Models
internal record OpenAIModelsResponse
{
    public string Object { get; init; } = string.Empty;
    public List<OpenAIModel> Data { get; init; } = new();
}

internal record OpenAIModel
{
    public string Id { get; init; } = string.Empty;
    public string Object { get; init; } = string.Empty;
    public long Created { get; init; }
    public string OwnedBy { get; init; } = string.Empty;
}
