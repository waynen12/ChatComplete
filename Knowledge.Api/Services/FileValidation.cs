// Knowledge.Api/Services/FileValidation.cs
using KnowledgeEngine.Logging;
using ILogger = Serilog.ILogger;

namespace Knowledge.Api.Services;

/// <summary>
/// Provides file upload validation services for the Knowledge API.
/// </summary>
public static class FileValidation
{
    /// <summary>Allowed extensions (lower-case, no dot).</summary>
    private static readonly HashSet<string> _ext =
        new(StringComparer.OrdinalIgnoreCase) { "pdf", "docx", "md", "txt" };

    /// <summary>Maximum upload size per file (bytes).</summary>
    public const long MaxBytes = 100 * 1024 * 1024;   // 25 MB

    /// <summary>
    /// Validates an uploaded file.  
    /// Returns <c>null</c> when the file is OK; otherwise an error message.
    /// </summary>
    public static string? Check(IFormFile file)
    {
        string? result = null;
        if (file != null)
        {
            if (file.Length == 0) result = $"“{file.FileName}” is empty.";

            if (file.Length > MaxBytes) result = $"“{file.FileName}” exceeds {MaxBytes / 1024 / 1024} MB.";

            var ext = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
            if (!_ext.Contains(ext)) result = $"“{file.FileName}” has unsupported extension .{ext}.";
        }
        else
        {
            result = "File is null.";
        }

        if (!string.IsNullOrEmpty(result))
        {
            LoggerProvider.Logger.Error(result);
        }
        return result; 
    }

    /// <summary>Async helper if you need to open/read the file later.</summary>
    public static ValueTask<string?> CheckAsync(IFormFile file) =>
        ValueTask.FromResult(Check(file));
}