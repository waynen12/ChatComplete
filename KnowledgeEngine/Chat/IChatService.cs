using System.Threading;
using System.Threading.Tasks;
using Knowledge.Contracts;   // ChatRequestDto

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
        CancellationToken cancellationToken = default);
}
