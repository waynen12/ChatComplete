using ChatCompletion;
using ChatCompletion.Config;
using Knowledge.Contracts.Types;
using KnowledgeEngine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace KnowledgeManager.Tests.KnowledgeEngine;

internal sealed class SpyChatComplete : ChatComplete
{
    public int? LastHistoryCount { get; private set; }

    public SpyChatComplete()
        : base(
            knowledgeManager:  null!,                 // not used by spy
            settings: new ChatCompleteSettings { Temperature = 0.7 },
            serviceProvider: null!)                   // not used by spy
    { }

    public override Task<string> AskAsync(
        string userMessage, string? knowledgeId, ChatHistory chatHistory,
        double apiTemperature, AiProvider provider, bool useExtendedInstructions = false, 
        string? ollamaModel = null, CancellationToken ct = default)
    {
        LastHistoryCount = chatHistory.Count;
        return Task.FromResult("stub-reply");
    }

    // --- local helper --------------------------------------------------------
    private static Kernel BuildStubKernel()
    {
        var builder = Kernel.CreateBuilder();

        // Inject a no-op chat-completion implementation
        builder.Services.AddSingleton<IChatCompletionService, NullChatCompletionService>();

        return builder.Build();
    }

    private sealed class NullChatCompletionService : IChatCompletionService
    {
        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        IAsyncEnumerable<StreamingChatMessageContent> IChatCompletionService.GetStreamingChatMessageContentsAsync(ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings, Kernel? kernel,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<ChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory history, PromptExecutionSettings? settings = null,
            Kernel? kernel = null, CancellationToken cancellationToken = default)
            => AsyncEnumerable.Empty<ChatMessageContent>();

        public Task<ChatMessageContent> GetChatMessageContentAsync(
            ChatHistory history, PromptExecutionSettings? settings = null,
            Kernel? kernel = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatMessageContent(AuthorRole.Assistant, string.Empty));

        public IReadOnlyDictionary<string, object?> Attributes { get; }
    }
}