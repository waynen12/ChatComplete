// Tests/ChatHistoryReducerTests.cs
using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Xunit;


namespace KnowledgeEngine.Tests;

public class ChatHistoryReducerTests
{
    // Arrange ­– same numbers you wired in MongoChatService
    private const int TargetTurns   = 10;                // ChatMaxTurns (user+assistant pair count)
    private const int TargetMsgs    = TargetTurns * 2;   // 20
    private const int ThresholdMsgs = 2;                 // start trimming after 22

    private static IEnumerable<ChatMessageContent> CreateChat(int messageCount)
    {
        for (int messageIter = 0; messageIter < messageCount; messageIter++)
        {
            yield return new ChatMessageContent(
                messageIter % 2 == 0 ? AuthorRole.User : AuthorRole.Assistant,
                $"Message {messageIter}");
        }
    }

    [Fact]
    [Experimental("SKEXP0001")]
    public async Task Reducer_trims_history_when_above_threshold()
    {
        // 30 messages ⇒ > Target+Threshold ⇒ should trim
        var allMsgs = CreateChat(30).ToList();
        var reducer = new ChatHistoryTruncationReducer(TargetMsgs, ThresholdMsgs);

        // Act
        var reduced = await reducer.ReduceAsync(allMsgs);

        // Assert
        Assert.NotNull(reduced);                               // trimming happened
        Assert.True(reduced!.Count() <= TargetMsgs);           // never exceeds 20
        Assert.Equal(
            allMsgs.Skip(allMsgs.Count - TargetMsgs).First().Content,
            reduced.First().Content);                          // kept the newest 20
    }

    [Fact]
    [Experimental("SKEXP0001")]
    public async Task Reducer_returns_null_when_below_threshold()
    {
        // 21 messages ⇒ ≤ Target+Threshold ⇒ no trimming
        var msgs = CreateChat(21).ToList();
        var reducer = new ChatHistoryTruncationReducer(TargetMsgs, ThresholdMsgs);

        var reduced = await reducer.ReduceAsync(msgs);

        Assert.Null(reduced);                                  // untouched
    }
}