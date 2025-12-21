using System.Linq;
using System.Text;
using ChatCompletion.Config;
using Knowledge.Analytics.Models;
using Knowledge.Analytics.Services;
using Knowledge.Contracts.Types;
using Knowledge.Data.Interfaces;
using KnowledgeEngine.Agents.AgentFramework;
using KnowledgeEngine.Agents.Models;
using KnowledgeEngine.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KnowledgeEngine;

/// <summary>
/// Agent Framework implementation of ChatComplete.
/// Clean, modern approach using Microsoft.Agents.AI instead of Semantic Kernel.
/// </summary>
public class ChatCompleteAF
{
    private readonly KnowledgeManager _knowledgeManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ChatCompleteSettings _settings;
    private readonly AgentFactory _agentFactory;
    private readonly IOllamaRepository? _ollamaRepository;
    private readonly IUsageTrackingService? _usageTrackingService;

    public ChatCompleteAF(
        KnowledgeManager knowledgeManager,
        ChatCompleteSettings settings,
        IServiceProvider serviceProvider
    )
    {
        _knowledgeManager = knowledgeManager;
        _serviceProvider = serviceProvider;
        _settings = settings;
        _agentFactory = new AgentFactory(settings);

        // Try to get the Ollama repository for tool support detection
        _ollamaRepository = serviceProvider.GetService<IOllamaRepository>();

        // Try to get the usage tracking service for analytics
        _usageTrackingService = serviceProvider.GetService<IUsageTrackingService>();

        Console.WriteLine("✅ [AF] ChatCompleteAF initialized with Agent Framework");
    }

    #region Prompt Helpers (Reused from SK version)

    private async Task<string> GetSystemPromptAsync(
        bool useExtendedInstructions,
        bool enableAgentTools
    )
    {
        // Try to use file-based prompts first, fallback to appsettings.json
        try
        {
            var (_, functionName) = (useExtendedInstructions, enableAgentTools) switch
            {
                (true, true) => ("ChatAssistant", "AgentCodingChat"), // Graham with tools
                (true, false) => ("ChatAssistant", "CodingAssistant"), // Graham without tools
                (false, true) => ("ChatAssistant", "AgentChat"), // Standard with tools
                (false, false) => ("ChatAssistant", "StandardChat"), // Standard without tools
            };

            var promptsDirectory = Path.Combine(AppContext.BaseDirectory, "Prompts");
            var promptFilePath = Path.Combine(
                promptsDirectory,
                "ChatAssistant",
                functionName,
                "skprompt.txt"
            );

            if (File.Exists(promptFilePath))
            {
                var promptContent = await File.ReadAllTextAsync(promptFilePath);
                Console.WriteLine($"📝 [AF] Using file-based prompt: {functionName}");
                return promptContent;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [AF] Failed to get file-based prompt: {ex.Message}");
        }

        // Fallback to hardcoded prompts
        return useExtendedInstructions
            ? _settings.SystemPromptWithCoding
            : _settings.SystemPrompt;
    }

    private async Task<string> GetUserPromptFromTemplateAsync(
        string userMessage,
        string? context = null
    )
    {
        // Try to use file-based contextual prompts first
        try
        {
            var hasContext = !string.IsNullOrEmpty(context);
            var functionName = hasContext ? "WithContext" : "WithoutContext";

            var promptsDirectory = Path.Combine(AppContext.BaseDirectory, "Prompts");
            var promptFilePath = Path.Combine(
                promptsDirectory,
                "ContextualChat",
                functionName,
                "skprompt.txt"
            );

            if (File.Exists(promptFilePath))
            {
                var templateContent = await File.ReadAllTextAsync(promptFilePath);

                // Perform manual template variable substitution
                var processedContent = templateContent.Replace("{{$userMessage}}", userMessage);
                if (hasContext)
                {
                    processedContent = processedContent.Replace("{{$context}}", context);
                }

                Console.WriteLine($"📝 [AF] Using contextual template: {functionName}");
                return processedContent;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [AF] Failed to get contextual template: {ex.Message}");
        }

        // Final fallback: construct basic user message without file dependencies
        return !string.IsNullOrEmpty(context) ? $"{userMessage}\n\n{context}" : userMessage;
    }

    private static string StripThinkingSections(string response)
    {
        if (string.IsNullOrEmpty(response))
            return response;

        // Remove <think>...</think> sections (case insensitive, multiline)
        var pattern = @"<think>.*?</think>";
        var result = System.Text.RegularExpressions.Regex.Replace(
            response,
            pattern,
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.Singleline
        );

        // Clean up extra whitespace that may be left behind
        return result.Trim();
    }

    private async Task<(
        string withContextInstruction,
        string withoutContextInstruction
    )> GetContextInstructionsAsync()
    {
        // Try to extract instruction strings from prompt templates
        try
        {
            var promptsDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "Prompts",
                "ContextualChat"
            );

            // Get WithContext template
            string withContextInstruction = "";
            var withContextPath = Path.Combine(promptsDirectory, "WithContext", "skprompt.txt");
            if (File.Exists(withContextPath))
            {
                var template = await File.ReadAllTextAsync(withContextPath);
                // Process template with empty values to get instruction text
                var processedTemplate = template
                    .Replace("{{$userMessage}}", "")
                    .Replace("{{$context}}", "");
                var lines = processedTemplate.Split('\n');
                withContextInstruction =
                    lines.LastOrDefault(l => l.Contains("SearchAllKnowledgeBasesAsync"))?.Trim()
                    ?? "";
            }

            // Get WithoutContext template
            string withoutContextInstruction = "";
            var withoutContextPath = Path.Combine(
                promptsDirectory,
                "WithoutContext",
                "skprompt.txt"
            );
            if (File.Exists(withoutContextPath))
            {
                var template = await File.ReadAllTextAsync(withoutContextPath);
                // Process template with empty values to get instruction text
                var processedTemplate = template.Replace("{{$userMessage}}", "");
                var lines = processedTemplate.Split('\n');
                withoutContextInstruction =
                    lines.LastOrDefault(l => l.Contains("SearchAllKnowledgeBasesAsync"))?.Trim()
                    ?? "";
            }

            if (
                !string.IsNullOrEmpty(withContextInstruction)
                && !string.IsNullOrEmpty(withoutContextInstruction)
            )
            {
                return (withContextInstruction, withoutContextInstruction);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [AF] Failed to get context instructions from prompts: {ex.Message}");
        }

        // Fallback to hardcoded instructions
        return (
            "If you need to search across all knowledge bases or find information from other documentation, use the SearchAllKnowledgeBasesAsync function.",
            "If you need to search across all knowledge bases or find information from other documentation, use the SearchAllKnowledgeBasesAsync function."
        );
    }

    #endregion

    #region Agent Framework Chat Methods

    /// <summary>
    /// Simple RAG chat without agent tools using Agent Framework.
    /// </summary>
    public virtual async Task<string> AskAsync(
        string userMessage,
        string? knowledgeId,
        List<ChatMessage> chatHistory,
        double apiTemperature,
        AiProvider provider,
        bool useExtendedInstructions = false,
        string? ollamaModel = null,
        CancellationToken ct = default
    )
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        bool wasSuccessful = false;
        string? conversationId = null; // No conversation tracking - allows NULL in usage metrics
        int totalTokens = 0;

        try
        {
            Console.WriteLine(
                $"💬 [AF] AskAsync called - Provider: {provider}, Model: {ollamaModel}"
            );

            // Create agent without tools
            var systemMessage = await GetSystemPromptAsync(useExtendedInstructions, false);
            var agent = _agentFactory.CreateAgent(provider, systemMessage, ollamaModel);

            // Build chat history for Agent Framework
            var messages = new List<ChatMessage> { new(ChatRole.System, systemMessage) };
            messages.AddRange(chatHistory);

            // Perform vector search if knowledge base is specified
            var searchResults = new List<KnowledgeSearchResult>();
            if (!string.IsNullOrEmpty(knowledgeId))
            {
                Console.WriteLine($"🔍 [AF] Searching knowledge base: {knowledgeId}");
                searchResults = await _knowledgeManager.SearchAsync(
                    knowledgeId,
                    userMessage,
                    limit: 10,
                    minRelevanceScore: 0.3,
                    cancellationToken: ct
                );
            }

            // Build context from search results
            var contextChunks = searchResults
                .OrderBy(r => r.ChunkOrder)
                .Select(r => r.Text)
                .Distinct()
                .ToList();

            var contextBlock = contextChunks.Any()
                ? string.Join("\n---\n", contextChunks)
                : null;

            // Build user prompt with context
            var userPrompt = await GetUserPromptFromTemplateAsync(
                userMessage,
                contextChunks.Any() ? contextBlock : null
            );
            messages.Add(new ChatMessage(ChatRole.User, userPrompt));

            // Configure chat options
            var chatOptions = new ChatOptions
            {
                Temperature =
                    apiTemperature < 0 ? (float)_settings.Temperature : (float)apiTemperature,
                MaxOutputTokens = 4096,
            };

            Console.WriteLine("🚀 [AF] Starting chat completion...");

            // Execute chat using Agent Framework
            // Build a simple prompt from the last user message for RunAsync
            var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
            var promptText = lastUserMessage?.Text ?? userMessage;

            var agentResponse = await agent.RunAsync(promptText, cancellationToken: ct);

            var responseText = agentResponse?.ToString() ?? "There was no response from the AI.";

            wasSuccessful =
                !string.IsNullOrEmpty(responseText)
                && !responseText.Contains("There was no response from the AI.");

            // Estimate token usage (rough approximation)
            var inputContent = userMessage + systemMessage + (contextBlock ?? "");
            totalTokens = (inputContent.Length + responseText.Length) / 4;

            Console.WriteLine(
                $"✅ [AF] AskAsync completed - {totalTokens} tokens, {stopwatch.ElapsedMilliseconds}ms"
            );

            return StripThinkingSections(responseText);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [AF] Error in AskAsync: {ex.Message}");
            wasSuccessful = false;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Track usage metrics if service is available
            if (_usageTrackingService != null)
            {
                await TrackUsageAsync(
                    conversationId,
                    knowledgeId,
                    provider,
                    ollamaModel,
                    startTime,
                    stopwatch.Elapsed,
                    totalTokens,
                    wasSuccessful,
                    apiTemperature,
                    usedAgentCapabilities: false,
                    toolExecutions: 0,
                    ct
                );
            }
        }
    }

    /// <summary>
    /// Agent chat with tool calling support using Agent Framework.
    /// </summary>
    public virtual async Task<AgentChatResponse> AskWithAgentAsync(
        string userMessage,
        string? knowledgeId,
        List<ChatMessage> chatHistory,
        double apiTemperature,
        AiProvider provider,
        bool useExtendedInstructions = false,
        bool enableAgentTools = true,
        string? ollamaModel = null,
        CancellationToken ct = default
    )
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        bool wasSuccessful = false;
        string? conversationId = null; // No conversation tracking - allows NULL in usage metrics
        int totalTokens = 0;
        var response = new AgentChatResponse();

        try
        {
            Console.WriteLine(
                $"🤖 [AF] AskWithAgentAsync called - Provider: {provider}, Model: {ollamaModel}, EnableTools: {enableAgentTools}"
            );

            // Get system prompt
            var systemMessage = await GetSystemPromptAsync(useExtendedInstructions, true);

            // Determine if we should use tools
            var shouldUseTools = await ShouldEnableToolsAsync(
                provider,
                ollamaModel,
                enableAgentTools,
                ct
            );

            // Disable tool calling for Anthropic (third-party connector issue in AF too)
            if (provider == AiProvider.Anthropic && shouldUseTools)
            {
                Console.WriteLine(
                    "⚠️ [AF] Disabling tool calling for Anthropic in Agent Framework mode"
                );
                shouldUseTools = false;
            }

            Console.WriteLine($"🔧 [AF] Tool decision: shouldUseTools = {shouldUseTools}");

            AIAgent agent;
            if (shouldUseTools)
            {
                // Create agent with plugins
                Console.WriteLine("🔧 [AF] Registering agent plugins...");
                var plugins = RegisterAgentPlugins();
                agent = _agentFactory.CreateAgentWithPlugins(
                    provider,
                    systemMessage,
                    plugins,
                    ollamaModel
                );
                response.UsedAgentCapabilities = true;
            }
            else
            {
                // Create agent without tools
                Console.WriteLine("⚠️ [AF] Agent tools disabled");
                agent = _agentFactory.CreateAgent(provider, systemMessage, ollamaModel);
            }

            // Build chat history for Agent Framework
            var messages = new List<ChatMessage> { new(ChatRole.System, systemMessage) };
            messages.AddRange(chatHistory);

            // Traditional knowledge search as fallback (only if specific knowledge base is requested)
            var searchResults = new List<KnowledgeSearchResult>();
            if (!string.IsNullOrEmpty(knowledgeId))
            {
                Console.WriteLine(
                    $"🔍 [AF] Performing traditional knowledge search for knowledgeId: {knowledgeId}"
                );
                searchResults = await _knowledgeManager.SearchAsync(
                    knowledgeId,
                    userMessage,
                    limit: 10,
                    minRelevanceScore: 0.3,
                    cancellationToken: ct
                );
                response.TraditionalSearchResults = searchResults;
            }
            else
            {
                Console.WriteLine(
                    "🤖 [AF] Agent mode: No specific knowledge base selected, relying on agent tools"
                );
            }

            // Build context for traditional search
            var contextChunks = searchResults
                .OrderBy(r => r.ChunkOrder)
                .Select(r => r.Text)
                .Distinct()
                .ToList();

            var contextBlock = contextChunks.Any() ? string.Join("\n---\n", contextChunks) : null;

            // Build user prompt with context
            var userPrompt = await GetUserPromptFromTemplateAsync(
                userMessage,
                contextChunks.Any() ? contextBlock : null
            );
            messages.Add(new ChatMessage(ChatRole.User, userPrompt));

            // Configure chat options
            var chatOptions = new ChatOptions
            {
                Temperature =
                    apiTemperature < 0 ? (float)_settings.Temperature : (float)apiTemperature,
                MaxOutputTokens = 4096,
            };

            Console.WriteLine("🚀 [AF] Starting agent chat completion with tools...");

            // Execute chat using Agent Framework
            AgentRunResponse agentResult;
            try
            {
                // Build a simple prompt from the last user message for RunAsync
                var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
                var promptText = lastUserMessage?.Text ?? userMessage;

                agentResult = await agent.RunAsync(promptText, cancellationToken: ct);
            }
            catch (Exception ex)
                when (ex.Message.Contains("does not support tools")
                    || ex.Message.Contains("ModelDoesNotSupportToolsException")
                )
            {
                Console.WriteLine(
                    $"⚠️ [AF] Model doesn't support tools, falling back to regular chat: {ex.Message}"
                );

                // Update model tool support status to false
                await UpdateModelToolSupportAsync(provider, ollamaModel, false, ct);

                // Fallback to AskAsync without agent capabilities
                var fallbackResult = await AskAsync(
                    userMessage,
                    knowledgeId,
                    new List<ChatMessage>(),
                    apiTemperature,
                    provider,
                    useExtendedInstructions,
                    ollamaModel,
                    ct
                );
                return new AgentChatResponse
                {
                    Response = fallbackResult,
                    UsedAgentCapabilities = false,
                    TraditionalSearchResults = new(),
                    ToolExecutions = new(),
                };
            }

            var responseText = agentResult?.ToString() ?? "There was no response from the AI.";

            Console.WriteLine(
                $"🔍 [AF] Chat result received. Content length: {responseText.Length}"
            );

            // TODO: Extract tool execution info from agentResult when AF exposes it
            // For now, we'll assume tools were used if they were enabled

            wasSuccessful =
                !string.IsNullOrEmpty(responseText)
                && !responseText.Contains("There was no response from the AI.");

            // Estimate token usage (rough approximation)
            var inputContent = userMessage + systemMessage + (contextBlock ?? "");
            totalTokens = (inputContent.Length + responseText.Length) / 4;

            Console.WriteLine(
                $"✅ [AF] AskWithAgentAsync completed - {totalTokens} tokens, {stopwatch.ElapsedMilliseconds}ms"
            );

            response.Response = StripThinkingSections(responseText);
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [AF] Error in AskWithAgentAsync: {ex.Message}");
            wasSuccessful = false;

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
        finally
        {
            stopwatch.Stop();

            // Track usage metrics if service is available
            if (_usageTrackingService != null)
            {
                await TrackUsageAsync(
                    conversationId,
                    knowledgeId,
                    provider,
                    ollamaModel,
                    startTime,
                    stopwatch.Elapsed,
                    totalTokens,
                    wasSuccessful,
                    apiTemperature,
                    response.UsedAgentCapabilities,
                    response.ToolExecutions?.Count ?? 0,
                    ct
                );
            }
        }
    }

    /// <summary>
    /// Simple RAG chat with streaming support using Agent Framework.
    /// </summary>
    public virtual async IAsyncEnumerable<string> AskStreamingAsync(
        string userMessage,
        string? knowledgeId,
        List<ChatMessage> chatHistory,
        double apiTemperature,
        AiProvider provider,
        bool useExtendedInstructions = false,
        string? ollamaModel = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        bool wasSuccessful = false;
        string? conversationId = null; // No conversation tracking - allows NULL in usage metrics
        int totalTokens = 0;
        var fullResponse = new StringBuilder();

        try
        {
            Console.WriteLine(
                $"💬📡 [AF] AskStreamingAsync called - Provider: {provider}, Model: {ollamaModel}"
            );

            // Create agent without tools
            var systemMessage = await GetSystemPromptAsync(useExtendedInstructions, false);
            var agent = _agentFactory.CreateAgent(provider, systemMessage, ollamaModel);

            // Build chat history for Agent Framework
            var messages = new List<ChatMessage> { new(ChatRole.System, systemMessage) };
            messages.AddRange(chatHistory);

            // Perform vector search if knowledge base is specified
            var searchResults = new List<KnowledgeSearchResult>();
            if (!string.IsNullOrEmpty(knowledgeId))
            {
                Console.WriteLine($"🔍 [AF] Searching knowledge base: {knowledgeId}");
                searchResults = await _knowledgeManager.SearchAsync(
                    knowledgeId,
                    userMessage,
                    limit: 10,
                    minRelevanceScore: 0.3,
                    cancellationToken: ct
                );
            }

            // Build context from search results
            var contextChunks = searchResults
                .OrderBy(r => r.ChunkOrder)
                .Select(r => r.Text)
                .Distinct()
                .ToList();

            var contextBlock = contextChunks.Any()
                ? string.Join("\n---\n", contextChunks)
                : null;

            // Build user prompt with context
            var userPrompt = await GetUserPromptFromTemplateAsync(
                userMessage,
                contextChunks.Any() ? contextBlock : null
            );
            messages.Add(new ChatMessage(ChatRole.User, userPrompt));

            Console.WriteLine("🚀📡 [AF] Starting streaming chat completion...");

            // Execute streaming chat using Agent Framework
            var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
            var promptText = lastUserMessage?.Text ?? userMessage;

            // Note: Temperature configuration is handled at ChatClient level in AgentFactory
            // Agent Framework doesn't support per-request temperature in RunStreamingAsync

            // Stream the response
            await foreach (var update in agent.RunStreamingAsync(promptText, cancellationToken: ct))
            {
                var textChunk = update?.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(textChunk))
                {
                    fullResponse.Append(textChunk);
                    yield return textChunk;
                }
            }

            var responseText = fullResponse.ToString();
            wasSuccessful = !string.IsNullOrEmpty(responseText);

            // Estimate token usage (rough approximation)
            var inputContent = userMessage + systemMessage + (contextBlock ?? "");
            totalTokens = (inputContent.Length + responseText.Length) / 4;

            Console.WriteLine(
                $"✅📡 [AF] AskStreamingAsync completed - {totalTokens} tokens, {stopwatch.ElapsedMilliseconds}ms"
            );
        }
        finally
        {
            stopwatch.Stop();

            // Track usage metrics if service is available
            if (_usageTrackingService != null)
            {
                await TrackUsageAsync(
                    conversationId,
                    knowledgeId,
                    provider,
                    ollamaModel,
                    startTime,
                    stopwatch.Elapsed,
                    totalTokens,
                    wasSuccessful,
                    apiTemperature,
                    usedAgentCapabilities: false,
                    toolExecutions: 0,
                    ct
                );
            }
        }
    }

    /// <summary>
    /// Agent chat with tool calling and streaming support using Agent Framework.
    /// </summary>
    public virtual async IAsyncEnumerable<string> AskWithAgentStreamingAsync(
        string userMessage,
        string? knowledgeId,
        List<ChatMessage> chatHistory,
        double apiTemperature,
        AiProvider provider,
        bool useExtendedInstructions = false,
        bool enableAgentTools = true,
        string? ollamaModel = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        bool wasSuccessful = false;
        string? conversationId = null; // No conversation tracking - allows NULL in usage metrics
        int totalTokens = 0;
        var fullResponse = new StringBuilder();
        var toolExecutionCount = 0;
        bool usedAgentCapabilities = false;

        try
        {
            Console.WriteLine(
                $"🤖📡 [AF] AskWithAgentStreamingAsync called - Provider: {provider}, Model: {ollamaModel}, EnableTools: {enableAgentTools}"
            );

            // Get system prompt
            var systemMessage = await GetSystemPromptAsync(useExtendedInstructions, true);

            // Determine if we should use tools
            var shouldUseTools = await ShouldEnableToolsAsync(
                provider,
                ollamaModel,
                enableAgentTools,
                ct
            );

            // Disable tool calling for Anthropic (third-party connector issue)
            if (provider == AiProvider.Anthropic && shouldUseTools)
            {
                Console.WriteLine(
                    "⚠️📡 [AF] Disabling tool calling for Anthropic in Agent Framework streaming mode"
                );
                shouldUseTools = false;
            }

            Console.WriteLine(
                $"🛠️📡 [AF] Tools enabled: {shouldUseTools} for provider {provider}"
            );

            // Create agent with or without tools
            AIAgent agent;
            if (shouldUseTools)
            {
                var plugins = RegisterAgentPlugins();
                agent = _agentFactory.CreateAgentWithPlugins(
                    provider,
                    systemMessage,
                    plugins,
                    ollamaModel
                );
                usedAgentCapabilities = true;
            }
            else
            {
                agent = _agentFactory.CreateAgent(provider, systemMessage, ollamaModel);
                usedAgentCapabilities = false;
            }

            // Build chat history
            var messages = new List<ChatMessage> { new(ChatRole.System, systemMessage) };
            messages.AddRange(chatHistory);

            // Perform vector search if knowledge base is specified
            var searchResults = new List<KnowledgeSearchResult>();
            if (!string.IsNullOrEmpty(knowledgeId))
            {
                Console.WriteLine($"🔍📡 [AF] Searching knowledge base: {knowledgeId}");
                searchResults = await _knowledgeManager.SearchAsync(
                    knowledgeId,
                    userMessage,
                    limit: 10,
                    minRelevanceScore: 0.3,
                    cancellationToken: ct
                );
            }

            // Build context from search results
            var contextChunks = searchResults
                .OrderBy(r => r.ChunkOrder)
                .Select(r => r.Text)
                .Distinct()
                .ToList();

            var contextBlock = contextChunks.Any()
                ? string.Join("\n---\n", contextChunks)
                : null;

            // Build user prompt with context
            var userPrompt = await GetUserPromptFromTemplateAsync(
                userMessage,
                contextChunks.Any() ? contextBlock : null
            );

            // Add tool usage instructions if tools are enabled
            if (shouldUseTools)
            {
                var (withContextInstruction, withoutContextInstruction) =
                    await GetContextInstructionsAsync();
                var toolInstruction = contextChunks.Any()
                    ? withContextInstruction
                    : withoutContextInstruction;
                userPrompt = $"{userPrompt}\n\n{toolInstruction}";
            }

            messages.Add(new ChatMessage(ChatRole.User, userPrompt));

            Console.WriteLine("🚀📡 [AF] Starting streaming agent chat with tools...");

            // Execute streaming chat using Agent Framework
            var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
            var promptText = lastUserMessage?.Text ?? userMessage;

            // Note: Temperature configuration is handled at ChatClient level in AgentFactory

            // Stream the response
            await foreach (var update in agent.RunStreamingAsync(promptText, cancellationToken: ct))
            {
                var textChunk = update?.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(textChunk))
                {
                    fullResponse.Append(textChunk);

                    // Strip thinking sections before yielding
                    var cleanedChunk = StripThinkingSections(textChunk);
                    if (!string.IsNullOrEmpty(cleanedChunk))
                    {
                        yield return cleanedChunk;
                    }
                }

                // Count tool executions if available
                // Note: Agent Framework may not expose tool execution details in streaming updates
                // This is a simplified approach - actual tool counting may require different handling
                if (update?.Contents != null)
                {
                    toolExecutionCount = update.Contents.Count(c => c is FunctionCallContent);
                }
            }

            var responseText = fullResponse.ToString();
            wasSuccessful = !string.IsNullOrEmpty(responseText);

            // Estimate token usage (rough approximation)
            var inputContent = userMessage + systemMessage + (contextBlock ?? "");
            totalTokens = (inputContent.Length + responseText.Length) / 4;

            Console.WriteLine(
                $"✅📡 [AF] AskWithAgentStreamingAsync completed - {totalTokens} tokens, {toolExecutionCount} tool calls, {stopwatch.ElapsedMilliseconds}ms"
            );
        }
        finally
        {
            stopwatch.Stop();

            // Track usage metrics if service is available
            if (_usageTrackingService != null)
            {
                await TrackUsageAsync(
                    conversationId,
                    knowledgeId,
                    provider,
                    ollamaModel,
                    startTime,
                    stopwatch.Elapsed,
                    totalTokens,
                    wasSuccessful,
                    apiTemperature,
                    usedAgentCapabilities,
                    toolExecutionCount,
                    ct
                );
            }
        }
    }

    #endregion

    #region Helper Methods

    private Dictionary<string, object> RegisterAgentPlugins()
    {
        try
        {
            var plugins = new Dictionary<string, object>();

            // Register all AF plugins
            var crossKnowledgePlugin =
                _serviceProvider.GetRequiredService<CrossKnowledgeSearchPlugin>();
            plugins["CrossKnowledgeSearch"] = crossKnowledgePlugin;

            var modelRecommendationPlugin =
                _serviceProvider.GetRequiredService<ModelRecommendationPlugin>();
            plugins["ModelRecommendation"] = modelRecommendationPlugin;

            var knowledgeAnalyticsPlugin =
                _serviceProvider.GetRequiredService<KnowledgeAnalyticsPlugin>();
            plugins["KnowledgeAnalytics"] = knowledgeAnalyticsPlugin;

            var systemHealthPlugin = _serviceProvider.GetRequiredService<SystemHealthPlugin>();
            plugins["SystemHealth"] = systemHealthPlugin;

            Console.WriteLine(
                $"✅ [AF] Registered {plugins.Count} agent plugins: CrossKnowledgeSearch, ModelRecommendation, KnowledgeAnalytics, SystemHealth"
            );

            return plugins;
        }
        catch (Exception ex)
        {
            // Log but don't fail - agent can work without plugins
            Console.WriteLine($"❌ [AF] Failed to register agent plugins: {ex.Message}");
            return new Dictionary<string, object>();
        }
    }

    private async Task<bool> ShouldEnableToolsAsync(
        AiProvider provider,
        string? ollamaModel,
        bool enableAgentTools,
        CancellationToken ct
    )
    {
        // For non-Ollama providers, always try tools if requested
        if (provider != AiProvider.Ollama)
        {
            return enableAgentTools;
        }

        // For Ollama, check the SupportsTools database flag
        if (_ollamaRepository != null && !string.IsNullOrEmpty(ollamaModel))
        {
            try
            {
                var modelRecord = await _ollamaRepository.GetModelAsync(ollamaModel, ct);
                if (modelRecord?.SupportsTools.HasValue == true)
                {
                    Console.WriteLine(
                        $"🔍 [AF] Ollama model {ollamaModel} SupportsTools = {modelRecord.SupportsTools.Value}"
                    );
                    return enableAgentTools && modelRecord.SupportsTools.Value;
                }
                else
                {
                    Console.WriteLine(
                        $"🔍 [AF] Ollama model {ollamaModel} has unknown tool support, will attempt and detect"
                    );
                    return enableAgentTools;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"⚠️ [AF] Failed to check Ollama model tool support: {ex.Message}"
                );
            }
        }

        Console.WriteLine("⚠️ [AF] Ollama model tool support unknown - defaulting to disabled");
        return false;
    }

    private async Task UpdateModelToolSupportAsync(
        AiProvider provider,
        string? ollamaModel,
        bool supportsTools,
        CancellationToken ct
    )
    {
        if (
            provider == AiProvider.Ollama
            && _ollamaRepository != null
            && !string.IsNullOrEmpty(ollamaModel)
        )
        {
            try
            {
                var modelRecord = await _ollamaRepository.GetModelAsync(ollamaModel, ct);
                if (modelRecord != null)
                {
                    if (
                        !modelRecord.SupportsTools.HasValue
                        || modelRecord.SupportsTools.Value != supportsTools
                    )
                    {
                        await _ollamaRepository.UpdateSupportsToolsAsync(
                            ollamaModel,
                            supportsTools,
                            ct
                        );
                        Console.WriteLine(
                            $"✅ [AF] Updated Ollama model {ollamaModel} SupportsTools = {supportsTools}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"⚠️ [AF] Failed to update Ollama model tool support: {ex.Message}"
                );
            }
        }
    }

    private string GetModelNameForTracking(AiProvider provider, string? ollamaModel)
    {
        if (provider == AiProvider.Ollama)
        {
            return ollamaModel ?? _settings.OllamaModel ?? "llama3.2:3b";
        }

        return provider.ToString();
    }

    private async Task TrackUsageAsync(
        string? conversationId,
        string? knowledgeId,
        AiProvider provider,
        string? ollamaModel,
        DateTime startTime,
        TimeSpan responseTime,
        int totalTokens,
        bool wasSuccessful,
        double apiTemperature,
        bool usedAgentCapabilities,
        int toolExecutions,
        CancellationToken ct
    )
    {
        try
        {
            var modelName = GetModelNameForTracking(provider, ollamaModel);

            var usageMetric = new UsageMetric
            {
                ConversationId = conversationId, // Allow NULL - not all usage is tied to a saved conversation
                KnowledgeId = knowledgeId,
                Provider = provider,
                ModelName = modelName,
                Timestamp = startTime,
                ResponseTime = responseTime,
                InputTokens = totalTokens / 2, // Rough split between input and output
                OutputTokens = totalTokens - (totalTokens / 2),
                WasSuccessful = wasSuccessful,
                Temperature = apiTemperature < 0 ? _settings.Temperature : apiTemperature,
                UsedAgentCapabilities = usedAgentCapabilities,
                ToolExecutions = toolExecutions,
            };

            await _usageTrackingService!.TrackUsageAsync(usageMetric, ct);
            Console.WriteLine(
                $"📊 [AF] Usage tracked: {provider}/{modelName} - {totalTokens} tokens - {responseTime.TotalMilliseconds}ms - Success: {wasSuccessful} - Agent: {usedAgentCapabilities}"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [AF] Failed to track usage: {ex.Message}");
        }
    }

    #endregion
}
