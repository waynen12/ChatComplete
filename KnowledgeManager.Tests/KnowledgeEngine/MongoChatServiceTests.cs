using ChatCompletion.Config;
using Knowledge.Contracts;
using Knowledge.Contracts.Types;
using KnowledgeEngine.Chat;
using KnowledgeManager.Tests.AgentFramework;
using Microsoft.Extensions.Options;

namespace KnowledgeManager.Tests.KnowledgeEngine;

public class MongoChatServiceTests
{
    private const int MaxTurns = 10;           // same value you put in settings
    private const int Window   = MaxTurns * 2; // msgs

    [Fact]
    public async Task MongoChatService_sends_bounded_history_to_ChatComplete()
    {
        // Arrange: 35 existing messages (> window)
        var repo = new FakeConvoRepo(messageCount: 35);
        var fake = new FakeChatCompleteAF();

        var svc  = new MongoChatService(
            fake,  // AF version (SK removed in Phase 4)
            repo,
            Options.Create(new ChatCompleteSettings { ChatMaxTurns = MaxTurns }));

        var dto = new ChatRequestDto
        {
            ConversationId = "cid-123",
            Message        = "new-question"
        };

        // Act
        await svc.GetReplyAsync(dto, AiProvider.OpenAi, CancellationToken.None);

        // Assert – Fake recorded the actual length handed to ChatComplete
        Assert.NotNull(fake.LastHistoryCount);
        Assert.True(fake.LastHistoryCount <= Window, $"Expected ≤{Window}, got {fake.LastHistoryCount}");
    }
}
