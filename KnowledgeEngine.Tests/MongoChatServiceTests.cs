using System.Diagnostics.CodeAnalysis;
using ChatCompletion.Config;
using Knowledge.Contracts;
using Knowledge.Contracts.Types;
using KnowledgeEngine.Chat;
using Microsoft.Extensions.Options;

public class MongoChatServiceTests
{
    private const int MaxTurns = 10;           // same value you put in settings
    private const int Window   = MaxTurns * 2; // msgs

    [Fact]
    [Experimental("SKEXP0001")]
    public async Task MongoChatService_sends_bounded_history_to_ChatComplete()
    {
        // Arrange: 35 existing messages (> window)
        var repo = new FakeConvoRepo(messageCount: 35);
        var spy  = new SpyChatComplete();

        var svc  = new MongoChatService(
            spy,
            repo,
            Options.Create(new ChatCompleteSettings { ChatMaxTurns = MaxTurns }));

        var dto = new ChatRequestDto
        {
            ConversationId = "cid-123",
            Message        = "new-question"
        };

        // Act
        await svc.GetReplyAsync(dto, AiProvider.OpenAi, CancellationToken.None);

        // Assert – Spy recorded the actual length handed to ChatComplete
        Assert.NotNull(spy.LastHistoryCount);
        Assert.True(spy.LastHistoryCount <= Window, $"Expected ≤{Window}, got {spy.LastHistoryCount}");
    }
}