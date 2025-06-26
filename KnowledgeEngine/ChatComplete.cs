using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

namespace ChatCompletion
{
    public class ChatComplete
    {
        IChatCompletionService _chatCompletionService;
        ISemanticTextMemory _memory;
        string _systemPrompt;
        double _temperature;

        public ChatComplete(
            ISemanticTextMemory memory,
            Kernel kernel,
            string systemPrompt,
            double temperature = 0.7
        )
        {
            _memory = memory;
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            _systemPrompt = systemPrompt;
            _temperature = temperature;
        }

        public async Task PerformChat()
        {
            ChatHistory history = new ChatHistory();
            history.AddSystemMessage("Keep answers at 100 words minimum.");
            var execSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = _temperature, // 0-1 or whatever you passed
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

        public async Task<string> AskAsync(
            string userMessage,
            string? knowledgeId,
            double apiTemperature,
            CancellationToken ct = default
        )
        {
            var kernel = KernelHelper.GetKernel();
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(_systemPrompt);

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

            double resolvedTemperature = apiTemperature == -1 ? _temperature : apiTemperature;
            var execSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = resolvedTemperature, // 0-1 or whatever you passed
                TopP = 1, // keep defaults or expose later
                MaxTokens = 4096,
            };
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

            return assistant.ToString().Trim();
        }

        public async Task KnowledgeChatWithHistory(string collection)
        {
            var kernel = KernelHelper.GetKernel();
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddSystemMessage(_systemPrompt);
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
                var responseStream = chatService.GetStreamingChatMessageContentsAsync(
                    history,
                    new OpenAIPromptExecutionSettings { Temperature = _temperature }
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
