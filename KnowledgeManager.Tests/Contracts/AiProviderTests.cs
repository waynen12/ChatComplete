using Knowledge.Contracts.Types;
using Xunit;

namespace KnowledgeManager.Tests.Contracts;

public class AiProviderTests
{
    [Fact]
    public void AiProvider_ShouldHaveAllExpectedValues()
    {
        // Arrange & Assert - Ensure all expected providers exist
        var providers = Enum.GetValues<AiProvider>();
        
        Assert.Contains(AiProvider.OpenAi, providers);
        Assert.Contains(AiProvider.Google, providers);
        Assert.Contains(AiProvider.Anthropic, providers);
        Assert.Contains(AiProvider.Ollama, providers);
    }

    [Theory]
    [InlineData(AiProvider.OpenAi)]
    [InlineData(AiProvider.Google)]
    [InlineData(AiProvider.Anthropic)]
    [InlineData(AiProvider.Ollama)]
    public void AiProvider_ToString_ShouldReturnExpectedValues(AiProvider provider)
    {
        // Act
        var result = provider.ToString();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void AiProvider_DefaultValue_ShouldBeOpenAi()
    {
        // Arrange
        var defaultProvider = default(AiProvider);

        // Assert
        Assert.Equal(AiProvider.OpenAi, defaultProvider);
    }
}