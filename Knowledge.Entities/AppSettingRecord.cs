namespace Knowledge.Entities;

/// <summary>
/// Database record for application settings
/// </summary>
public class AppSettingRecord
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Value { get; set; }
    public byte[]? EncryptedValue { get; set; }
    public bool IsEncrypted { get; set; }
    public string Category { get; set; } = "General";
    public string DataType { get; set; } = "String";
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}