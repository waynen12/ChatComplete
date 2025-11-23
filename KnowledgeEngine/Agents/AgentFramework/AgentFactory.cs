using Anthropic.SDK;
using GenerativeAI.Microsoft;
using Knowledge.Contracts.Types;
using ChatCompletion.Config;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
// using OllamaSharp; // TODO: Disabled due to SK connector conflict - will be re-enabled after SK migration
using OpenAI;

namespace KnowledgeEngine.Agents.AgentFramework;

/// <summary>
/// Factory for creating Agent Framework AIAgent instances for different LLM providers.
/// Replaces KernelFactory for Agent Framework-based chat.
/// </summary>
public class AgentFactory
{
    private readonly ChatCompleteSettings _settings;

    public AgentFactory(ChatCompleteSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Creates an AIAgent for the specified provider without tools.
    /// </summary>
    public AIAgent CreateAgent(AiProvider provider, string? systemPrompt = null, string? ollamaModel = null)
    {
        return CreateAgent(provider, systemPrompt, tools: null, ollamaModel);
    }

    /// <summary>
    /// Creates an AIAgent for the specified provider with optional tools.
    /// </summary>
    /// <param name="provider">The LLM provider to use</param>
    /// <param name="systemPrompt">Optional system prompt/instructions for the agent</param>
    /// <param name="tools">Optional list of tools the agent can use</param>
    /// <param name="ollamaModel">Optional model override for Ollama provider</param>
    /// <returns>Configured AIAgent instance</returns>
    public AIAgent CreateAgent(
        AiProvider provider,
        string? systemPrompt,
        IEnumerable<AITool>? tools,
        string? ollamaModel = null)
    {
        AIAgent agent;

        switch (provider)
        {
            case AiProvider.OpenAi:
                var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (string.IsNullOrEmpty(openAiApiKey))
                {
                    throw new InvalidOperationException(
                        "The OpenAI API key is not set in the environment variables."
                    );
                }

                var openAiClient = new OpenAIClient(openAiApiKey);
                var chatClient = openAiClient.GetChatClient(_settings.OpenAIModel);

                agent = tools != null
                    ? chatClient.CreateAIAgent(instructions: systemPrompt, tools: tools.ToArray())
                    : chatClient.CreateAIAgent(instructions: systemPrompt);

                Console.WriteLine($"✅ Created OpenAI agent with model: {_settings.OpenAIModel}");
                break;

            case AiProvider.Google:
                var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (string.IsNullOrEmpty(geminiApiKey))
                {
                    throw new InvalidOperationException(
                        "The Gemini API key is not set in the environment variables."
                    );
                }

                var googleClient = new GenerativeAIChatClient(geminiApiKey, _settings.GoogleModel);
                agent = new ChatClientAgent(googleClient);

                Console.WriteLine($"✅ Created Google agent with model: {_settings.GoogleModel}");
                break;

            case AiProvider.Anthropic:
                var anthropicApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
                if (string.IsNullOrEmpty(anthropicApiKey))
                {
                    throw new InvalidOperationException(
                        "The Anthropic API key is not set in the environment variables."
                    );
                }

                var anthropicClient = new AnthropicClient(new APIAuthentication(anthropicApiKey))
                    .Messages.AsBuilder()
                    .Build();

                agent = new ChatClientAgent(anthropicClient);

                Console.WriteLine($"✅ Created Anthropic agent with model: {_settings.AnthropicModel}");
                break;

            case AiProvider.Ollama:
                // TODO: Ollama support temporarily disabled due to version conflict with SK connector
                // OllamaSharp 5.4.11 is incompatible with Microsoft.SemanticKernel.Connectors.Ollama 1.64.0
                // This will be re-enabled after completing the SK migration
                throw new NotSupportedException(
                    "Ollama provider is temporarily unavailable in Agent Framework mode. " +
                    "Use OpenAI, Google, or Anthropic providers instead. " +
                    "Ollama support will be restored after Semantic Kernel migration is complete.");

                /* Original implementation - will be uncommented after SK removal:
                if (string.IsNullOrEmpty(_settings.OllamaBaseUrl))
                {
                    throw new InvalidOperationException(
                        "The Ollama base URL is not configured in settings."
                    );
                }

                var modelToUse = ollamaModel ?? _settings.OllamaModel;
                var ollamaClient = new OllamaApiClient(_settings.OllamaBaseUrl, modelToUse);
                agent = new ChatClientAgent(ollamaClient);

                Console.WriteLine($"✅ Created Ollama agent with model: {modelToUse}");
                break;
                */

            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported AI provider");
        }

        return agent;
    }

    /// <summary>
    /// Creates an AIAgent with tools registered from plugin instances.
    /// </summary>
    public AIAgent CreateAgentWithPlugins(
        AiProvider provider,
        string? systemPrompt,
        Dictionary<string, object> plugins,
        string? ollamaModel = null)
    {
        var tools = AgentToolRegistration.CreateToolsFromPlugins(plugins);

        if (!tools.Any())
        {
            Console.WriteLine("⚠️  No tools were registered from plugins");
        }

        return CreateAgent(provider, systemPrompt, tools, ollamaModel);
    }
}
