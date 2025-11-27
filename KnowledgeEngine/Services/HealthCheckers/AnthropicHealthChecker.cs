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
            // Use KernelFactory to create Anthropic kernel and test connectivity
            var kernel = new KernelFactory(_settings).Create(AiProvider.Anthropic);
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // Make minimal test request
            var testHistory = new ChatHistory();
            testHistory.AddUserMessage("test");

            var response = await chatService.GetChatMessageContentAsync(
                testHistory,
                new AnthropicPromptExecutionSettings() { MaxTokens = 1 },
                kernel: null, // Kernel parameter not used for health check
                cancellationToken: cancellationToken
            );

            return new ComponentHealth
            {
                ComponentName = ComponentName,
                IsConnected = true,
                Status = "Healthy",
                StatusMessage = "Anthropic API accessible and responding"
            };
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