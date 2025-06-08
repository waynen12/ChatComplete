using Serilog;
using ChatCompletion.Config;

namespace KnowledgeEngine.Logging;
public static class LoggerProvider
{
    private static ILogger? _logger;

    public static void ConfigureLogger()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var logFilePath = $"Logs/log-{timestamp}-.txt";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Hour,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 10,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(2)
            )
            .CreateLogger();

        _logger = Log.Logger;
    }

    public static ILogger Logger
    {
        get
        {
            if (_logger == null)
            {
                ConfigureLogger();
            }

            return _logger!;
        }
    }
}