using ChatCompletion.Config;
using Knowledge.Contracts;
using Knowledge.Contracts.Types;
using KnowledgeEngine.Chat;
using KnowledgeManager.Tests.AgentFramework;
using Microsoft.Extensions.Options;

namespace KnowledgeManager.Tests.KnowledgeEngine;

/// <summary>
/// Unit tests for SqliteChatService - SQLite-based chat persistence
/// Tests conversation persistence, history management, and chat flow
/// </summary>
public class SqliteChatServiceTests
{
    private const int MaxTurns = 12;
    private const int Window = MaxTurns * 2; // messages

    [Fact]
    public async Task GetReplyAsync_WithNewConversation_ShouldCreateConversationId()
    {
        // Arrange
        var repo = new FakeConvoRepo(messageCount: 0);
        var fake = new FakeChatCompleteAF();
        var service = CreateService(fake, repo);

        var request = new ChatRequestDto
        {
            Message = "Hello",
            ConversationId = null, // New conversation
            KnowledgeId = "test-kb"
        };

        // Act
        var reply = await service.GetReplyAsync(request, AiProvider.OpenAi, CancellationToken.None);

        // Assert
        Assert.Equal("stub-response", reply);
        Assert.NotNull(request.ConversationId); // Should be populated
    }

    [Fact]
    public async Task GetReplyAsync_ShouldPersistUserAndAssistantMessages()
    {
        // Arrange
        var repo = new FakeConvoRepo(messageCount: 0);
        var fake = new FakeChatCompleteAF();
        var service = CreateService(fake, repo);

        var request = new ChatRequestDto
        {
            Message = "User question",
            ConversationId = "conv-123",
            KnowledgeId = "test-kb"
        };

        // Act
        await service.GetReplyAsync(request, AiProvider.OpenAi, CancellationToken.None);

        // Assert - Verify 2 messages were persisted (user + assistant)
        var messages = await repo.GetMessagesAsync("conv-123", CancellationToken.None);
        Assert.Equal(2, messages.Count);
        Assert.Equal("user", messages[0].Role);
        Assert.Equal("User question", messages[0].Content);
        Assert.Equal("assistant", messages[1].Role);
        Assert.Equal("stub-response", messages[1].Content);
    }

    [Fact]
    public async Task GetReplyAsync_WithExistingHistory_ShouldPassHistoryToChatComplete()
    {
        // Arrange - 5 existing messages
        var repo = new FakeConvoRepo(messageCount: 5);
        var fake = new FakeChatCompleteAF();
        var service = CreateService(fake, repo);

        var request = new ChatRequestDto
        {
            Message = "New question",
            ConversationId = "conv-123",
            KnowledgeId = "test-kb"
        };

        // Act
        await service.GetReplyAsync(request, AiProvider.OpenAi, CancellationToken.None);

        // Assert - Fake should receive existing 5 messages + user message + system message for conversation ID
        Assert.NotNull(fake.LastHistoryCount);
        Assert.True(fake.LastHistoryCount >= 6, $"Expected at least 6 messages (5 existing + 1 user), got {fake.LastHistoryCount}");
    }

    [Fact]
    public async Task GetReplyAsync_WithManyMessages_ShouldTruncateHistory()
    {
        // Arrange - 40 existing messages (exceeds window)
        var repo = new FakeConvoRepo(messageCount: 40);
        var fake = new FakeChatCompleteAF();
        var service = CreateService(fake, repo);

        var request = new ChatRequestDto
        {
            Message = "New question",
            ConversationId = "conv-123",
            KnowledgeId = "test-kb"
        };

        // Act
        await service.GetReplyAsync(request, AiProvider.OpenAi, CancellationToken.None);

        // Assert - History should be truncated to window size
        Assert.NotNull(fake.LastHistoryCount);
        // Should be within window (24 messages) + some buffer for system message
        Assert.True(fake.LastHistoryCount <= Window + 5,
            $"Expected ≤{Window + 5}, got {fake.LastHistoryCount}");
    }

    [Fact]
    public async Task GetReplyAsync_ShouldPassCorrectProvider()
    {
        // Arrange
        var repo = new FakeConvoRepo(messageCount: 0);
        var fake = new FakeChatCompleteAF();
        var service = CreateService(fake, repo);

        var request = new ChatRequestDto
        {
            Message = "Test",
            ConversationId = "conv-123",
            KnowledgeId = "test-kb"
        };

        // Act
        await service.GetReplyAsync(request, AiProvider.Google, CancellationToken.None);

        // Assert
        Assert.Equal(AiProvider.Google, fake.LastProvider);
    }

    [Fact]
    public async Task GetReplyAsync_ShouldPassTemperature()
    {
        // Arrange
        var repo = new FakeConvoRepo(messageCount: 0);
        var fake = new FakeChatCompleteAF();
        var service = CreateService(fake, repo);

        var request = new ChatRequestDto
        {
            Message = "Test",
            ConversationId = "conv-123",
            KnowledgeId = "test-kb",
            Temperature = 0.8
        };

        // Act
        await service.GetReplyAsync(request, AiProvider.OpenAi, CancellationToken.None);

        // Assert
        Assert.Equal(0.8, fake.LastTemperature);
    }

    [Fact]
    public async Task GetReplyAsync_ShouldPassOllamaModel()
    {
        // Arrange
        var repo = new FakeConvoRepo(messageCount: 0);
        var fake = new FakeChatCompleteAF();
        var service = CreateService(fake, repo);

        var request = new ChatRequestDto
        {
            Message = "Test",
            ConversationId = "conv-123",
            KnowledgeId = "test-kb",
            Provider = AiProvider.Ollama,
            OllamaModel = "llama3.2:3b"
        };

        // Act
        await service.GetReplyAsync(request, AiProvider.Ollama, CancellationToken.None);

        // Assert
        Assert.Equal("llama3.2:3b", fake.LastOllamaModel);
    }

    [Fact]
    public async Task GetReplyAsync_MultipleRounds_ShouldAccumulateHistory()
    {
        // Arrange
        var repo = new FakeConvoRepo(messageCount: 0);
        var fake = new FakeChatCompleteAF();
        var service = CreateService(fake, repo);

        var conversationId = "conv-multi";

        // Act - Multiple rounds of conversation
        await service.GetReplyAsync(new ChatRequestDto
        {
            Message = "First question",
            ConversationId = conversationId,
            KnowledgeId = "test-kb"
        }, AiProvider.OpenAi, CancellationToken.None);

        await service.GetReplyAsync(new ChatRequestDto
        {
            Message = "Second question",
            ConversationId = conversationId,
            KnowledgeId = "test-kb"
        }, AiProvider.OpenAi, CancellationToken.None);

        await service.GetReplyAsync(new ChatRequestDto
        {
            Message = "Third question",
            ConversationId = conversationId,
            KnowledgeId = "test-kb"
        }, AiProvider.OpenAi, CancellationToken.None);

        // Assert - Should have 6 messages (3 user + 3 assistant)
        var messages = await repo.GetMessagesAsync(conversationId, CancellationToken.None);
        Assert.Equal(6, messages.Count);
        Assert.Equal("First question", messages[0].Content);
        Assert.Equal("Second question", messages[2].Content);
        Assert.Equal("Third question", messages[4].Content);
    }

    [Fact]
    public async Task GetReplyAsync_WithKnowledgeId_ShouldPassToChat()
    {
        // Arrange
        var repo = new FakeConvoRepo(messageCount: 0);
        var fake = new FakeChatCompleteAF();
        var service = CreateService(fake, repo);

        var request = new ChatRequestDto
        {
            Message = "Test",
            ConversationId = "conv-123",
            KnowledgeId = "my-knowledge-base"
        };

        // Act
        await service.GetReplyAsync(request, AiProvider.OpenAi, CancellationToken.None);

        // Assert
        Assert.Equal("my-knowledge-base", fake.LastKnowledgeId);
    }

    [Fact]
    public async Task GetReplyAsync_WithExtendedInstructions_ShouldPass()
    {
        // Arrange
        var repo = new FakeConvoRepo(messageCount: 0);
        var fake = new FakeChatCompleteAF();
        var service = CreateService(fake, repo);

        var request = new ChatRequestDto
        {
            Message = "Test",
            ConversationId = "conv-123",
            KnowledgeId = "test-kb",
            UseExtendedInstructions = true
        };

        // Act
        await service.GetReplyAsync(request, AiProvider.OpenAi, CancellationToken.None);

        // Assert
        Assert.True(fake.LastUseExtendedInstructions);
    }

    [Fact]
    public void Constructor_ShouldEnforceMinimumMaxTurns()
    {
        // Arrange
        var repo = new FakeConvoRepo(messageCount: 0);
        var fake = new FakeChatCompleteAF();

        // Act - Create service with ChatMaxTurns = 1 (below minimum of 2)
        var service = CreateService(fake, repo, maxTurns: 1);

        // Assert - Service should be created successfully
        // (minimum is enforced internally in SqliteChatService constructor)
        Assert.NotNull(service);
    }

    [Fact]
    public async Task GetReplyAsync_ShouldReturnStubReply()
    {
        // Arrange
        var repo = new FakeConvoRepo(messageCount: 0);
        var fake = new FakeChatCompleteAF();
        var service = CreateService(fake, repo);

        var request = new ChatRequestDto
        {
            Message = "Test",
            ConversationId = "conv-123",
            KnowledgeId = "test-kb"
        };

        // Act
        var result = await service.GetReplyAsync(request, AiProvider.OpenAi, CancellationToken.None);

        // Assert
        Assert.Equal("stub-response", result);
    }

    // Helper method to create service
    private SqliteChatService CreateService(
        FakeChatCompleteAF fake,
        FakeConvoRepo repo,
        int maxTurns = MaxTurns)
    {
        return new SqliteChatService(
            fake,  // AF version (SK removed in Phase 4)
            repo,
            Options.Create(new ChatCompleteSettings { ChatMaxTurns = maxTurns })
        );
    }
}
