using System.Diagnostics.CodeAnalysis;
using ChatCompletion.Config;
using Knowledge.Contracts.Types;
using KnowledgeEngine.Agents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Anthropic;

namespace KnowledgeEngine.Services.HealthCheckers;

public class AnthropicHealthChecker : IComponentHealthChecker
{
    private readonly IOptions<ChatCompleteSettings> _settings;
    private readonly ILogger<AnthropicHealthChecker> _logger;
    public string ComponentName => "Anthropic";
    public int Priority => 1;
    public bool IsCriticalComponent => true;
    
    public AnthropicHealthChecker(
        IOptions<ChatCompleteSettings> settings, 
        ILogger<AnthropicHealthChecker> logger
    )
    {
        _settings = settings;
        _logger = logger;
    }
   
    // API Key retrieval (follows existing pattern from analytics services)
    private string? GetApiKey()
    {
        return Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
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
                StatusMessage = "Anthropic API key not configured. Set ANTHROPIC_API_KEY environment variable."
            };
        }

        try
        {
            // Direct API call without SK to avoid third-party connector incompatibility
            // Note: Lost.SemanticKernel.Connectors.Anthropic v1.25.0-alpha3 is incompatible with SK 1.64.0
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var requestBody = new
            {
                model = _settings.Value.AnthropicModel,
                max_tokens = 1,
                messages = new[]
                {
                    new { role = "user", content = "test" }
                }
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync(
                "https://api.anthropic.com/v1/messages",
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
                    StatusMessage = "Anthropic API accessible and responding"
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
                    StatusMessage = $"Anthropic API returned status {response.StatusCode}: {errorContent}"
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
                StatusMessage = $"Anthropic API Error: {ex.Message}"
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