namespace Knowledge.Api.Options;

/// <summary>
/// Configuration options for Cross-Origin Resource Sharing (CORS).
/// </summary>
public sealed record CorsOptions
{
    /// <summary>List of allowed origin URLs for CORS requests.</summary>
    public string[] AllowedOrigins  { get; init; } = Array.Empty<string>();
    /// <summary>List of allowed HTTP headers for CORS requests.</summary>
    public string[] AllowedHeaders  { get; init; } = { "Content-Type" };
    /// <summary>Maximum age in hours for CORS preflight cache.</summary>
    public int      MaxAgeHours     { get; init; } = 24;
}