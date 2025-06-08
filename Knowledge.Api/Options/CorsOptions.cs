namespace Knowledge.Api.Options;

public sealed record CorsOptions
{
    public string[] AllowedOrigins  { get; init; } = Array.Empty<string>();
    public string[] AllowedHeaders  { get; init; } = { "Content-Type" };
    public int      MaxAgeHours     { get; init; } = 24;
}