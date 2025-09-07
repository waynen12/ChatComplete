using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Knowledge.Analytics.Services;

public class BackgroundSyncOptions
{
    public TimeSpan AccountSyncInterval { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan UsageSyncInterval { get; set; } = TimeSpan.FromHours(1);
    public bool EnableBackgroundSync { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(2);
}

public interface IBackgroundSyncService
{
    Task SyncAllProvidersAsync(CancellationToken cancellationToken = default);
    Task SyncProviderAccountsAsync(CancellationToken cancellationToken = default);
    Task SyncProviderUsageAsync(CancellationToken cancellationToken = default);
    BackgroundSyncStatus GetSyncStatus();
}

public record BackgroundSyncStatus
{
    public DateTime LastAccountSync { get; init; } = DateTime.MinValue;
    public DateTime LastUsageSync { get; init; } = DateTime.MinValue;
    public DateTime NextAccountSync { get; init; } = DateTime.MinValue;
    public DateTime NextUsageSync { get; init; } = DateTime.MinValue;
    public bool IsRunning { get; init; }
    public string? LastError { get; init; }
    public int SuccessfulSyncs { get; init; }
    public int FailedSyncs { get; init; }
}

public class BackgroundSyncService : BackgroundService, IBackgroundSyncService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundSyncService> _logger;
    private readonly BackgroundSyncOptions _options;
    
    private DateTime _lastAccountSync = DateTime.MinValue;
    private DateTime _lastUsageSync = DateTime.MinValue;
    private bool _isRunning = false;
    private string? _lastError = null;
    private int _successfulSyncs = 0;
    private int _failedSyncs = 0;

    public BackgroundSyncService(
        IServiceProvider serviceProvider,
        ILogger<BackgroundSyncService> logger,
        IOptions<BackgroundSyncOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableBackgroundSync)
        {
            _logger.LogInformation("Background sync is disabled");
            return;
        }

        _logger.LogInformation("Background sync service started with intervals: Accounts={AccountInterval}, Usage={UsageInterval}",
            _options.AccountSyncInterval, _options.UsageSyncInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var shouldSyncAccounts = now - _lastAccountSync >= _options.AccountSyncInterval;
                var shouldSyncUsage = now - _lastUsageSync >= _options.UsageSyncInterval;

                if (shouldSyncAccounts || shouldSyncUsage)
                {
                    _isRunning = true;
                    
                    if (shouldSyncAccounts)
                    {
                        await SyncProviderAccountsAsync(stoppingToken);
                    }
                    
                    if (shouldSyncUsage)
                    {
                        await SyncProviderUsageAsync(stoppingToken);
                    }
                    
                    _isRunning = false;
                }

                // Wait for the shorter of the two intervals before checking again
                var nextCheck = TimeSpan.FromMinutes(Math.Min(
                    _options.AccountSyncInterval.TotalMinutes,
                    _options.UsageSyncInterval.TotalMinutes
                ) / 4); // Check 4 times per interval

                await Task.Delay(nextCheck, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Background sync service cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background sync service main loop");
                _lastError = ex.Message;
                _failedSyncs++;
                
                // Wait before retrying
                await Task.Delay(_options.RetryDelay, stoppingToken);
            }
        }
    }

    public async Task SyncAllProvidersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting full provider sync");
        
        await SyncProviderAccountsAsync(cancellationToken);
        await SyncProviderUsageAsync(cancellationToken);
        
        _logger.LogInformation("Completed full provider sync");
    }

    public async Task SyncProviderAccountsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting provider accounts sync");
        
        await ExecuteWithRetry(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var cachedAggregationService = scope.ServiceProvider
                .GetRequiredService<ICachedProviderAggregationService>();
            
            // Refresh all provider data (which includes accounts)
            await cachedAggregationService.RefreshAllProviderDataAsync(cancellationToken);
            
            _lastAccountSync = DateTime.UtcNow;
            _successfulSyncs++;
            _lastError = null;
            
            _logger.LogDebug("Provider accounts sync completed successfully");
        }, "account sync", cancellationToken);
    }

    public async Task SyncProviderUsageAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting provider usage sync");
        
        await ExecuteWithRetry(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var cachedAggregationService = scope.ServiceProvider
                .GetRequiredService<ICachedProviderAggregationService>();
            var cacheService = scope.ServiceProvider
                .GetRequiredService<IAnalyticsCacheService>();
            
            // Invalidate usage-related cache to force refresh
            await cacheService.InvalidatePatternAsync(
                AnalyticsCacheService.CacheKeys.ProviderUsage, cancellationToken);
            await cacheService.InvalidatePatternAsync(
                AnalyticsCacheService.CacheKeys.ProviderSummary, cancellationToken);
            
            // Fetch fresh usage data for the last 30 days
            await cachedAggregationService.GetAllUsageInfoAsync(30, cancellationToken);
            await cachedAggregationService.GetProviderSummaryAsync(30, cancellationToken);
            
            _lastUsageSync = DateTime.UtcNow;
            _successfulSyncs++;
            _lastError = null;
            
            _logger.LogDebug("Provider usage sync completed successfully");
        }, "usage sync", cancellationToken);
    }

    public BackgroundSyncStatus GetSyncStatus()
    {
        var now = DateTime.UtcNow;
        
        return new BackgroundSyncStatus
        {
            LastAccountSync = _lastAccountSync,
            LastUsageSync = _lastUsageSync,
            NextAccountSync = _lastAccountSync == DateTime.MinValue 
                ? now 
                : _lastAccountSync.Add(_options.AccountSyncInterval),
            NextUsageSync = _lastUsageSync == DateTime.MinValue 
                ? now 
                : _lastUsageSync.Add(_options.UsageSyncInterval),
            IsRunning = _isRunning,
            LastError = _lastError,
            SuccessfulSyncs = _successfulSyncs,
            FailedSyncs = _failedSyncs
        };
    }

    private async Task ExecuteWithRetry(Func<Task> operation, string operationName, CancellationToken cancellationToken)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < _options.MaxRetryAttempts)
        {
            try
            {
                await operation();
                return; // Success
            }
            catch (Exception ex) when (attempt < _options.MaxRetryAttempts - 1)
            {
                attempt++;
                lastException = ex;
                
                _logger.LogWarning(ex, "Attempt {Attempt}/{MaxAttempts} failed for {Operation}. Retrying in {Delay}",
                    attempt, _options.MaxRetryAttempts, operationName, _options.RetryDelay);
                
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_options.RetryDelay, cancellationToken);
                }
            }
        }

        // All retries failed
        _failedSyncs++;
        _lastError = lastException?.Message;
        
        _logger.LogError(lastException, "All {MaxAttempts} attempts failed for {Operation}",
            _options.MaxRetryAttempts, operationName);
        
        throw lastException ?? new InvalidOperationException($"Failed to execute {operationName}");
    }

    public override void Dispose()
    {
        _logger.LogInformation("Background sync service disposing");
        base.Dispose();
    }
}