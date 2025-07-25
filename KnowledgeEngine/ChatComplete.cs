using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ChatCompletion.Config;
using Knowledge.Contracts.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Anthropic;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Memory;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

namespace ChatCompletion
{
    public class ChatComplete
    {
        IChatCompletionService _chatCompletionService;
        ISemanticTextMemory _memory;
        ChatCompleteSettings _settings;
        private readonly ConcurrentDictionary<AiProvider, Kernel> _kernels = new();

        public ChatComplete(
            ISemanticTextMemory memory,
            ChatCompleteSettings settings
        )
        {
            _memory = memory;
           _settings = settings;
        }
        
        [Experimental("SKEXP0070")]
        private Kernel GetOrCreateKernel(AiProvider provider)
            => _kernels.GetOrAdd(provider, p => KernelHelper.GetKernel(_settings, p));

        public async Task PerformChat()
        {
            ChatHistory history = new ChatHistory();
            history.AddSystemMessage("Keep answers at 100 words minimum.");
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
            CancellationToken ct = default
        )
        {
            var kernel = GetOrCreateKernel(provider);
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            string systemMessage = useExtendedInstructions ? _settings.SystemPromptWithCoding : _settings.SystemPrompt;
            chatHistory.AddSystemMessage(systemMessage);

            // 1. Vector search and collect tuples
            var temp = new List<(int Order, string Content)>();

            if (!string.IsNullOrEmpty(knowledgeId))
            {
                await foreach (
                    var result in _memory.SearchAsync(
                        knowledgeId,
                        userMessage,
                        limit: 10,
                        minRelevanceScore: 0.6,
                        cancellationToken: ct
                    )
                )
                {
                    if (string.IsNullOrWhiteSpace(result.Metadata.Description))
                        continue;

                    var orderStr = result
                        .Metadata.AdditionalMetadata?.Split(',')
                        .FirstOrDefault(m =>
                            m.StartsWith("ChunkOrder=", StringComparison.OrdinalIgnoreCase)
                        )
                        ?.Split('=')[1];

                    int order = int.TryParse(orderStr, out var o) ? o : int.MaxValue;
                    temp.Add((order, result.Metadata.Description));
                }
            }

            // 2. Convert to ordered distinct strings
            IEnumerable<string> contextChunks = temp.OrderBy(t => t.Order)
                .Select(t => t.Content)
                .Distinct();

            var contextBlock = contextChunks.Any()
                ? string.Join("\n---\n", contextChunks)
                : "No relevant documentation was found for this query.";

            // 3. Build prompt & call GPT (unchanged)
            chatHistory.AddUserMessage(
                $"""
                {userMessage}

                Refer to the following documentation to help answer:
                {contextBlock}
                """
            );

            double resolvedTemperature = apiTemperature == -1 ? _settings.Temperature : apiTemperature;
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

            return assistant.Length > 0 ? assistant.ToString().Trim() : "There was no response from the AI.";
        }

        public async Task KnowledgeChatWithHistory(string collection)
        {
          //  var kernel = KernelHelper.GetKernel();
          //  var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddSystemMessage(_settings.SystemPrompt);
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

                // Step 1: Perform vector search
                var searchResults = _memory.SearchAsync(
                    collection,
                    userInput,
                    limit: 8,
                    minRelevanceScore: 0.6
                );
                var contextChunks = new List<(int Order, string Content)>();

                await foreach (
                    var result in _memory.SearchAsync(
                        collection,
                        userInput,
                        limit: 10,
                        minRelevanceScore: 0.6
                    )
                )
                {
                    if (!string.IsNullOrWhiteSpace(result.Metadata.Description))
                    {
                        var chunkText = result.Metadata.Description;
                        var chunkOrderStr = result
                            .Metadata.AdditionalMetadata?.Split(',') // optional, if multiple values
                            .FirstOrDefault(m =>
                                m.StartsWith("ChunkOrder=", StringComparison.OrdinalIgnoreCase)
                            )
                            ?.Split('=')[1];

                        if (int.TryParse(chunkOrderStr, out int chunkOrder))
                        {
                            contextChunks.Add((chunkOrder, chunkText));
                        }
                        else
                        {
                            // fallback if missing
                            contextChunks.Add((int.MaxValue, chunkText));
                        }
                    }
                }
                ;

                var orderedContext = contextChunks
                    .OrderBy(c => c.Order)
                    .Select(c => c.Content)
                    .Distinct();

                string context = orderedContext.Any()
                    ? string.Join("\n---\n", orderedContext)
                    : "No relevant documentation was found for this query.";

                // Step 2: Add user question and context
                // Combine both as a single user message with context appended
                history.AddUserMessage(
                    $"""
                    {userInput}

                    Refer to the following documentation to help answer:
                    {context}
                    """
                );

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
