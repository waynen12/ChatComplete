using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ChatCompletion.Config;
using Knowledge.Contracts.Types;
using KnowledgeEngine;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Xunit;

namespace KnowledgeManager.Tests.KnowledgeEngine;

public class KernelSelectionTests
{
    private static readonly ChatCompleteSettings FakeSettings = new()
    {
        GoogleModel      = "gemini-pro",
        AnthropicModel       = "claude-3-opus-20240229",
        OllamaModel          = "gemma3:12b",
        EmbeddingProviders = new() 
        { 
            ActiveProvider = "OpenAI",
            OpenAI = new() { ModelName = "text-embedding-ada-002" }
        },
        Temperature          = 0.7,
        Atlas = new() { ClusterName = "dummy", SearchIndexName = "dummy" }
    };

    private static readonly Dictionary<AiProvider, (string envVar, string value)> Keys = new()
    {
        { AiProvider.OpenAi   , ("OPENAI_API_KEY"  , "test-key") },
        { AiProvider.Google   , ("GEMINI_API_KEY"  , "test-key") },
        { AiProvider.Anthropic, ("ANTHROPIC_API_KEY", "test-key") }
        // Ollama runs local → no key
    };
    
}