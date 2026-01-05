using ChatCompletion.Config;
using KnowledgeEngine.Agents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeEngine.Services.HealthCheckers;

public class GoogleAIHealthChecker : IComponentHealthChecker
{
    private readonly IOptions<ChatCompleteSettings> _settings;
    private readonly ILogger<GoogleAIHealthChecker> _logger;
    public string ComponentName => "Google AI";
    public int Priority => 1;
    public bool IsCriticalComponent => true;

    public GoogleAIHealthChecker(
        IOptions<ChatCompleteSettings> settings,
        ILogger<GoogleAIHealthChecker> logger
    )
    {
        _settings = settings;
        _logger = logger;
    }

    // API Key retrieval (follows existing pattern from analytics services)
    private string? GetApiKey()
    {
        return Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    }

    public async Task<ComponentHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            return new ComponentHealth
            {
                ComponentName = ComponentName,
                IsConnected = false,
                Status = "Missing API Key",
                StatusMessage = "Google API key not configured. Set GEMINI_API_KEY environment variable."
            };
        }

        try
        {
            // Direct API call for health check - more reliable than SDK
            using var httpClient = new HttpClient();

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = "test" }
                        }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = 1
                }
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Value.GoogleModel}:generateContent?key={apiKey}",
                content,
                cancellationToken
            );

            if (response.IsSuccessStatusCode)
            {
                return new ComponentHealth
                {
                    ComponentName = ComponentName,
                    IsConnected = true,
                    Status = "Healthy",
                    StatusMessage = "Google AI API accessible and responding"
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return new ComponentHealth
                {
                    ComponentName = ComponentName,
                    IsConnected = false,
                    Status = "Failed",
                    StatusMessage = $"Google AI API returned status {response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (Exception ex)
        {
            return new ComponentHealth
            {
                ComponentName = ComponentName,
                IsConnected = false,
                Status = "Failed",
                StatusMessage = $"Google AI API Error: {ex.Message}"
            };
        }
    }

    public async Task<ComponentHealth> QuickHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        // For external API providers, quick check is the same as full check
        return await CheckHealthAsync(cancellationToken);
    }

    public Task<Dictionary<string, object>> GetComponentMetricsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}