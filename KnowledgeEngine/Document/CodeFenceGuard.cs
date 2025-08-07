using System;
using System.Text;
using ChatCompletion.Config;
using KnowledgeEngine.Logging;

namespace KnowledgeEngine.Document
{
    /// <summary>
    /// Guards against oversized code fences that can cause performance issues
    /// or crashes when sent to AI providers
    /// </summary>
    public static class CodeFenceGuard
    {
        private const string TruncationMessage = "\n\n// ... [CODE TRUNCATED - CONTENT TOO LARGE] ...";
        private const int DefaultMaxSize = 10240; // 10KB default
        
        /// <summary>
        /// Guards a code fence by truncating if it exceeds the configured size limit
        /// </summary>
        /// <param name="code">The code content to guard</param>
        /// <param name="language">The programming language (for logging)</param>
        /// <param name="settings">Configuration settings with size limits</param>
        /// <returns>Original code or truncated version with warning message</returns>
        public static string GuardCodeFence(string code, string language, ChatCompleteSettings? settings = null)
        {
            if (string.IsNullOrEmpty(code))
                return code;

            var maxSize = settings?.MaxCodeFenceSize ?? DefaultMaxSize;
            var shouldTruncate = settings?.TruncateOversizedCodeFences ?? true;

            // Check if code is within acceptable size
            var codeSize = Encoding.UTF8.GetByteCount(code);
            if (codeSize <= maxSize)
            {
                return code; // No truncation needed
            }

            // Log the oversized code fence
            LoggerProvider.Logger.Warning(
                "Oversized code fence detected: {Language} code block is {SizeKB}KB (max: {MaxSizeKB}KB). {Action}",
                string.IsNullOrEmpty(language) ? "unknown" : language,
                Math.Round(codeSize / 1024.0, 1),
                Math.Round(maxSize / 1024.0, 1),
                shouldTruncate ? "Truncating" : "Keeping original"
            );

            if (!shouldTruncate)
            {
                return code; // Keep original if truncation disabled
            }

            // Calculate how much content we can keep (reserve space for truncation message)
            var truncationMessageSize = Encoding.UTF8.GetByteCount(TruncationMessage);
            var availableSize = Math.Max(0, maxSize - truncationMessageSize);

            if (availableSize == 0)
            {
                return TruncationMessage.Trim();
            }

            // Truncate at character boundaries, not byte boundaries for better UX
            var truncatedCode = TruncateAtCharacterBoundary(code, availableSize);
            
            return truncatedCode + TruncationMessage;
        }

        /// <summary>
        /// Truncates text at character boundaries to avoid cutting UTF-8 sequences
        /// </summary>
        private static string TruncateAtCharacterBoundary(string text, int maxBytes)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Start with a reasonable estimate
            var estimatedLength = Math.Min(text.Length, maxBytes);
            
            // Adjust downward until we're within the byte limit
            while (estimatedLength > 0)
            {
                var candidate = text.Substring(0, estimatedLength);
                if (Encoding.UTF8.GetByteCount(candidate) <= maxBytes)
                {
                    return candidate;
                }
                estimatedLength--;
            }

            return string.Empty;
        }

        /// <summary>
        /// Checks if a code fence would be truncated without actually truncating it
        /// </summary>
        public static bool WouldTruncate(string code, ChatCompleteSettings? settings = null)
        {
            if (string.IsNullOrEmpty(code))
                return false;

            var maxSize = settings?.MaxCodeFenceSize ?? DefaultMaxSize;
            var shouldTruncate = settings?.TruncateOversizedCodeFences ?? true;

            return shouldTruncate && Encoding.UTF8.GetByteCount(code) > maxSize;
        }

        /// <summary>
        /// Gets statistics about code fence size and truncation
        /// </summary>
        public static CodeFenceStats GetStats(string code, ChatCompleteSettings? settings = null)
        {
            var originalSize = string.IsNullOrEmpty(code) ? 0 : Encoding.UTF8.GetByteCount(code);
            var maxSize = settings?.MaxCodeFenceSize ?? DefaultMaxSize;
            var wouldTruncate = WouldTruncate(code, settings);
            
            var guardedCode = GuardCodeFence(code, "unknown", settings);
            var finalSize = string.IsNullOrEmpty(guardedCode) ? 0 : Encoding.UTF8.GetByteCount(guardedCode);

            return new CodeFenceStats
            {
                OriginalSizeBytes = originalSize,
                FinalSizeBytes = finalSize,
                MaxAllowedSizeBytes = maxSize,
                WasTruncated = wouldTruncate,
                CompressionRatio = originalSize > 0 ? (double)finalSize / originalSize : 1.0
            };
        }
    }

    /// <summary>
    /// Statistics about code fence processing
    /// </summary>
    public class CodeFenceStats
    {
        public int OriginalSizeBytes { get; set; }
        public int FinalSizeBytes { get; set; }
        public int MaxAllowedSizeBytes { get; set; }
        public bool WasTruncated { get; set; }
        public double CompressionRatio { get; set; }

        public double OriginalSizeKB => Math.Round(OriginalSizeBytes / 1024.0, 1);
        public double FinalSizeKB => Math.Round(FinalSizeBytes / 1024.0, 1);
        public double MaxAllowedSizeKB => Math.Round(MaxAllowedSizeBytes / 1024.0, 1);
    }
}