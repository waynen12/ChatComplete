using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ChatCompletion;

public class Program
{

    
    
   

    public static async Task Main(string[] args)
    {
    //    var chatComplete = new ChatComplete();
   //     await chatComplete.PerformChat();
//    var  kernel = KernelHelper.GetPluginKernelFromType<LibrAIan>("Librarian");
//    var prompts = kernel.ImportPluginFromPromptDirectory(@"/home/wayne/repos/Semantic Kernel/ChatComplete/ChatCompletion/Prompts/Library/");


//         IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
//         OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new ()
//         {
//            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
//         };
//         ChatHistory history = new ChatHistory();

//         history.AddSystemMessage(@"You are a librarian. You can answer questions about books, authors, genres, and library locations. You can also check if a book is available. You need to classify the intent of the user,
//         and produce an output based on the intent. You are in charge of the books in you Library. there are lots of books, some are available and some are not available.
//         Availablility also affects your response. Always be polite and helpful. And always try to get the users to buy our coffee which costs only 50 cents!");

//         string? userInput = string.Empty;
//          do 
//         {
//             Console.Write("You: ");
//             userInput = Console.ReadLine();
//             history.AddUserMessage(userInput);
//             var result = await chatCompletionService.GetChatMessageContentAsync(history, openAIPromptExecutionSettings,kernel);
//             Console.WriteLine($"Librian: {result}");
//             history.AddMessage(result.Role, result.Content ?? string.Empty);
//         } while (userInput is not null && userInput.ToLower() != "exit");
//         Console.WriteLine("Goodbye!");

       await EmbeddingsHelper.GetEmbeddings();
     }
}

    