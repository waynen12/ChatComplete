using System.Threading;
using System.Threading.Tasks;
using ChatCompletion;
using Knowledge.Contracts;
using KnowledgeEngine.Persistence;
using Microsoft.Extensions.Logging;

namespace KnowledgeEngine.Chat;

public sealed class MongoChatService : IChatService
{
    private readonly ChatComplete _core;
    private readonly IKnowledgeRepository _repo;
    private readonly ILogger<MongoChatService> _log;

    public MongoChatService(
        ChatComplete core,
        IKnowledgeRepository repo,
        ILogger<MongoChatService> log
    )
    {
        _core = core;
        _repo = repo;
        _log = log;
    }

    public async Task<string> GetReplyAsync(ChatRequestDto request, CancellationToken ct = default)
    {
        // Guard: if KnowledgeId supplied, ensure it exists
        if (
            !string.IsNullOrEmpty(request.KnowledgeId)
            && !await _repo.ExistsAsync(request.KnowledgeId, ct)
        )
        {
            _log.LogWarning("KnowledgeId {Id} not found", request.KnowledgeId);
            throw new KeyNotFoundException($"Knowledge '{request.KnowledgeId}' not found.");
        }

        // Delegate to ChatComplete's AskAsync
        return await _core.AskAsync(
            request.Message,
            request.KnowledgeId,
            request.Temperature,
            request.UseExtendedInstructions,
            ct
        );
    }
}
