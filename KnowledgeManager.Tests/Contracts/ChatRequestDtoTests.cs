using Knowledge.Contracts;
using Knowledge.Contracts.Types;
using Xunit;

namespace KnowledgeManager.Tests.Contracts;

public class ChatRequestDtoTests
{
    [Fact]
    public void ChatRequestDto_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var dto = new ChatRequestDto();

        // Assert
        Assert.Equal("", dto.Message);
        Assert.Equal(-1, dto.Temperature);
        Assert.False(dto.StripMarkdown);
        Assert.False(dto.UseExtendedInstructions);
        Assert.Null(dto.ConversationId);
        Assert.Equal("", dto.KnowledgeId); // KnowledgeId defaults to empty string, not null
        Assert.Equal(AiProvider.OpenAi, dto.Provider);
    }

    [Fact]
    public void ChatRequestDto_WithValues_ShouldPreserveProperties()
    {
        // Arrange & Act
        var dto = new ChatRequestDto
        {
            Message = "Test message",
            Temperature = 0.7,
            StripMarkdown = true,
            UseExtendedInstructions = true,
            ConversationId = "conv123",
            KnowledgeId = "kb456",
            Provider = AiProvider.Google
        };

        // Assert
        Assert.Equal("Test message", dto.Message);
        Assert.Equal(0.7, dto.Temperature);
        Assert.True(dto.StripMarkdown);
        Assert.True(dto.UseExtendedInstructions);
        Assert.Equal("conv123", dto.ConversationId);
        Assert.Equal("kb456", dto.KnowledgeId);
        Assert.Equal(AiProvider.Google, dto.Provider);
    }
}