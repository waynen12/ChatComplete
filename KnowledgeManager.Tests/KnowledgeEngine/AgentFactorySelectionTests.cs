using ChatCompletion.Config;
using Knowledge.Contracts.Types;
using KnowledgeEngine.Agents.AgentFramework;
using Microsoft.Agents.AI;

namespace KnowledgeManager.Tests.KnowledgeEngine;

/// <summary>
/// Tests for AgentFactory - verifies provider selection and agent creation
/// Replaces KernelSelectionTests for Agent Framework
/// </summary>
public class AgentFactorySelectionTests
{
    private static readonly ChatCompleteSettings TestSettings = new()
    {
        OpenAIModel = "gpt-4",
        GoogleModel = "gemini-1.5-pro",
        AnthropicModel = "claude-3-5-sonnet-20241022",
        OllamaModel = "llama3.2:3b",
        OllamaBaseUrl = "http://localhost:11434",
        Temperature = 0.7,
        EmbeddingProviders = new()
        {
            ActiveProvider = "OpenAI",
            OpenAI = new() { ModelName = "text-embedding-ada-002" }
        }
    };

    private static readonly Dictionary<AiProvider, (string envVar, string value)> TestKeys = new()
    {
        { AiProvider.OpenAi, ("OPENAI_API_KEY", "test-openai-key") },
        { AiProvider.Google, ("GEMINI_API_KEY", "test-gemini-key") },
        { AiProvider.Anthropic, ("ANTHROPIC_API_KEY", "test-anthropic-key") }
        // Ollama runs locally, no API key required
    };

    [Fact]
    public void Constructor_WithNullSettings_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AgentFactory(null!));
    }

    [Fact]
    public void Constructor_WithValidSettings_ShouldCreateInstance()
    {
        // Act
        var factory = new AgentFactory(TestSettings);

        // Assert
        Assert.NotNull(factory);
    }

    [Theory]
    [InlineData(AiProvider.OpenAi, "OPENAI_API_KEY")]
    [InlineData(AiProvider.Google, "GEMINI_API_KEY")]
    [InlineData(AiProvider.Anthropic, "ANTHROPIC_API_KEY")]
    public void CreateAgent_WithoutApiKey_ShouldThrowInvalidOperationException(
        AiProvider provider,
        string requiredEnvVar)
    {
        // Arrange
        var factory = new AgentFactory(TestSettings);
        var originalValue = Environment.GetEnvironmentVariable(requiredEnvVar);

        try
        {
            // Clear the environment variable
            Environment.SetEnvironmentVariable(requiredEnvVar, null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                factory.CreateAgent(provider, "Test prompt"));

            Assert.Contains("API key", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            // Restore original value
            Environment.SetEnvironmentVariable(requiredEnvVar, originalValue);
        }
    }

    [Theory]
    [InlineData(AiProvider.OpenAi)]
    [InlineData(AiProvider.Google)]
    [InlineData(AiProvider.Anthropic)]
    public void CreateAgent_WithValidApiKey_ShouldCreateAgent(AiProvider provider)
    {
        // Arrange
        var factory = new AgentFactory(TestSettings);
        var (envVar, testValue) = TestKeys[provider];
        var originalValue = Environment.GetEnvironmentVariable(envVar);

        try
        {
            // Set test API key
            Environment.SetEnvironmentVariable(envVar, testValue);

            // Act
            var agent = factory.CreateAgent(provider, "You are a helpful assistant");

            // Assert
            Assert.NotNull(agent);
            Assert.IsAssignableFrom<AIAgent>(agent);
        }
        finally
        {
            // Restore original value
            Environment.SetEnvironmentVariable(envVar, originalValue);
        }
    }

    [Fact]
    public void CreateAgent_Ollama_WithoutApiKey_ShouldCreateAgent()
    {
        // Arrange
        var factory = new AgentFactory(TestSettings);

        // Act
        var agent = factory.CreateAgent(AiProvider.Ollama, "You are a helpful assistant");

        // Assert
        Assert.NotNull(agent);
        Assert.IsAssignableFrom<AIAgent>(agent);
    }

    [Fact]
    public void CreateAgent_Ollama_WithCustomModel_ShouldUseSpecifiedModel()
    {
        // Arrange
        var factory = new AgentFactory(TestSettings);
        var customModel = "gemma2:9b";

        // Act
        var agent = factory.CreateAgent(
            AiProvider.Ollama,
            "You are a helpful assistant",
            ollamaModel: customModel);

        // Assert
        Assert.NotNull(agent);
        // Note: Can't easily verify the model name without exposing internal state
        // This test verifies the method accepts the parameter without error
    }

    [Fact]
    public void CreateAgent_Ollama_WithNullBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var settingsWithNullUrl = new ChatCompleteSettings
        {
            OllamaBaseUrl = null,
            OllamaModel = "llama3.2:3b"
        };
        var factory = new AgentFactory(settingsWithNullUrl);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            factory.CreateAgent(AiProvider.Ollama, "Test prompt"));

        Assert.Contains("Ollama base URL", exception.Message);
    }

    [Fact]
    public void CreateAgent_WithNullSystemPrompt_ShouldCreateAgent()
    {
        // Arrange
        var factory = new AgentFactory(TestSettings);
        var originalKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");

            // Act
            var agent = factory.CreateAgent(AiProvider.OpenAi, systemPrompt: null);

            // Assert
            Assert.NotNull(agent);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalKey);
        }
    }

    [Fact]
    public void CreateAgent_WithNullTools_ShouldCreateAgentWithoutTools()
    {
        // Arrange
        var factory = new AgentFactory(TestSettings);
        var originalKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");

            // Act
            var agent = factory.CreateAgent(
                AiProvider.OpenAi,
                "You are a helpful assistant",
                tools: null);

            // Assert
            Assert.NotNull(agent);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalKey);
        }
    }

    [Fact]
    public void CreateAgentWithPlugins_WithEmptyPlugins_ShouldCreateAgentWithoutTools()
    {
        // Arrange
        var factory = new AgentFactory(TestSettings);
        var originalKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");
            var emptyPlugins = new Dictionary<string, object>();

            // Act
            var agent = factory.CreateAgentWithPlugins(
                AiProvider.OpenAi,
                "You are a helpful assistant",
                emptyPlugins);

            // Assert
            Assert.NotNull(agent);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalKey);
        }
    }

    [Fact]
    public void CreateAgent_WithUnsupportedProvider_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var factory = new AgentFactory(TestSettings);
        var unsupportedProvider = (AiProvider)999;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            factory.CreateAgent(unsupportedProvider, "Test prompt"));
    }
}
