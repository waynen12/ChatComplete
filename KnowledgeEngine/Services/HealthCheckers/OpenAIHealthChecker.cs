using System.Diagnostics.CodeAnalysis;
using ChatCompletion.Config;
using Knowledge.Contracts.Types;
using KnowledgeEngine.Agents.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace KnowledgeEngine.Services.HealthCheckers;

public class OpenAIHealthChecker : IComponentHealthChecker
{
    private readonly IOptions<ChatCompleteSettings> _settings;
    private readonly ILogger<OpenAIHealthChecker> _logger;
    
    public string ComponentName => "OpenAI";
    public int Priority => 1; // High priority - critical for external AI
    public bool IsCriticalComponent => true; // Main external provider

    public OpenAIHealthChecker(
        IOptions<ChatCompleteSettings> settings, 
        ILogger<OpenAIHealthChecker> logger
    )
    {
        _settings = settings;
        _logger = logger;
    }
    
    
    // API Key retrieval (follows existing pattern from analytics services)
    private string? GetApiKey()
    {
        return Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }
    
    [Experimental("SKEXP0070")]
    public  async Task<ComponentHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            return new ComponentHealth
            {
                ComponentName = ComponentName,
                IsConnected = false,
                Status = "Missing API Key",
                StatusMessage = "OpenAI API key not configured. Set OPENAI_API_KEY environment variable."
            };
        }

        try
        {
            // Direct API call for health check - more reliable than SK connector
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var requestBody = new
            {
                model = _settings.Value.OpenAIModel,
                messages = new[]
                {
                    new { role = "user", content = "test" }
                },
                max_completion_tokens = 10 // Minimum reasonable token count
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync(
                "https://api.openai.com/v1/chat/completions",
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
                    StatusMessage = "OpenAI API accessible and responding"
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
                    StatusMessage = $"OpenAI API returned status {response.StatusCode}: {errorContent}"
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
                StatusMessage = $"OpenAI API Error: {ex.Message}"
            };
        }
    }

    [Experimental("SKEXP0070")]
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