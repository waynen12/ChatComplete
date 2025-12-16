using ChatCompletion.Config;
using Knowledge.Contracts.Types;
using KnowledgeEngine;
using KnowledgeEngine.Agents.Models;
using KnowledgeEngine.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeManager.Tests.AgentFramework;

/// <summary>
/// Test double for ChatCompleteAF that overrides methods for controlled testing
/// Similar pattern to SpyChatComplete
/// </summary>
internal sealed class FakeChatCompleteAF : ChatCompleteAF
{
    public int? LastHistoryCount { get; private set; }
    public string? LastKnowledgeId { get; private set; }
    public double LastTemperature { get; private set; }
    public AiProvider LastProvider { get; private set; }
    public bool LastUseExtendedInstructions { get; private set; }
    public string? LastOllamaModel { get; private set; }
    public bool? LastEnableAgentTools { get; private set; }

    // Configure responses for tests
    public string? AskResponse { get; set; } = "stub-response";
    public AgentChatResponse? AskWithAgentResponse { get; set; }
    public bool ShouldThrowException { get; set; }
    public Exception? ExceptionToThrow { get; set; }

    public FakeChatCompleteAF()
        : base(
            knowledgeManager: null!,  // not used by fake
            settings: new ChatCompleteSettings
            {
                OpenAIModel = "gpt-4",
                Temperature = 0.7,
                SystemPrompt = "You are a helpful assistant.",
                SystemPromptWithCoding = "You are a coding assistant.",
                UseAgentFramework = true
            },
            serviceProvider: CreateMockServiceProvider())
    {
        AskWithAgentResponse = new AgentChatResponse
        {
            Response = "stub-agent-response",
            UsedAgentCapabilities = false
        };
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var services = new ServiceCollection();
        // Add any required services here if needed
        return services.BuildServiceProvider();
    }

    public override Task<string> AskAsync(
        string userMessage,
        string? knowledgeId,
        List<ChatMessage> chatHistory,
        double apiTemperature,
        AiProvider provider,
        bool useExtendedInstructions = false,
        string? ollamaModel = null,
        CancellationToken ct = default)
    {
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new InvalidOperationException("Test exception");
        }

        LastHistoryCount = chatHistory.Count;
        LastKnowledgeId = knowledgeId;
        LastTemperature = apiTemperature;
        LastProvider = provider;
        LastUseExtendedInstructions = useExtendedInstructions;
        LastOllamaModel = ollamaModel;
        LastEnableAgentTools = null; // AskAsync doesn't use tools

        return Task.FromResult(AskResponse ?? "stub-response");
    }

    public override Task<AgentChatResponse> AskWithAgentAsync(
        string userMessage,
        string? knowledgeId,
        List<ChatMessage> chatHistory,
        double apiTemperature,
        AiProvider provider,
        bool useExtendedInstructions = false,
        bool enableAgentTools = true,
        string? ollamaModel = null,
        CancellationToken ct = default)
    {
        if (ShouldThrowException)
        {
            throw ExceptionToThrow ?? new InvalidOperationException("Test exception");
        }

        LastHistoryCount = chatHistory.Count;
        LastKnowledgeId = knowledgeId;
        LastTemperature = apiTemperature;
        LastProvider = provider;
        LastUseExtendedInstructions = useExtendedInstructions;
        LastOllamaModel = ollamaModel;
        LastEnableAgentTools = enableAgentTools;

        return Task.FromResult(AskWithAgentResponse ?? new AgentChatResponse
        {
            Response = "stub-agent-response",
            UsedAgentCapabilities = enableAgentTools
        });
    }
}
