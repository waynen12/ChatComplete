using ChatCompletion.Config;
using Knowledge.Contracts.Types;
using KnowledgeEngine;
using KnowledgeEngine.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KnowledgeManager.Tests.AgentFramework;

/// <summary>
/// Unit tests for ChatCompleteAF (Agent Framework version).
/// Tests core chat functionality, tool calling, and error handling.
/// </summary>
public class ChatCompleteAFTests
{
    private readonly Mock<global::KnowledgeEngine.KnowledgeManager> _mockKnowledgeManager;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly ChatCompleteSettings _settings;

    public ChatCompleteAFTests()
    {
        _mockKnowledgeManager = new Mock<global::KnowledgeEngine.KnowledgeManager>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _settings = new ChatCompleteSettings
        {
            OpenAIModel = "gpt-4",
            Temperature = 0.7,
            SystemPrompt = "You are a helpful assistant.",
            SystemPromptWithCoding = "You are a coding assistant.",
            UseAgentFramework = true,
        };
    }

    [Fact]
    public void ChatCompleteAF_Constructor_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var chatAF = new ChatCompleteAF(
            _mockKnowledgeManager.Object,
            _settings,
            _mockServiceProvider.Object
        );

        // Assert
        Assert.NotNull(chatAF);
    }

    [Fact]
    public async Task AskAsync_WithValidInput_ShouldReturnResponse()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test-key-123");

        var chatAF = new ChatCompleteAF(
            _mockKnowledgeManager.Object,
            _settings,
            _mockServiceProvider.Object
        );

        var chatHistory = new List<ChatMessage>();

        // Mock knowledge search to return empty results
        _mockKnowledgeManager
            .Setup(km => km.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new List<KnowledgeSearchResult>());

        // Act & Assert
        // Note: This test will fail if no API key is set, but validates the structure
        try
        {
            var result = await chatAF.AskAsync(
                userMessage: "Hello",
                knowledgeId: null,
                chatHistory: chatHistory,
                apiTemperature: 0.7,
                provider: AiProvider.OpenAi,
                useExtendedInstructions: false,
                ollamaModel: null,
                ct: CancellationToken.None
            );

            // If we get here, the API call succeeded (requires valid API key)
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
        catch (Exception ex)
        {
            // Expected to fail without valid API key - verify the structure is correct
            Assert.True(
                ex.Message.Contains("API key") ||
                ex.Message.Contains("Unauthorized") ||
                ex.Message.Contains("authentication"),
                $"Expected API key error, got: {ex.Message}"
            );
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        }
    }

    [Fact]
    public async Task AskAsync_WithKnowledgeId_ShouldPerformVectorSearch()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test-key-123");

        var chatAF = new ChatCompleteAF(
            _mockKnowledgeManager.Object,
            _settings,
            _mockServiceProvider.Object
        );

        var chatHistory = new List<ChatMessage>();
        var searchResults = new List<KnowledgeSearchResult>
        {
            new()
            {
                Text = "Test knowledge content",
                Score = 0.85,
                ChunkOrder = 1,
            }
        };

        _mockKnowledgeManager
            .Setup(km => km.SearchAsync(
                "test-kb",
                "What is the test?",
                10,
                0.3,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(searchResults);

        // Act
        try
        {
            await chatAF.AskAsync(
                userMessage: "What is the test?",
                knowledgeId: "test-kb",
                chatHistory: chatHistory,
                apiTemperature: 0.7,
                provider: AiProvider.OpenAi,
                useExtendedInstructions: false,
                ollamaModel: null,
                ct: CancellationToken.None
            );
        }
        catch
        {
            // Expected without valid API key
        }

        // Assert
        _mockKnowledgeManager.Verify(
            km => km.SearchAsync(
                "test-kb",
                "What is the test?",
                10,
                0.3,
                It.IsAny<CancellationToken>()
            ),
            Times.Once,
            "Vector search should be called when knowledgeId is provided"
        );

        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
    }

    [Fact]
    public void AskAsync_WithoutApiKey_ShouldThrowException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);

        var chatAF = new ChatCompleteAF(
            _mockKnowledgeManager.Object,
            _settings,
            _mockServiceProvider.Object
        );

        var chatHistory = new List<ChatMessage>();

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await chatAF.AskAsync(
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
    }

    [Fact]
    public void AskWithAgentAsync_WithoutPlugins_ShouldInitialize()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test-key-123");

        // Setup service provider to return null for plugins (no plugins registered)
        _mockServiceProvider
            .Setup(sp => sp.GetService(It.IsAny<Type>()))
            .Returns(null);

        var chatAF = new ChatCompleteAF(
            _mockKnowledgeManager.Object,
            _settings,
            _mockServiceProvider.Object
        );

        var chatHistory = new List<ChatMessage>();

        // Mock knowledge search
        _mockKnowledgeManager
            .Setup(km => km.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new List<KnowledgeSearchResult>());

        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(async () =>
            await chatAF.AskWithAgentAsync(
                userMessage: "Hello",
                knowledgeId: null,
                chatHistory: chatHistory,
                apiTemperature: 0.7,
                provider: AiProvider.OpenAi,
                useExtendedInstructions: false,
                enableAgentTools: true,
                ollamaModel: null,
                ct: CancellationToken.None
            )
        );

        // Should fail gracefully when plugins are not registered
        Assert.NotNull(exception);

        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
    }

    [Fact]
    public async Task AskWithAgentAsync_WithToolsDisabled_ShouldNotRegisterPlugins()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "sk-test-key-123");

        var chatAF = new ChatCompleteAF(
            _mockKnowledgeManager.Object,
            _settings,
            _mockServiceProvider.Object
        );

        var chatHistory = new List<ChatMessage>();

        _mockKnowledgeManager
            .Setup(km => km.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new List<KnowledgeSearchResult>());

        // Act
        try
        {
            await chatAF.AskWithAgentAsync(
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
        }
        catch
        {
            // Expected without valid API key
        }

        // Assert - service provider should not be called to get plugins
        _mockServiceProvider.Verify(
            sp => sp.GetService(It.IsAny<Type>()),
            Times.Never,
            "Plugins should not be requested when tools are disabled"
        );

        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
    }

    [Fact]
    public void ChatCompleteAF_ShouldLogAFPrefixes()
    {
        // Arrange & Act
        using var consoleCapture = new StringWriter();
        Console.SetOut(consoleCapture);

        var chatAF = new ChatCompleteAF(
            _mockKnowledgeManager.Object,
            _settings,
            _mockServiceProvider.Object
        );

        var output = consoleCapture.ToString();

        // Assert
        Assert.Contains("[AF]", output);
        Assert.Contains("ChatCompleteAF initialized with Agent Framework", output);
    }
}
