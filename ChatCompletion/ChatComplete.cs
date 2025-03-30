using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ChatCompletion
{
    public class ChatComplete
    {
        IChatCompletionService _chatCompletionService;
        public ChatComplete()
        {
            var kernel = KernelHelper.GetKernel();
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        }

        public async Task PerformChat()
        {
            ChatHistory history = new ChatHistory();
            history.AddSystemMessage("Keep answers at 100 words minimum.");

            while(true)
            {
                Console.Write("You: ");
                string userMessage = Console.ReadLine();
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
        
    }
}