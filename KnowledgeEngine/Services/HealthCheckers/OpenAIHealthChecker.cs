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
            // Use KernelFactory to create OpenAI kernel and test connectivity
            var kernel = new KernelFactory(_settings).Create(AiProvider.OpenAi);
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // Make minimal test request
            var testHistory = new ChatHistory();
            testHistory.AddUserMessage("test");

            var response = await chatService.GetChatMessageContentAsync(
                testHistory,
                new OpenAIPromptExecutionSettings { MaxTokens = 1 },
                cancellationToken: cancellationToken
            );

            return new ComponentHealth
            {
                ComponentName = ComponentName,
                IsConnected = true,
                Status = "Healthy",
                StatusMessage = "OpenAI API accessible and responding"
            };
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