using System.Diagnostics.CodeAnalysis;
using ChatCompletion.Config;
using Knowledge.Contracts.Types;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace KnowledgeEngine;

public sealed class KernelFactory
{
    private readonly ChatCompleteSettings _cfg;

    public KernelFactory(IOptions<ChatCompleteSettings> cfg)
    {
        _cfg = cfg.Value;
    }

    [Experimental("SKEXP0070")]
    public Kernel Create(AiProvider provider, string? ollamaModel = null)
    {
        var builder = Kernel.CreateBuilder();       // public factory

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
                builder.AddOpenAIChatCompletion(_cfg.OpenAIModel, openAiApiKey);
                break;

            case AiProvider.Google:
                var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (string.IsNullOrEmpty(geminiApiKey))
                {
                    throw new InvalidOperationException(
                        "The Gemini API key is not set in the environment variables."
                    );
                }
                builder.AddGoogleAIGeminiChatCompletion(_cfg.GoogleModel, geminiApiKey);
                break;

            case AiProvider.Anthropic:
                var anthropicApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
                if (string.IsNullOrEmpty(anthropicApiKey))
                {
                    throw new InvalidOperationException(
                        "The Anthropic API key is not set in the environment variables."
                    );
                }
                builder.AddAnthropicChatCompletion(_cfg.AnthropicModel, anthropicApiKey);
                break;

            case AiProvider.Ollama:
                // Ollama doesn't require an API key, but validate the base URL
                if (string.IsNullOrEmpty(_cfg.OllamaBaseUrl))
                {
                    throw new InvalidOperationException(
                        "The Ollama base URL is not configured in settings."
                    );
                }
                var client = new HttpClient();
                client.BaseAddress = new Uri(_cfg.OllamaBaseUrl);
                
                // Use provided model or fall back to appsettings model
                var modelToUse = !string.IsNullOrEmpty(ollamaModel) ? ollamaModel : _cfg.OllamaModel;
                builder.AddOllamaChatCompletion(modelToUse, client);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(provider));
        }

        return builder.Build();
    }
}