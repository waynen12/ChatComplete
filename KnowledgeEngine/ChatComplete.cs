using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ChatCompletion.Config;
using Knowledge.Contracts.Types;
using KnowledgeEngine;
using KnowledgeEngine.Agents.Models;
using KnowledgeEngine.Agents.Plugins;
using KnowledgeEngine.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Anthropic;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050, SKEXP0070

namespace ChatCompletion
{
    public class ChatComplete
    {
        IChatCompletionService _chatCompletionService;
        private readonly KnowledgeEngine.KnowledgeManager _knowledgeManager;
        private readonly IServiceProvider _serviceProvider;
        ChatCompleteSettings _settings;
        IOptions<ChatCompleteSettings> _options;
        private readonly ConcurrentDictionary<string, Kernel> _kernels = new();
        
        // File-based prompt plugins
        private KernelPlugin? _chatAssistantPlugin;
        private KernelPlugin? _contextualChatPlugin;

        public ChatComplete(
            KnowledgeEngine.KnowledgeManager knowledgeManager,
            ChatCompleteSettings settings,
            IServiceProvider serviceProvider
        )
        {
            _knowledgeManager = knowledgeManager;
            _serviceProvider = serviceProvider;
            _settings = settings;
            _options = Options.Create(settings);
            
            // Load file-based prompt plugins
            LoadPromptPlugins();
        }
        
        private void LoadPromptPlugins()
        {
            try
            {
                var promptsDirectory = Path.Combine(AppContext.BaseDirectory, "Prompts");
                
                // Create a temporary kernel for loading prompt plugins
                var tempKernel = Kernel.CreateBuilder().Build();
                
                // Load system prompt plugins
                _chatAssistantPlugin = tempKernel.CreatePluginFromPromptDirectory(
                    Path.Combine(promptsDirectory, "ChatAssistant"));
                    
                _contextualChatPlugin = tempKernel.CreatePluginFromPromptDirectory(
                    Path.Combine(promptsDirectory, "ContextualChat"));
                    
                Console.WriteLine("✅ Loaded file-based prompt plugins successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load prompt plugins: {ex.Message}. Using fallback prompts.");
            }
        }
        
        private async Task<string> GetSystemPromptAsync(bool useExtendedInstructions, bool enableAgentTools)
        {
            // Try to use file-based prompts first, fallback to appsettings.json
            try
            {
                var (pluginName, functionName) = (useExtendedInstructions, enableAgentTools) switch
                {
                    (true, true) => ("ChatAssistant", "AgentCodingChat"),   // Graham with tools
                    (true, false) => ("ChatAssistant", "CodingAssistant"),  // Graham without tools
                    (false, true) => ("ChatAssistant", "AgentChat"),        // Standard with tools
                    (false, false) => ("ChatAssistant", "StandardChat")     // Standard without tools
                };
                
                if (_chatAssistantPlugin != null && _chatAssistantPlugin.TryGetFunction(functionName, out var promptFunction))
                {
                    // Create a minimal kernel to invoke the prompt function
                    var tempKernel = Kernel.CreateBuilder().Build();
                    
                    var result = await tempKernel.InvokeAsync(promptFunction);
                    var promptContent = result.ToString();
                    
                    Console.WriteLine($"📝 Using file-based prompt: {functionName}");
                    return promptContent;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to get file-based prompt: {ex.Message}");
            }
            
            // Fallback to hardcoded prompts
            return useExtendedInstructions
                ? _settings.SystemPromptWithCoding
                : _settings.SystemPrompt;
        }

        private async Task<string> GetUserPromptFromTemplateAsync(string userMessage, string? context = null)
        {
            // Try to use file-based contextual prompts first
            try
            {
                var hasContext = !string.IsNullOrEmpty(context);
                var functionName = hasContext ? "WithContext" : "WithoutContext";
                
                if (_contextualChatPlugin != null && _contextualChatPlugin.TryGetFunction(functionName, out var promptFunction))
                {
                    var tempKernel = Kernel.CreateBuilder().Build();
                    var kernelArguments = new KernelArguments
                    {
                        ["userMessage"] = userMessage
                    };
                    
                    if (hasContext)
                    {
                        kernelArguments["context"] = context;
                    }
                    
                    var result = await tempKernel.InvokeAsync(promptFunction, kernelArguments);
                    Console.WriteLine($"📝 Using contextual template: {functionName}");
                    return result.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to get contextual template: {ex.Message}");
            }
            
            // Fallback: Read template content directly from files
            try
            {
                var hasContext = !string.IsNullOrEmpty(context);
                var templatePath = hasContext 
                    ? Path.Combine(AppContext.BaseDirectory, "Prompts", "ContextualChat", "WithContext", "skprompt.txt")
                    : Path.Combine(AppContext.BaseDirectory, "Prompts", "ContextualChat", "WithoutContext", "skprompt.txt");
                
                if (File.Exists(templatePath))
                {
                    var template = await File.ReadAllTextAsync(templatePath);
                    var result = template.Replace("{{$userMessage}}", userMessage);
                    if (hasContext)
                    {
                        result = result.Replace("{{$context}}", context);
                    }
                    Console.WriteLine($"📝 Using fallback template file: {Path.GetFileName(Path.GetDirectoryName(templatePath))}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to read fallback template files: {ex.Message}");
            }
            
            // Final fallback: construct basic user message without file dependencies
            return !string.IsNullOrEmpty(context)
                ? $"{userMessage}\n\n{context}"
                : userMessage;
        }

        private async Task<(string withContextInstruction, string withoutContextInstruction)> GetContextInstructionsAsync()
        {
            // Try to extract instruction strings from prompt templates
            try
            {
                if (_contextualChatPlugin != null)
                {
                    var tempKernel = Kernel.CreateBuilder().Build();
                    
                    // Get WithContext template
                    string withContextInstruction = "";
                    if (_contextualChatPlugin.TryGetFunction("WithContext", out var withContextFunction))
                    {
                        var result = await tempKernel.InvokeAsync(withContextFunction, new KernelArguments
                        {
                            ["userMessage"] = "",
                            ["context"] = ""
                        });
                        var template = result.ToString();
                        // Extract the instruction part after the context placeholder
                        var lines = template.Split('\n');
                        withContextInstruction = lines.LastOrDefault(l => l.Contains("SearchAllKnowledgeBasesAsync"))?.Trim() ?? "";
                    }
                    
                    // Get WithoutContext template
                    string withoutContextInstruction = "";
                    if (_contextualChatPlugin.TryGetFunction("WithoutContext", out var withoutContextFunction))
                    {
                        var result = await tempKernel.InvokeAsync(withoutContextFunction, new KernelArguments
                        {
                            ["userMessage"] = ""
                        });
                        var template = result.ToString();
                        // Extract the instruction part
                        var lines = template.Split('\n');
                        withoutContextInstruction = lines.LastOrDefault(l => l.Contains("SearchAllKnowledgeBasesAsync"))?.Trim() ?? "";
                    }
                    
                    if (!string.IsNullOrEmpty(withContextInstruction) && !string.IsNullOrEmpty(withoutContextInstruction))
                    {
                        Console.WriteLine("📝 Using context instructions from prompt files");
                        return (withContextInstruction, withoutContextInstruction);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to extract context instructions: {ex.Message}");
            }
            
            // Fallback: Read directly from template files
            try
            {
                var withContextPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "ContextualChat", "WithContext", "skprompt.txt");
                var withoutContextPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "ContextualChat", "WithoutContext", "skprompt.txt");
                
                if (File.Exists(withContextPath) && File.Exists(withoutContextPath))
                {
                    var withContextTemplate = await File.ReadAllTextAsync(withContextPath);
                    var withoutContextTemplate = await File.ReadAllTextAsync(withoutContextPath);
                    
                    // Extract instruction lines
                    var withContextInstruction = withContextTemplate.Split('\n').LastOrDefault(l => l.Contains("SearchAllKnowledgeBasesAsync"))?.Trim() ?? "";
                    var withoutContextInstruction = withoutContextTemplate.Split('\n').LastOrDefault(l => l.Contains("SearchAllKnowledgeBasesAsync"))?.Trim() ?? "";
                    
                    Console.WriteLine("📝 Using fallback instructions from template files");
                    return (withContextInstruction, withoutContextInstruction);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to read instruction fallback files: {ex.Message}");
            }
            
            // Final resort: return empty strings to avoid hardcoded prompts
            Console.WriteLine("⚠️ Unable to extract context instructions from any source");
            return ("", "");
        }

        [Experimental("SKEXP0070")]
        private Kernel GetOrCreateKernel(AiProvider provider, string? ollamaModel = null)
        {
            // Create a unique key for caching - include model for Ollama
            var key =
                provider == AiProvider.Ollama && !string.IsNullOrEmpty(ollamaModel)
                    ? $"{provider}:{ollamaModel}"
                    : provider.ToString();

            return _kernels.GetOrAdd(
                key,
                _ => new KernelFactory(_options).Create(provider, ollamaModel)
            );
        }

        public async Task PerformChat()
        {
            ChatHistory history = new ChatHistory();
            var systemPrompt = await GetSystemPromptAsync(false, false);
            history.AddSystemMessage(systemPrompt);
            var execSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = _settings.Temperature, // 0-1 or whatever you passed
                TopP = 1, // keep defaults or expose later
                MaxTokens = 1024,
            };

            while (true)
            {
                Console.Write("You: ");
                string? userMessage = Console.ReadLine();
                if (!string.IsNullOrEmpty(userMessage))
                {
                    if (userMessage.ToLower() == "exit")
                    {
                        break;
                    }
                    Console.WriteLine($"You:{userMessage}");
                    history.AddUserMessage(userMessage);
                    var enumerator = _chatCompletionService
                        .GetStreamingChatMessageContentsAsync(history, execSettings)
                        .GetAsyncEnumerator();
                    Console.Write($"Bot: ");
                    while (await enumerator.MoveNextAsync())
                    {
                        var response = enumerator.Current;
                        //       history.AddSystemMessage(response.Content);
                        Console.Write(response.Content);
                    }
                }
            }
            Console.WriteLine("Goodbye!");
        }

        [Experimental("SKEXP0070")]
        public virtual async Task<string> AskAsync(
            string userMessage,
            string? knowledgeId,
            ChatHistory chatHistory,
            double apiTemperature,
            AiProvider provider,
            bool useExtendedInstructions = false,
            string? ollamaModel = null,
            CancellationToken ct = default
        )
        {
            var kernel = GetOrCreateKernel(provider, ollamaModel);
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            string systemMessage = await GetSystemPromptAsync(useExtendedInstructions, false);
            chatHistory.AddSystemMessage(systemMessage);

            // 1. Vector search using new KnowledgeManager
            var searchResults = new List<KnowledgeSearchResult>();

            if (!string.IsNullOrEmpty(knowledgeId))
            {
                searchResults = await _knowledgeManager.SearchAsync(
                    knowledgeId,
                    userMessage,
                    limit: 10,
                    minRelevanceScore: 0.6,
                    cancellationToken: ct
                );
            }

            // 2. Convert to ordered distinct strings
            IEnumerable<string> contextChunks = searchResults
                .OrderBy(r => r.ChunkOrder)
                .Select(r => r.Text)
                .Distinct();

            var contextBlock = contextChunks.Any()
                ? string.Join("\n---\n", contextChunks)
                : "No relevant documentation was found for this query.";

            // 3. Build prompt & call GPT using template
            var userPrompt = await GetUserPromptFromTemplateAsync(userMessage, contextChunks.Any() ? contextBlock : null);
            chatHistory.AddUserMessage(userPrompt);

            double resolvedTemperature =
                apiTemperature == -1 ? _settings.Temperature : apiTemperature;
            PromptExecutionSettings execSettings;

            switch (provider)
            {
                case AiProvider.OpenAi:
                default:
                    execSettings = new OpenAIPromptExecutionSettings
                    {
                        Temperature = resolvedTemperature, // 0-1 or whatever you passed
                        TopP = 1, // keep defaults or expose later
                        MaxTokens = 4096,
                    };
                    break;
                case AiProvider.Google:
                    execSettings = new GeminiPromptExecutionSettings()
                    {
                        Temperature = resolvedTemperature,
                        TopP = 1,
                        MaxTokens = 4096,
                    };
                    break;
                case AiProvider.Anthropic:
                    execSettings = new AnthropicPromptExecutionSettings()
                    {
                        Temperature = resolvedTemperature,
                        TopP = 1,
                        MaxTokens = 4096,
                    };
                    break;
            }

            var responseStream = chatService.GetStreamingChatMessageContentsAsync(
                chatHistory,
                execSettings,
                null,
                ct
            );
            var assistant = new System.Text.StringBuilder();

            await foreach (var chunk in responseStream.WithCancellation(ct))
            {
                assistant.Append(chunk.Content);
            }

            return assistant.Length > 0
                ? assistant.ToString().Trim()
                : "There was no response from the AI.";
        }

        [Experimental("SKEXP0070")]
        public virtual async Task<AgentChatResponse> AskWithAgentAsync(
            string userMessage,
            string? knowledgeId,
            ChatHistory chatHistory,
            double apiTemperature,
            AiProvider provider,
            bool useExtendedInstructions = false,
            bool enableAgentTools = true,
            string? ollamaModel = null,
            CancellationToken ct = default
        )
        {
            var response = new AgentChatResponse();

            try
            {
                Console.WriteLine($"🤖 AskWithAgentAsync called - Provider: {provider}, Model: {ollamaModel}, EnableTools: {enableAgentTools}");
                
                var kernel = GetOrCreateKernel(provider, ollamaModel);

                // Register agent plugins if enabled
                if (enableAgentTools)
                {
                    Console.WriteLine("🔧 Registering agent plugins...");
                    await RegisterAgentPluginsAsync(kernel);
                    response.UsedAgentCapabilities = true;
                }
                else
                {
                    Console.WriteLine("⚠️ Agent tools disabled");
                }

                var chatService = kernel.GetRequiredService<IChatCompletionService>();
                string systemMessage = await GetSystemPromptAsync(useExtendedInstructions, true);
                chatHistory.AddSystemMessage(systemMessage);

                // Traditional knowledge search as fallback
                var searchResults = new List<KnowledgeSearchResult>();
                if (!string.IsNullOrEmpty(knowledgeId))
                {
                    searchResults = await _knowledgeManager.SearchAsync(
                        knowledgeId,
                        userMessage,
                        limit: 10,
                        minRelevanceScore: 0.6,
                        cancellationToken: ct
                    );
                    response.TraditionalSearchResults = searchResults;
                }

                // Build context for traditional search
                IEnumerable<string> contextChunks = searchResults
                    .OrderBy(r => r.ChunkOrder)
                    .Select(r => r.Text)
                    .Distinct();

                var contextBlock = contextChunks.Any()
                    ? string.Join("\n---\n", contextChunks)
                    : "No relevant documentation was found for this query.";

                // Add user message with explicit tool usage instruction
                var userPrompt = await GetUserPromptFromTemplateAsync(userMessage, contextChunks.Any() ? contextBlock : null);
                chatHistory.AddUserMessage(userPrompt);

                // Configure execution settings with tool calling
                double resolvedTemperature =
                    apiTemperature == -1 ? _settings.Temperature : apiTemperature;
                Console.WriteLine($"🔧 Configuring execution settings for provider: {provider}, enableAgentTools: {enableAgentTools}");
                PromptExecutionSettings execSettings = provider switch
                {
                    AiProvider.OpenAi => new OpenAIPromptExecutionSettings
                    {
                        Temperature = resolvedTemperature,
                        TopP = 1,
                        MaxTokens = 4096,
                        ToolCallBehavior = enableAgentTools
                            ? ToolCallBehavior.AutoInvokeKernelFunctions
                            : null,
                    },
                    AiProvider.Google => new GeminiPromptExecutionSettings
                    {
                        Temperature = resolvedTemperature,
                        TopP = 1,
                        MaxTokens = 4096,
                        ToolCallBehavior = enableAgentTools
                            ? GeminiToolCallBehavior.AutoInvokeKernelFunctions
                            : null,
                    },
                    AiProvider.Anthropic => new AnthropicPromptExecutionSettings
                    {
                        Temperature = resolvedTemperature,
                        TopP = 1,
                        MaxTokens = 4096,
                    },
                    AiProvider.Ollama => new OllamaPromptExecutionSettings
                    {
                        Temperature = (float)resolvedTemperature,
                        FunctionChoiceBehavior = enableAgentTools
                            ? FunctionChoiceBehavior.Auto()
                            : null,
                    },
                    _ => new OpenAIPromptExecutionSettings
                    {
                        Temperature = resolvedTemperature,
                        TopP = 1,
                        MaxTokens = 4096,
                        ToolCallBehavior = enableAgentTools
                            ? ToolCallBehavior.AutoInvokeKernelFunctions
                            : null,
                    },
                };

                // Debug: Log execution settings
                var hasToolCalling = execSettings switch
                {
                    OpenAIPromptExecutionSettings openAi => openAi.ToolCallBehavior != null,
                    GeminiPromptExecutionSettings gemini => gemini.ToolCallBehavior != null,
                    OllamaPromptExecutionSettings ollama => ollama.FunctionChoiceBehavior != null,
                    _ => false
                };
                Console.WriteLine($"🔧 Execution settings configured - HasToolCalling: {hasToolCalling}");

                // Log kernel plugins for debugging
                Console.WriteLine($"🔧 Kernel has {kernel.Plugins.Count} plugins registered:");
                foreach (var plugin in kernel.Plugins)
                {
                    Console.WriteLine($"  📦 Plugin: {plugin.Name}");
                    foreach (var function in plugin)
                    {
                        Console.WriteLine($"    🔧 Function: {function.Name} - {function.Description}");
                    }
                }

                // Execute with tool calling enabled
                Console.WriteLine("🚀 Starting chat completion with agent capabilities...");
                
                ChatMessageContent? chatResult;
                try
                {
                    // Use non-streaming for better tool call visibility
                    chatResult = await chatService.GetChatMessageContentAsync(
                        chatHistory,
                        execSettings,
                        kernel,
                        ct
                    );
                }
                catch (Exception ex) when (ex.Message.Contains("ModelDoesNotSupportToolsException") || 
                                          ex.Message.Contains("does not support tools") ||
                                          ex.GetType().Name.Contains("ModelDoesNotSupportToolsException"))
                {
                    Console.WriteLine($"⚠️ Model doesn't support tools, falling back to regular chat: {ex.Message}");
                    
                    // Remove the explicit tool usage instruction from user message to prevent loops
                    var lastUserMessage = chatHistory.LastOrDefault(m => m.Role == AuthorRole.User);
                    if (lastUserMessage != null)
                    {
                        // Get instructions from prompt files for cleaning
                        var (withContextInstruction, withoutContextInstruction) = await GetContextInstructionsAsync();
                        
                        // Clean up the message to remove tool-specific instructions
                        var cleanMessage = lastUserMessage.Content?
                            .Replace(withoutContextInstruction, "")
                            .Replace(withContextInstruction, "")
                            .Trim();
                        
                        chatHistory.RemoveAt(chatHistory.Count - 1);
                        chatHistory.AddUserMessage(cleanMessage ?? userMessage);
                    }
                    
                    // Fallback to regular AskAsync without agent capabilities
                    var fallbackResult = await AskAsync(userMessage, knowledgeId, new ChatHistory(), apiTemperature, provider, useExtendedInstructions, ollamaModel, ct);
                    return new AgentChatResponse
                    {
                        Response = fallbackResult,
                        UsedAgentCapabilities = false,
                        TraditionalSearchResults = new(),
                        ToolExecutions = new()
                    };
                }

                Console.WriteLine($"🔍 Chat result received. Content length: {chatResult?.Content?.Length ?? 0}");
                Console.WriteLine($"🔍 Function call results count: {chatResult?.Items?.OfType<FunctionCallContent>()?.Count() ?? 0}");
                Console.WriteLine($"🔍 Function result items count: {chatResult?.Items?.OfType<FunctionResultContent>()?.Count() ?? 0}");
                
                // Log any function calls/results
                if (chatResult?.Items != null)
                {
                    foreach (var item in chatResult.Items)
                    {
                        Console.WriteLine($"🔍 Item type: {item.GetType().Name}");
                        if (item is FunctionCallContent funcCall)
                        {
                            Console.WriteLine($"  🛠️ Function called: {funcCall.FunctionName}");
                            response.ToolExecutions.Add(new AgentToolExecution
                            {
                                ToolName = funcCall.FunctionName,
                                Summary = $"Called with args: {funcCall.Arguments}",
                                Success = true
                            });
                        }
                        if (item is FunctionResultContent funcResult)
                        {
                            Console.WriteLine($"  ✅ Function result: {funcResult.Result}");
                        }
                    }
                }

                var assistant = new System.Text.StringBuilder();
                assistant.Append(chatResult?.Content ?? "");

                response.Response =
                    assistant.Length > 0
                        ? assistant.ToString().Trim()
                        : "There was no response from the AI.";
                return response;
            }
            catch (Exception ex)
            {
                // Graceful degradation - fall back to traditional chat
                response.Response = await AskAsync(
                    userMessage,
                    knowledgeId,
                    chatHistory,
                    apiTemperature,
                    provider,
                    useExtendedInstructions,
                    ollamaModel,
                    ct
                );
                response.UsedAgentCapabilities = false;
                response.ToolExecutions.Add(
                    new AgentToolExecution
                    {
                        ToolName = "AgentFallback",
                        Summary = $"Agent mode failed, fell back to traditional chat: {ex.Message}",
                        Success = false,
                    }
                );
                return response;
            }
        }

        private async Task RegisterAgentPluginsAsync(Kernel kernel)
        {
            try
            {
                var crossKnowledgePlugin = _serviceProvider.GetRequiredService<CrossKnowledgeSearchPlugin>();
                kernel.Plugins.AddFromObject(crossKnowledgePlugin, "CrossKnowledgeSearch");
                
                // Debug: Log plugin registration
                Console.WriteLine($"✅ Registered CrossKnowledgeSearchPlugin. Total plugins: {kernel.Plugins.Count}");
                foreach (var plugin in kernel.Plugins)
                {
                    Console.WriteLine($"  Plugin: {plugin.Name} with {plugin.Count()} functions");
                    foreach (var function in plugin)
                    {
                        Console.WriteLine($"    Function: {function.Name} - {function.Description}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail - agent can work without plugins
                Console.WriteLine($"❌ Failed to register agent plugins: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to register agent plugins: {ex.Message}"
                );
            }
        }

        public async Task KnowledgeChatWithHistory(string collection)
        {
            //  var kernel = KernelHelper.GetKernel();
            //  var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            var systemPrompt = await GetSystemPromptAsync(false, false);
            history.AddSystemMessage(systemPrompt);
            Console.WriteLine("Assistant with Memory Mode. Type 'exit' to quit.\n");

            while (true)
            {
                Console.Write("You: ");
                string? userInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(userInput) || userInput.ToLower() == "exit")
                {
                    Console.WriteLine("👋 Goodbye!");
                    break;
                }

                // Step 1: Perform vector search using new KnowledgeManager
                var searchResults = await _knowledgeManager.SearchAsync(
                    collection,
                    userInput,
                    limit: 10,
                    minRelevanceScore: 0.6
                );

                var orderedContext = searchResults
                    .OrderBy(r => r.ChunkOrder)
                    .Select(r => r.Text)
                    .Distinct();

                string context = orderedContext.Any()
                    ? string.Join("\n---\n", orderedContext)
                    : "No relevant documentation was found for this query.";

                // Step 2: Add user question and context using template
                var userPrompt = await GetUserPromptFromTemplateAsync(userInput, orderedContext.Any() ? context : null);
                history.AddUserMessage(userPrompt);

                // Step 3: Stream GPT response and add to history
                var responseStream = _chatCompletionService.GetStreamingChatMessageContentsAsync(
                    history,
                    new OpenAIPromptExecutionSettings { Temperature = _settings.Temperature }
                );
                string assistantResponse = string.Empty;

                Console.Write("Assistant: ");
                await foreach (var message in responseStream)
                {
                    if (!string.IsNullOrWhiteSpace(message.Content))
                    {
                        Console.Write(message.Content);
                        assistantResponse += message.Content;
                    }
                }

                Console.WriteLine();
                history.AddAssistantMessage(assistantResponse);
            }
        }
    }
}
