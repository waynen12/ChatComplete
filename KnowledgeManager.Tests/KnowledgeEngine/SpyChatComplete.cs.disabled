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
    public string? LastKnowledgeId { get; private set; }
    public double LastTemperature { get; private set; }
    public AiProvider LastProvider { get; private set; }
    public bool LastUseExtendedInstructions { get; private set; }
    public string? LastOllamaModel { get; private set; }

    public SpyChatComplete()
        : base(
            knowledgeManager:  null!,                 // not used by spy
            settings: new ChatCompleteSettings { Temperature = 0.7 },
            serviceProvider: CreateMockServiceProvider())
    { }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var services = new ServiceCollection();
        // Add any required services here - currently ChatComplete only tries to get SqliteOllamaRepository
        // which is optional (can be null), so we don't need to register it
        return services.BuildServiceProvider();
    }

    public override Task<string> AskAsync(
        string userMessage, string? knowledgeId, ChatHistory chatHistory,
        double apiTemperature, AiProvider provider, bool useExtendedInstructions = false,
        string? ollamaModel = null, CancellationToken ct = default)
    {
        LastHistoryCount = chatHistory.Count;
        LastKnowledgeId = knowledgeId;
        LastTemperature = apiTemperature;
        LastProvider = provider;
        LastUseExtendedInstructions = useExtendedInstructions;
        LastOllamaModel = ollamaModel;
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