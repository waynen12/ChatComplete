using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ChatComplete.Mcp.Configuration;

/// <summary>
/// OpenTelemetry configuration for ChatComplete MCP Server
/// </summary>
public static class OpenTelemetryConfiguration
{
    public const string ServiceName = "chatcomplete-mcp-server";
    public const string ServiceVersion = "1.0.0";
    
    /// <summary>
    /// Configure OpenTelemetry tracing for MCP server
    /// </summary>
    public static TracerProviderBuilder ConfigureTracing(this TracerProviderBuilder builder, McpServerOptions options)
    {
        return builder
            .SetResourceBuilder(CreateResourceBuilder())
            .AddSource("ChatComplete.Mcp")
            .AddSource("ChatComplete.Mcp.Server") 
            .AddHttpClientInstrumentation(httpOptions =>
            {
                httpOptions.RecordException = true;
                httpOptions.FilterHttpRequestMessage = request => 
                {
                    // Don't trace sensitive endpoints
                    var path = request.RequestUri?.AbsolutePath ?? "";
                    return !path.Contains("auth", StringComparison.OrdinalIgnoreCase);
                };
            })
            .AddConsoleExporter(consoleOptions =>
            {
                consoleOptions.Targets = ConsoleExporterOutputTargets.Console;
            })
            .ConfigureConditionalExporters(options);
    }
    
    /// <summary>
    /// Configure OpenTelemetry metrics for MCP server
    /// </summary>
    public static MeterProviderBuilder ConfigureMetrics(this MeterProviderBuilder builder, McpServerOptions options)
    {
        return builder
            .SetResourceBuilder(CreateResourceBuilder())
            .AddMeter("ChatComplete.Mcp")
            .AddMeter("ChatComplete.Mcp.Server")
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .ConfigureConditionalMetricExporters(options);
    }
    
    /// <summary>
    /// Create resource builder with service information
    /// </summary>
    private static ResourceBuilder CreateResourceBuilder()
    {
        return ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: ServiceName,
                serviceVersion: ServiceVersion,
                serviceInstanceId: Environment.MachineName
            )
            .AddAttributes(new Dictionary<string, object>
            {
                ["service.description"] = "ChatComplete Model Context Protocol Server",
                ["service.environment"] = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "development",
                ["host.name"] = Environment.MachineName,
                ["process.pid"] = Environment.ProcessId,
                ["dotnet.version"] = Environment.Version.ToString()
            });
    }
    
    /// <summary>
    /// Configure conditional trace exporters based on options
    /// </summary>
    private static TracerProviderBuilder ConfigureConditionalExporters(this TracerProviderBuilder builder, McpServerOptions options)
    {
        // OTLP Exporter (for Jaeger, Grafana, etc.)
        if (!string.IsNullOrEmpty(options.OpenTelemetry.OtlpEndpoint))
        {
            builder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(options.OpenTelemetry.OtlpEndpoint);
                if (!string.IsNullOrEmpty(options.OpenTelemetry.OtlpHeaders))
                {
                    otlpOptions.Headers = options.OpenTelemetry.OtlpHeaders;
                }
                otlpOptions.Protocol = OtlpExportProtocol.Grpc;
            });
        }
        
        // Console exporter for development
        if (options.OpenTelemetry.EnableConsoleExporter)
        {
            builder.AddConsoleExporter();
        }
        
        return builder;
    }
    
    /// <summary>
    /// Configure conditional metric exporters based on options
    /// </summary>
    private static MeterProviderBuilder ConfigureConditionalMetricExporters(this MeterProviderBuilder builder, McpServerOptions options)
    {
        // OTLP Exporter for metrics
        if (!string.IsNullOrEmpty(options.OpenTelemetry.OtlpEndpoint))
        {
            builder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(options.OpenTelemetry.OtlpEndpoint);
                if (!string.IsNullOrEmpty(options.OpenTelemetry.OtlpHeaders))
                {
                    otlpOptions.Headers = options.OpenTelemetry.OtlpHeaders;
                }
                otlpOptions.Protocol = OtlpExportProtocol.Grpc;
            });
        }
        
        // Prometheus exporter
        if (options.OpenTelemetry.EnablePrometheusExporter)
        {
            builder.AddPrometheusExporter();
        }
        
        // Console exporter for development
        if (options.OpenTelemetry.EnableConsoleExporter)
        {
            builder.AddConsoleExporter();
        }
        
        return builder;
    }
}

/// <summary>
/// Configuration options for MCP server
/// </summary>
public class McpServerOptions
{
    public OpenTelemetryOptions OpenTelemetry { get; set; } = new();
    public LoggingOptions Logging { get; set; } = new();
    public SecurityOptions Security { get; set; } = new();
}

/// <summary>
/// OpenTelemetry configuration options
/// </summary>
public class OpenTelemetryOptions
{
    /// <summary>
    /// OTLP endpoint for exporting traces and metrics (e.g., Jaeger, Grafana)
    /// </summary>
    public string? OtlpEndpoint { get; set; }
    
    /// <summary>
    /// OTLP headers for authentication (e.g., API keys)
    /// </summary>
    public string? OtlpHeaders { get; set; }
    
    /// <summary>
    /// Enable console exporter for development
    /// </summary>
    public bool EnableConsoleExporter { get; set; } = true;
    
    /// <summary>
    /// Enable Prometheus metrics exporter
    /// </summary>
    public bool EnablePrometheusExporter { get; set; } = false;
    
    /// <summary>
    /// Sample rate for traces (0.0 to 1.0)
    /// </summary>
    public double TraceSampleRate { get; set; } = 1.0;
}

/// <summary>
/// Logging configuration options
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Log level (Debug, Information, Warning, Error)
    /// </summary>
    public string LogLevel { get; set; } = "Information";
    
    /// <summary>
    /// Enable structured logging with JSON format
    /// </summary>
    public bool EnableStructuredLogging { get; set; } = true;
    
    /// <summary>
    /// Log file path (optional)
    /// </summary>
    public string? LogFilePath { get; set; }
}

/// <summary>
/// Security configuration options
/// </summary>
public class SecurityOptions
{
    /// <summary>
    /// Enable API key authentication
    /// </summary>
    public bool EnableApiKeyAuthentication { get; set; } = false;
    
    /// <summary>
    /// Valid API keys for authentication
    /// </summary>
    public List<string> ApiKeys { get; set; } = new();
    
    /// <summary>
    /// Enable request rate limiting
    /// </summary>
    public bool EnableRateLimiting { get; set; } = false;
    
    /// <summary>
    /// Maximum requests per minute
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 100;
}