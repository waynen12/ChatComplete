using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Kernel.CreateBuilder();
        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(openAiApiKey))
        {
            throw new InvalidOperationException("The OpenAI API key is not set in the environment variables.");
        }
        builder.AddOpenAIChatCompletion("gpt-4o", openAiApiKey);
        Kernel kernel = builder.Build();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        //ChatMessageContent response = await chatCompletionService.GetChatMessageContentAsync("Hello, how are you?");
        //response.Dump();
        //Console.WriteLine(response.Content);

        ChatHistory history = new ChatHistory();
        history.AddSystemMessage("Keep answers at 100 words minimum.");

        while(true)
        {
            Console.Write("You: ");
            string userMessage = Console.ReadLine();
            if (userMessage.ToLower() == "exit")
            {
                break;
            }
            Console.WriteLine($"You:{userMessage}" );
            history.AddUserMessage(userMessage);
           // ChatMessageContent response = await chatCompletionService.GetChatMessageContentAsync(history);
            var enumerator = chatCompletionService.GetStreamingChatMessageContentsAsync(history).GetAsyncEnumerator();
            Console.Write($"Bot: ");
            while (await enumerator.MoveNextAsync())
            {
                var response = enumerator.Current;
                history.AddSystemMessage(response.Content);
                Console.Write(response.Content);
            }
            
           // history.AddSystemMessage(response.Content);
          //  Console.WriteLine($"Bot: {response.Content}");
        }
        Console.WriteLine("Goodbye!");
    }
}