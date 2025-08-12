using System.Diagnostics.CodeAnalysis;
using ChatCompletion;
using ChatCompletion.Config;
using Knowledge.Contracts;
using Knowledge.Contracts.Types;
using KnowledgeEngine.Persistence.Conversations;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace KnowledgeEngine.Chat;

/// <summary>
/// SQLite-based chat service that replaces MongoDB dependency
/// Provides persistent conversation history using local database
/// </summary>
public sealed class SqliteChatService : IChatService
{
    private readonly ChatComplete _chat;
    private readonly IConversationRepository _convos;
    private readonly int _maxTurns;

    public SqliteChatService(ChatComplete chat, IConversationRepository convos, IOptions<ChatCompleteSettings> cfg)
    {
        _chat = chat;
        _convos = convos;
        _maxTurns = Math.Max(2, cfg.Value.ChatMaxTurns);   // never < 2
    }

    [Experimental("SKEXP0001")]
    public async Task<string> GetReplyAsync(ChatRequestDto dto, AiProvider provider, CancellationToken ct)
    {
        // 1️⃣ Ensure conversation exists
        if (string.IsNullOrEmpty(dto.ConversationId))
            dto.ConversationId = await _convos.CreateAsync(dto.KnowledgeId, ct);

        // 2️⃣ Persist *this* user turn immediately
        var userMsg = new ChatMessage
        {
            Role = "user",
            Content = dto.Message,
            Ts = DateTime.UtcNow
        };
        await _convos.AppendMessageAsync(dto.ConversationId!, userMsg, ct);

        // 3️⃣ Re-load history *without* duplicating the new turn
        var past = await _convos.GetMessagesAsync(dto.ConversationId!, ct);

        // Convert to SK ChatMessageContent list
        var skMessages = past.Select(m => new ChatMessageContent(
                m.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? AuthorRole.Assistant : AuthorRole.User,
                m.Content))
            .ToList();

        // Use SK sliding-window reducer
        var reducer = new ChatHistoryTruncationReducer(
            targetCount: _maxTurns * 2,     // keep e.g. last 24 msgs
            thresholdCount: 2);             // trim only when 2 over

        var reduced = await reducer.ReduceAsync(skMessages, ct);

        // If reducer returned null ⇒ no trimming needed
        var historyForLLM = new ChatHistory(reduced ?? skMessages);
        
        // 4️⃣ Ask the model (ChatComplete adds system msg + the enriched user prompt)
        var replyText = await _chat.AskAsync(
            dto.Message,
            dto.KnowledgeId,
            historyForLLM,
            dto.Temperature,
            provider,
            dto.UseExtendedInstructions,
            dto.OllamaModel,
            ct);

        // 5️⃣ Persist assistant turn
        await _convos.AppendMessageAsync(dto.ConversationId!, new ChatMessage
        {
            Role = "assistant",
            Content = replyText,
            Ts = DateTime.UtcNow
        }, ct);

        return replyText;
    }
}