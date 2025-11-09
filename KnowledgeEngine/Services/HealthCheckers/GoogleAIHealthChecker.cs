using System.Diagnostics.CodeAnalysis;
using ChatCompletion.Config;
using Knowledge.Contracts.Types;
using KnowledgeEngine.Agents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;


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
                StatusMessage = "Google API key not configured. Set GEMINI_API_KEY environment variable."
            };
        }

        try
        {
            // Use KernelFactory to create Google kernel and test connectivity
            var kernel = new KernelFactory(_settings).Create(AiProvider.Google);
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // Make minimal test request
            var testHistory = new ChatHistory();
            testHistory.AddUserMessage("test");

            var response = await chatService.GetChatMessageContentAsync(
                testHistory,
                new GeminiPromptExecutionSettings() { MaxTokens = 1 },
                cancellationToken: cancellationToken
            );

            return new ComponentHealth
            {
                ComponentName = ComponentName,
                IsConnected = true,
                Status = "Healthy",
                StatusMessage = "Google API accessible and responding"
            };
        }
        catch (Exception ex)
        {
            return new ComponentHealth
            {
                ComponentName = ComponentName,
                IsConnected = false,
                Status = "Failed",
                StatusMessage = $"Google API Error: {ex.Message}"
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