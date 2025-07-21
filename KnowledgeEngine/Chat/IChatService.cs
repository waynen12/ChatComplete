using System.Threading;
using System.Threading.Tasks;
using Knowledge.Contracts;
using Knowledge.Contracts.Types;


namespace KnowledgeEngine.Chat;

/// <summary>
/// Generates an assistant reply based on the user’s message and
/// (optionally) a specific knowledge collection.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Returns the assistant’s reply text.
    /// </summary>
    Task<string> GetReplyAsync(
        ChatRequestDto request,
        AiProvider provider,
        CancellationToken cancellationToken = default);
}
