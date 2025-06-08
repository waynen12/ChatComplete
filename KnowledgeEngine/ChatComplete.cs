using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
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
        public ChatComplete(ISemanticTextMemory memory, string systemPrompt)
        {
            _memory = memory;
            var kernel = KernelHelper.GetKernel();
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            _systemPrompt = systemPrompt;

        }

        public async Task PerformChat()
        {
            ChatHistory history = new ChatHistory();
            history.AddSystemMessage("Keep answers at 100 words minimum.");

            while(true)
            {
                Console.Write("You: ");
                string? userMessage = Console.ReadLine();
                if (!string.IsNullOrEmpty(userMessage))
                {
                    if (userMessage.ToLower() == "exit")
                    {
                        break;
                    }
                    Console.WriteLine($"You:{userMessage}" );
                    history.AddUserMessage(userMessage);
                    var enumerator = _chatCompletionService.GetStreamingChatMessageContentsAsync(history).GetAsyncEnumerator();
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
            var searchResults = _memory.SearchAsync(collection, userInput, limit: 3, minRelevanceScore: 0.6);
            var contextChunks = new List<string>();

            await foreach (var result in searchResults)
            {
                if (!string.IsNullOrWhiteSpace(result.Metadata.Description))
                {
                    contextChunks.Add(result.Metadata.Description);
                }
            }

            string context = contextChunks.Any()
            ? string.Join("\n---\n", contextChunks)
            : "No relevant documentation was found for this query.";

            // Step 2: Add user question and context
            // Combine both as a single user message with context appended
            history.AddUserMessage($"""
            {userInput}

            Refer to the following documentation to help answer:
            {context}
            """);


           // Step 3: Stream GPT response and add to history
            var responseStream = chatService.GetStreamingChatMessageContentsAsync(history);
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