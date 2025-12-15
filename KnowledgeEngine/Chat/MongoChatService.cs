using System.Diagnostics.CodeAnalysis;
using System.Text;
using ChatCompletion;
using ChatCompletion.Config;
using Knowledge.Contracts;
using Knowledge.Contracts.Types;
using KnowledgeEngine.Agents.Models;
using KnowledgeEngine.Persistence.Conversations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace KnowledgeEngine.Chat;

/// <summary>
/// MongoDB-based chat service for persistent conversation history
/// Routes to either Semantic Kernel or Agent Framework based on UseAgentFramework flag
/// </summary>
public sealed class MongoChatService : IChatService
{
    private readonly ChatComplete _chatSK;
    private readonly ChatCompleteAF _chatAF;
    private readonly IConversationRepository _convos;
    private readonly ChatCompleteSettings _settings;
    private readonly int _maxTurns;

    public MongoChatService(
        ChatComplete chatSK,
        ChatCompleteAF chatAF,
        IConversationRepository convos,
        IOptions<ChatCompleteSettings> cfg
    )
    {
        _chatSK = chatSK;
        _chatAF = chatAF;
        _convos = convos;
        _settings = cfg.Value;
        _maxTurns = Math.Max(2, _settings.ChatMaxTurns); // never < 2
    }

    [Experimental("SKEXP0001")]
    public async Task<string> GetReplyAsync(ChatRequestDto dto, AiProvider provider, CancellationToken ct)
    {
        // 1️⃣  Ensure conversation exists
        if (string.IsNullOrEmpty(dto.ConversationId))
            dto.ConversationId = await _convos.CreateAsync(dto.KnowledgeId, ct);

        // 2️⃣  Persist *this* user turn immediately
        var userMsg = new Persistence.Conversations.ChatMessage
        {
            Role    = "user",
            Content = dto.Message,
            Ts      = DateTime.UtcNow
        };
        await _convos.AppendMessageAsync(dto.ConversationId!, userMsg, ct);

        // 3️⃣  Re-load history *without* duplicating the new turn
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

        // 4️⃣ Ask the model - route based on UseAgentFramework feature flag
        string replyText;
        if (_settings.UseAgentFramework)
        {
            // Use Agent Framework version
            Console.WriteLine("🔄 [API] Routing to ChatCompleteAF (Agent Framework)");

            // Convert SK ChatHistory to AF List<ChatMessage>
            var afMessages = historyForLLM
                .Select(m => new Microsoft.Extensions.AI.ChatMessage(
                    m.Role == AuthorRole.Assistant ? ChatRole.Assistant : ChatRole.User,
                    m.Content
                ))
                .ToList();

            if (dto.UseAgent)
            {
                var agentResponse = await _chatAF.AskWithAgentAsync(
                    dto.Message,
                    dto.KnowledgeId,
                    afMessages,
                    dto.Temperature,
                    provider,
                    dto.UseExtendedInstructions,
                    enableAgentTools: true,
                    dto.OllamaModel,
                    ct
                );
                replyText = agentResponse.Response;
            }
            else
            {
                replyText = await _chatAF.AskAsync(
                    dto.Message,
                    dto.KnowledgeId,
                    afMessages,
                    dto.Temperature,
                    provider,
                    dto.UseExtendedInstructions,
                    dto.OllamaModel,
                    ct
                );
            }
        }
        else
        {
            // Use Semantic Kernel version (existing code)
            Console.WriteLine("🔄 [API] Routing to ChatComplete (Semantic Kernel)");

            if (dto.UseAgent)
            {
                var agentResponse = await _chatSK.AskWithAgentAsync(
                    dto.Message,
                    dto.KnowledgeId,
                    historyForLLM,
                    dto.Temperature,
                    provider,
                    dto.UseExtendedInstructions,
                    enableAgentTools: true,
                    dto.OllamaModel,
                    ct
                );
                replyText = agentResponse.Response;
            }
            else
            {
                replyText = await _chatSK.AskAsync(
                    dto.Message,
                    dto.KnowledgeId,
                    historyForLLM,
                    dto.Temperature,
                    provider,
                    dto.UseExtendedInstructions,
                    dto.OllamaModel,
                    ct
                );
            }
        }

        // 5️⃣  Persist assistant turn
        await _convos.AppendMessageAsync(dto.ConversationId!, new Persistence.Conversations.ChatMessage
        {
            Role    = "assistant",
            Content = replyText,
            Ts      = DateTime.UtcNow
        }, ct);

        return replyText;
    }
}
