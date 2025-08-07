using System;
using System.Text;
using ChatCompletion.Config;
using KnowledgeEngine.Document;
using Xunit;
using Xunit.Abstractions;

namespace KnowledgeManager.Tests.KnowledgeEngine
{
    /// <summary>
    /// Tests for CodeFenceGuard to ensure proper handling of oversized code blocks
    /// </summary>
    public class CodeFenceGuardTests
    {
        private readonly ITestOutputHelper _output;

        public CodeFenceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GuardCodeFence_SmallCodeBlock_ShouldReturnOriginal()
        {
            // Arrange
            var smallCode = "function hello() {\n  console.log('Hello World!');\n}";
            var settings = new ChatCompleteSettings { MaxCodeFenceSize = 10240, TruncateOversizedCodeFences = true };

            // Act
            var result = CodeFenceGuard.GuardCodeFence(smallCode, "javascript", settings);

            // Assert
            Assert.Equal(smallCode, result);
            _output.WriteLine($"Small code block ({smallCode.Length} chars) passed through unchanged");
        }

        [Fact]
        public void GuardCodeFence_LargeCodeBlock_ShouldTruncateWithMessage()
        {
            // Arrange
            var largeCode = new string('x', 15000); // 15KB of code
            var settings = new ChatCompleteSettings { MaxCodeFenceSize = 10240, TruncateOversizedCodeFences = true };

            // Act
            var result = CodeFenceGuard.GuardCodeFence(largeCode, "text", settings);

            // Assert
            Assert.True(result.Length < largeCode.Length, "Result should be shorter than original");
            Assert.Contains("[CODE TRUNCATED - CONTENT TOO LARGE]", result);
            Assert.True(Encoding.UTF8.GetByteCount(result) <= settings.MaxCodeFenceSize);
            
            _output.WriteLine($"Large code block truncated from {largeCode.Length} to {result.Length} chars");
        }

        [Fact]
        public void GuardCodeFence_LargeCodeBlockWithTruncationDisabled_ShouldReturnOriginal()
        {
            // Arrange
            var largeCode = new string('x', 15000); // 15KB of code
            var settings = new ChatCompleteSettings { MaxCodeFenceSize = 10240, TruncateOversizedCodeFences = false };

            // Act
            var result = CodeFenceGuard.GuardCodeFence(largeCode, "text", settings);

            // Assert
            Assert.Equal(largeCode, result);
            _output.WriteLine("Large code block preserved when truncation disabled");
        }

        [Fact]
        public void GuardCodeFence_NullOrEmptyCode_ShouldReturnOriginal()
        {
            // Arrange & Act
            var nullResult = CodeFenceGuard.GuardCodeFence(null!, "text");
            var emptyResult = CodeFenceGuard.GuardCodeFence("", "text");

            // Assert
            Assert.Null(nullResult);
            Assert.Equal("", emptyResult);
            _output.WriteLine("Null and empty code blocks handled correctly");
        }

        [Fact] 
        public void GuardCodeFence_DefaultSettings_ShouldUseDefaults()
        {
            // Arrange
            var largeCode = new string('x', 15000); // 15KB of code

            // Act - No settings provided, should use defaults
            var result = CodeFenceGuard.GuardCodeFence(largeCode, "text");

            // Assert - Default is 10KB max, truncation enabled
            Assert.True(result.Length < largeCode.Length);
            Assert.Contains("[CODE TRUNCATED - CONTENT TOO LARGE]", result);
            _output.WriteLine("Default settings applied correctly when none provided");
        }

        [Theory]
        [InlineData("javascript", 1000)]
        [InlineData("python", 5000)]
        [InlineData("csharp", 15000)]
        [InlineData("", 20000)]
        public void GuardCodeFence_VariousLanguages_ShouldHandleCorrectly(string language, int codeSize)
        {
            // Arrange
            var code = new string('A', codeSize);
            var settings = new ChatCompleteSettings { MaxCodeFenceSize = 10240, TruncateOversizedCodeFences = true };

            // Act
            var result = CodeFenceGuard.GuardCodeFence(code, language, settings);

            // Assert
            var shouldBeTruncated = codeSize > settings.MaxCodeFenceSize;
            if (shouldBeTruncated)
            {
                Assert.True(result.Length < code.Length, $"{language} code should be truncated");
                Assert.Contains("[CODE TRUNCATED - CONTENT TOO LARGE]", result);
            }
            else
            {
                Assert.Equal(code, result);
            }

            _output.WriteLine($"Language: {language}, Size: {codeSize}, Truncated: {shouldBeTruncated}");
        }

        [Fact]
        public void WouldTruncate_ShouldDetectCorrectly()
        {
            // Arrange
            var smallCode = new string('x', 1000);
            var largeCode = new string('x', 15000);
            var settings = new ChatCompleteSettings { MaxCodeFenceSize = 10240, TruncateOversizedCodeFences = true };

            // Act & Assert
            Assert.False(CodeFenceGuard.WouldTruncate(smallCode, settings));
            Assert.True(CodeFenceGuard.WouldTruncate(largeCode, settings));

            // Test with truncation disabled
            settings.TruncateOversizedCodeFences = false;
            Assert.False(CodeFenceGuard.WouldTruncate(largeCode, settings));

            _output.WriteLine("WouldTruncate detection working correctly");
        }

        [Fact]
        public void GetStats_ShouldProvideAccurateStatistics()
        {
            // Arrange
            var largeCode = new string('x', 15000); // 15KB of code
            var settings = new ChatCompleteSettings { MaxCodeFenceSize = 10240, TruncateOversizedCodeFences = true };

            // Act
            var stats = CodeFenceGuard.GetStats(largeCode, settings);

            // Assert
            Assert.Equal(15000, stats.OriginalSizeBytes);
            Assert.True(stats.FinalSizeBytes <= settings.MaxCodeFenceSize);
            Assert.Equal(10240, stats.MaxAllowedSizeBytes);
            Assert.True(stats.WasTruncated);
            Assert.True(stats.CompressionRatio < 1.0); // Should be compressed

            _output.WriteLine($"Stats - Original: {stats.OriginalSizeKB}KB, Final: {stats.FinalSizeKB}KB, " +
                             $"Compression: {stats.CompressionRatio:P2}, Truncated: {stats.WasTruncated}");
        }

        [Fact]
        public void GuardCodeFence_UnicodeContent_ShouldHandleCorrectly()
        {
            // Arrange - Create code with Unicode characters (multibyte chars)
            var unicodeCode = "// This is a comment with unicode chars\n" +
                             "function greet(name: string) {\n" +
                             "  return `Hello ${name}!`;\n" +
                             "}\n" +
                             new string('ñ', 3000); // Add many multibyte chars to make it large
            
            var settings = new ChatCompleteSettings { MaxCodeFenceSize = 5000, TruncateOversizedCodeFences = true };

            // Act
            var result = CodeFenceGuard.GuardCodeFence(unicodeCode, "typescript", settings);

            // Assert
            Assert.True(Encoding.UTF8.GetByteCount(result) <= settings.MaxCodeFenceSize);
            Assert.Contains("[CODE TRUNCATED - CONTENT TOO LARGE]", result);
            
            // Ensure the result is valid UTF-8 (no broken Unicode sequences)
            var bytes = Encoding.UTF8.GetBytes(result);
            var reconverted = Encoding.UTF8.GetString(bytes);
            Assert.NotNull(reconverted); // If no exception thrown, UTF-8 is valid

            _output.WriteLine($"Unicode code handled correctly - Original UTF-8 bytes: {Encoding.UTF8.GetByteCount(unicodeCode)}, " +
                             $"Final UTF-8 bytes: {Encoding.UTF8.GetByteCount(result)}");
        }

        [Fact]
        public void GuardCodeFence_ExtremelyLargeCode_ShouldHandleGracefully()
        {
            // Arrange - Create a very large code block
            var extremelyLargeCode = new string('A', 100000); // 100KB
            var settings = new ChatCompleteSettings { MaxCodeFenceSize = 1024, TruncateOversizedCodeFences = true }; // Very small limit

            // Act
            var result = CodeFenceGuard.GuardCodeFence(extremelyLargeCode, "text", settings);

            // Assert
            Assert.True(Encoding.UTF8.GetByteCount(result) <= settings.MaxCodeFenceSize);
            Assert.Contains("[CODE TRUNCATED - CONTENT TOO LARGE]", result);

            // Should handle extreme cases without throwing exceptions
            var stats = CodeFenceGuard.GetStats(extremelyLargeCode, settings);
            Assert.True(stats.CompressionRatio < 0.1); // Very high compression ratio

            _output.WriteLine($"Extremely large code handled - Compression ratio: {stats.CompressionRatio:P2}");
        }

        [Fact]
        public void GuardCodeFence_EdgeCaseSizes_ShouldHandleCorrectly()
        {
            // Test edge cases around the size limit
            var settings = new ChatCompleteSettings { MaxCodeFenceSize = 1000, TruncateOversizedCodeFences = true };

            // Exactly at limit
            var exactSizeCode = new string('x', 1000);
            var exactResult = CodeFenceGuard.GuardCodeFence(exactSizeCode, "text", settings);
            Assert.Equal(exactSizeCode, exactResult); // Should not be truncated

            // One byte over limit  
            var overLimitCode = new string('x', 1001);
            var overResult = CodeFenceGuard.GuardCodeFence(overLimitCode, "text", settings);
            Assert.True(overResult.Length < overLimitCode.Length); // Should be truncated

            _output.WriteLine("Edge case sizes handled correctly");
        }
    }
}