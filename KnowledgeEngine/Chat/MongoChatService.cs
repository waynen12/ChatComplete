using ChatCompletion;
using ChatCompletion.Config;
using Knowledge.Contracts;
using Knowledge.Contracts.Types;
using KnowledgeEngine.Persistence.Conversations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace KnowledgeEngine.Chat;

/// <summary>
/// MongoDB-based chat service for persistent conversation history
/// Uses Agent Framework (Microsoft.Agents.AI) for all chat completions
/// </summary>
public sealed class MongoChatService : IChatService
{
    private readonly ChatCompleteAF _chatAF;
    private readonly IConversationRepository _convos;
    private readonly ChatCompleteSettings _settings;
    private readonly int _maxTurns;

    public MongoChatService(
        ChatCompleteAF chatAF,
        IConversationRepository convos,
        IOptions<ChatCompleteSettings> cfg
    )
    {
        _chatAF = chatAF;
        _convos = convos;
        _settings = cfg.Value;
        _maxTurns = Math.Max(2, _settings.ChatMaxTurns); // never < 2
    }

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

        // Convert to AF ChatMessage list (using fully qualified name to avoid ambiguity)
        var afMessages = past
            .Select(m => new Microsoft.Extensions.AI.ChatMessage(
                m.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? ChatRole.Assistant : ChatRole.User,
                m.Content
            ))
            .ToList();

        // Apply sliding-window truncation (keep last N messages)
        var maxMessages = _maxTurns * 2; // e.g., last 24 messages (12 user + 12 assistant)
        var pastCount = past.Count();
        if (afMessages.Count > maxMessages + 2) // +2 threshold before trimming
        {
            // Keep only the most recent messages
            afMessages = afMessages.Skip(afMessages.Count - maxMessages).ToList();
            Console.WriteLine($"📝 [Chat] Truncated history to {afMessages.Count} messages (from {pastCount})");
        }

        // 4️⃣ Ask the model using Agent Framework
        Console.WriteLine("🔄 [API] Using ChatCompleteAF (Agent Framework)");

        string replyText;
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
