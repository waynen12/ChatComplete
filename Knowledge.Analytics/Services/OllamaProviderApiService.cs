using Knowledge.Analytics.Models;
using Knowledge.Contracts.Types;
using Knowledge.Data.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace Knowledge.Analytics.Services;

/// <summary>
/// Provider API service for Ollama - handles connection status and basic metrics
/// </summary>
public class OllamaProviderApiService : IProviderApiService
{
    private readonly IOllamaRepository _ollamaRepo;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly ILogger<OllamaProviderApiService> _logger;
    private readonly HttpClient _httpClient;

    public OllamaProviderApiService(IOllamaRepository ollamaRepo, IUsageTrackingService usageTrackingService, ILogger<OllamaProviderApiService> logger, HttpClient httpClient)
    {
        _ollamaRepo = ollamaRepo;
        _usageTrackingService = usageTrackingService;
        _logger = logger;
        _httpClient = httpClient;
    }

    public string ProviderName => "Ollama";
    public bool IsConfigured => true; // Ollama doesn't require API keys

    public async Task<ProviderApiAccountInfo?> GetAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if Ollama is available by checking the repository for models
            var models = await _ollamaRepo.GetInstalledModelsAsync(cancellationToken);
            var isConnected = models?.Count() > 0;
            
            // Also try to ping the Ollama service to verify it's running
            if (!isConnected)
            {
                try
                {
                    // Try a simple HTTP check to Ollama's default endpoint
                    var response = await _httpClient.GetAsync("http://localhost:11434/api/tags", cancellationToken);
                    isConnected = response.IsSuccessStatusCode;
                }
                catch
                {
                    // Ollama service not available
                    isConnected = false;
                }
            }

            return new ProviderApiAccountInfo
            {
                ProviderName = ProviderName,
                AccountId = "local", // Ollama runs locally
                Balance = null, // Ollama is free/local
                Currency = null,
                CreditUsed = null,
                CreditLimit = null,
                PlanType = "Local Installation",
                AdditionalInfo = new Dictionary<string, object>
                {
                    ["isConnected"] = isConnected,
                    ["modelCount"] = models?.Count() ?? 0,
                    ["errorMessage"] = isConnected ? string.Empty : "Ollama service not available"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to connect to Ollama: {Message}", ex.Message);
            
            return new ProviderApiAccountInfo
            {
                ProviderName = ProviderName,
                AccountId = "local",
                Balance = null,
                Currency = null,
                CreditUsed = null,
                CreditLimit = null,
                PlanType = "Local Installation",
                AdditionalInfo = new Dictionary<string, object>
                {
                    ["isConnected"] = false,
                    ["errorMessage"] = $"Connection failed: {ex.Message}"
                }
            };
        }
    }

    public async Task<ProviderApiUsageInfo?> GetUsageInfoAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var endDate = DateTime.UtcNow;
            
            // Get usage history from our local tracking database
            var usageHistory = await _usageTrackingService.GetUsageHistoryAsync(days, cancellationToken);
            
            // Filter for Ollama provider
            var ollamaUsage = usageHistory.Where(u => u.Provider == AiProvider.Ollama).ToList();
            
            var totalRequests = ollamaUsage.Count;
            var totalTokens = ollamaUsage.Sum(u => u.InputTokens + u.OutputTokens);
            
            // Group by model for breakdown
            var modelBreakdown = ollamaUsage
                .Where(u => !string.IsNullOrEmpty(u.ModelName))
                .GroupBy(u => u.ModelName!)
                .Select(g => new ModelUsageInfo
                {
                    ModelName = g.Key,
                    Requests = g.Count(),
                    InputTokens = g.Sum(u => u.InputTokens),
                    OutputTokens = g.Sum(u => u.OutputTokens),
                    Cost = 0 // Ollama is free
                })
                .ToList();
            
            return new ProviderApiUsageInfo
            {
                ProviderName = ProviderName,
                StartDate = startDate,
                EndDate = endDate,
                TotalCost = 0, // Ollama is free
                Currency = "USD",
                TotalRequests = totalRequests,
                TotalTokens = totalTokens,
                ModelBreakdown = modelBreakdown,
                AdditionalInfo = new Dictionary<string, object>
                {
                    ["dataSource"] = "Local usage tracking database",
                    ["averageResponseTime"] = ollamaUsage.Any() ? ollamaUsage.Average(u => u.ResponseTime.TotalMilliseconds) : 0,
                    ["successRate"] = ollamaUsage.Any() ? (double)ollamaUsage.Count(u => u.WasSuccessful) / ollamaUsage.Count * 100 : 100
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Ollama usage info from tracking database");
            
            // Return empty data instead of null on error
            return new ProviderApiUsageInfo
            {
                ProviderName = ProviderName,
                StartDate = DateTime.UtcNow.AddDays(-days),
                EndDate = DateTime.UtcNow,
                TotalCost = 0,
                Currency = "USD",
                TotalRequests = 0,
                TotalTokens = 0,
                ModelBreakdown = new List<ModelUsageInfo>(),
                AdditionalInfo = new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                }
            };
        }
    }
}