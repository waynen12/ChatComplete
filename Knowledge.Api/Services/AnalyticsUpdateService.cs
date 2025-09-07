using Knowledge.Analytics.Services;
using Knowledge.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Knowledge.Api.Services;

public class AnalyticsUpdateOptions
{
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableRealTimeUpdates { get; set; } = true;
    public TimeSpan OpenAIBalanceUpdateInterval { get; set; } = TimeSpan.FromMinutes(2);
}

/// <summary>
/// Background service that pushes real-time analytics updates via SignalR
/// </summary>
public class AnalyticsUpdateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<AnalyticsHub> _hubContext;
    private readonly ILogger<AnalyticsUpdateService> _logger;
    private readonly AnalyticsUpdateOptions _options;

    public AnalyticsUpdateService(
        IServiceProvider serviceProvider,
        IHubContext<AnalyticsHub> hubContext,
        ILogger<AnalyticsUpdateService> logger,
        IOptions<AnalyticsUpdateOptions> options)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableRealTimeUpdates)
        {
            _logger.LogInformation("Real-time analytics updates are disabled");
            return;
        }

        _logger.LogInformation("Analytics update service started with interval: {Interval}", _options.UpdateInterval);

        var openAITimer = new PeriodicTimer(_options.OpenAIBalanceUpdateInterval);
        var generalTimer = new PeriodicTimer(_options.UpdateInterval);

        // Start both timers concurrently
        var openAITask = HandleOpenAIUpdates(openAITimer, stoppingToken);
        var generalTask = HandleGeneralUpdates(generalTimer, stoppingToken);

        try
        {
            await Task.WhenAny(openAITask, generalTask);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Analytics update service cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in analytics update service");
        }
        finally
        {
            openAITimer.Dispose();
            generalTimer.Dispose();
        }
    }

    /// <summary>
    /// Handle frequent OpenAI balance updates
    /// </summary>
    private async Task HandleOpenAIUpdates(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await BroadcastOpenAIBalanceUpdate(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting OpenAI balance update");
            }
        }
    }

    /// <summary>
    /// Handle general analytics updates
    /// </summary>
    private async Task HandleGeneralUpdates(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await BroadcastGeneralAnalyticsUpdate(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting general analytics update");
            }
        }
    }

    /// <summary>
    /// Broadcast OpenAI balance update specifically
    /// </summary>
    private async Task BroadcastOpenAIBalanceUpdate(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var cachedProviderService = scope.ServiceProvider.GetRequiredService<ICachedProviderAggregationService>();
        
        try
        {
            var accountInfos = await cachedProviderService.GetAllAccountInfoAsync(cancellationToken);
            var openAIAccount = accountInfos.FirstOrDefault(a => 
                string.Equals(a.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase));
            
            if (openAIAccount != null)
            {
                var updateData = new
                {
                    Provider = "OpenAI",
                    Balance = openAIAccount.Balance,
                    BalanceUnit = openAIAccount.BalanceUnit,
                    MonthlyUsage = openAIAccount.MonthlyUsage,
                    IsConnected = openAIAccount.IsConnected,
                    LastUpdated = DateTime.UtcNow,
                    UpdateType = "balance"
                };

                await _hubContext.Clients.Group("Analytics")
                    .SendAsync("OpenAIBalanceUpdate", updateData, cancellationToken);
                
                _logger.LogDebug("Broadcast OpenAI balance update: Balance={Balance}, Usage={Usage}", 
                    openAIAccount.Balance, openAIAccount.MonthlyUsage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast OpenAI balance update");
            
            // Send error notification to clients
            await _hubContext.Clients.Group("Analytics")
                .SendAsync("OpenAIBalanceError", new
                {
                    Error = "Failed to retrieve OpenAI balance",
                    Timestamp = DateTime.UtcNow
                }, cancellationToken);
        }
    }

    /// <summary>
    /// Broadcast general analytics update
    /// </summary>
    private async Task BroadcastGeneralAnalyticsUpdate(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var cachedProviderService = scope.ServiceProvider.GetRequiredService<ICachedProviderAggregationService>();
        var cachedAnalyticsService = scope.ServiceProvider.GetRequiredService<ICachedAnalyticsService>();
        
        try
        {
            // Get latest data
            var accountInfosTask = cachedProviderService.GetAllAccountInfoAsync(cancellationToken);
            var summaryTask = cachedProviderService.GetProviderSummaryAsync(30, cancellationToken);
            var modelStatsTask = cachedAnalyticsService.GetModelUsageStatsAsync(cancellationToken);

            await Task.WhenAll(accountInfosTask, summaryTask, modelStatsTask);

            var updateData = new
            {
                AccountInfos = await accountInfosTask,
                Summary = await summaryTask,
                ModelStats = await modelStatsTask,
                Timestamp = DateTime.UtcNow,
                UpdateType = "general"
            };

            await _hubContext.Clients.Group("Analytics")
                .SendAsync("AnalyticsUpdate", updateData, cancellationToken);
            
            _logger.LogDebug("Broadcast general analytics update");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast general analytics update");
        }
    }

    public override void Dispose()
    {
        _logger.LogInformation("Analytics update service disposing");
        base.Dispose();
    }
}