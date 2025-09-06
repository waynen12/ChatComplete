namespace Knowledge.Entities;

/// <summary>
/// Database record for Ollama models
/// </summary>
public class OllamaModelRecord
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public long Size { get; set; }
    public string? Family { get; set; }
    public string? ParameterSize { get; set; }
    public string? QuantizationLevel { get; set; }
    public string? Format { get; set; }
    public string? Template { get; set; }
    public string? Parameters { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public DateTime InstalledAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string Status { get; set; } = "Ready";
    public bool? SupportsTools { get; set; } = null;
}

/// <summary>
/// Database record for download tracking
/// </summary>
public class OllamaDownloadRecord
{
    public string ModelName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public double PercentComplete { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}