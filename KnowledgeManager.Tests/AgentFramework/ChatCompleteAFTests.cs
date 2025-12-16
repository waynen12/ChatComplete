using ChatCompletion.Config;
using Knowledge.Contracts.Types;
using KnowledgeEngine.Agents.Models;
using Microsoft.Extensions.AI;
using Xunit;

namespace KnowledgeManager.Tests.AgentFramework;

/// <summary>
/// Unit tests for ChatCompleteAF (Agent Framework version).
/// Tests core chat functionality, tool calling, and error handling.
/// Uses FakeChatCompleteAF test double pattern (similar to SpyChatComplete).
/// </summary>
public class ChatCompleteAFTests
{
    // No setup needed - each test creates its own FakeChatCompleteAF instance

    [Fact]
    public void ChatCompleteAF_Constructor_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var chatAF = new FakeChatCompleteAF();

        // Assert
        Assert.NotNull(chatAF);
    }

    [Fact]
    public async Task AskAsync_WithValidInput_ShouldReturnResponse()
    {
        // Arrange
        var fake = new FakeChatCompleteAF
        {
            AskResponse = "Hello! How can I help you?"
        };

        var chatHistory = new List<ChatMessage>();

        // Act
        var result = await fake.AskAsync(
            userMessage: "Hello",
            knowledgeId: null,
            chatHistory: chatHistory,
            apiTemperature: 0.7,
            provider: AiProvider.OpenAi,
            useExtendedInstructions: false,
            ollamaModel: null,
            ct: CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello! How can I help you?", result);
        Assert.Equal(0, fake.LastHistoryCount);
        Assert.Null(fake.LastKnowledgeId);
        Assert.Equal(0.7, fake.LastTemperature);
        Assert.Equal(AiProvider.OpenAi, fake.LastProvider);
        Assert.False(fake.LastUseExtendedInstructions);
        Assert.Null(fake.LastOllamaModel);
    }

    [Fact]
    public async Task AskAsync_WithKnowledgeId_ShouldCaptureParameters()
    {
        // Arrange
        var fake = new FakeChatCompleteAF
        {
            AskResponse = "Based on the knowledge base: Test answer"
        };

        var chatHistory = new List<ChatMessage>();

        // Act
        var result = await fake.AskAsync(
            userMessage: "What is the test?",
            knowledgeId: "test-kb",
            chatHistory: chatHistory,
            apiTemperature: 0.7,
            provider: AiProvider.OpenAi,
            useExtendedInstructions: false,
            ollamaModel: null,
            ct: CancellationToken.None
        );

        // Assert - Verify parameters were captured
        Assert.NotNull(result);
        Assert.Equal("test-kb", fake.LastKnowledgeId);
        Assert.Equal(AiProvider.OpenAi, fake.LastProvider);
        Assert.Equal(0.7, fake.LastTemperature);
    }

    [Fact]
    public async Task AskAsync_WithException_ShouldThrow()
    {
        // Arrange
        var fake = new FakeChatCompleteAF
        {
            ShouldThrowException = true,
            ExceptionToThrow = new InvalidOperationException("API key not found")
        };

        var chatHistory = new List<ChatMessage>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await fake.AskAsync(
                userMessage: "Hello",
                knowledgeId: null,
                chatHistory: chatHistory,
                apiTemperature: 0.7,
                provider: AiProvider.OpenAi,
                useExtendedInstructions: false,
                ollamaModel: null,
                ct: CancellationToken.None
            )
        );

        Assert.NotNull(exception);
        Assert.Equal("API key not found", exception.Message);
    }

    [Fact]
    public async Task AskWithAgentAsync_WithToolsEnabled_ShouldReturnAgentResponse()
    {
        // Arrange
        var fake = new FakeChatCompleteAF
        {
            AskWithAgentResponse = new AgentChatResponse
            {
                Response = "Agent response with tools",
                UsedAgentCapabilities = true
            }
        };

        var chatHistory = new List<ChatMessage>();

        // Act
        var result = await fake.AskWithAgentAsync(
            userMessage: "Search for knowledge",
            knowledgeId: null,
            chatHistory: chatHistory,
            apiTemperature: 0.7,
            provider: AiProvider.OpenAi,
            useExtendedInstructions: false,
            enableAgentTools: true,
            ollamaModel: null,
            ct: CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Agent response with tools", result.Response);
        Assert.True(result.UsedAgentCapabilities);
        Assert.True(fake.LastEnableAgentTools);
    }

    [Fact]
    public async Task AskWithAgentAsync_WithToolsDisabled_ShouldNotUseAgentCapabilities()
    {
        // Arrange
        var fake = new FakeChatCompleteAF
        {
            AskWithAgentResponse = new AgentChatResponse
            {
                Response = "Response without tools",
                UsedAgentCapabilities = false
            }
        };

        var chatHistory = new List<ChatMessage>();

        // Act
        var result = await fake.AskWithAgentAsync(
            userMessage: "Hello",
            knowledgeId: null,
            chatHistory: chatHistory,
            apiTemperature: 0.7,
            provider: AiProvider.OpenAi,
            useExtendedInstructions: false,
            enableAgentTools: false, // Tools disabled
            ollamaModel: null,
            ct: CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Response without tools", result.Response);
        Assert.False(result.UsedAgentCapabilities);
        Assert.False(fake.LastEnableAgentTools);
    }

    [Fact(Skip = "Console redirection interferes with other tests")]
    public void ChatCompleteAF_ShouldLogAFPrefixes()
    {
        // Arrange & Act
        using var consoleCapture = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(consoleCapture);

        var fake = new FakeChatCompleteAF();

        var output = consoleCapture.ToString();

        // Restore console output immediately
        Console.SetOut(originalOut);

        // Assert
        Assert.Contains("[AF]", output);
        Assert.Contains("ChatCompleteAF initialized with Agent Framework", output);
    }
}
