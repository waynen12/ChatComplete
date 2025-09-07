using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Knowledge.Analytics.Services;

public interface IProviderApiRateLimiter
{
    Task<bool> CanMakeRequestAsync(string provider, CancellationToken cancellationToken = default);
    Task RecordRequestAsync(string provider, bool successful, CancellationToken cancellationToken = default);
    Task<RateLimitStatus> GetStatusAsync(string provider, CancellationToken cancellationToken = default);
    Task<IEnumerable<RateLimitStatus>> GetAllStatusAsync(CancellationToken cancellationToken = default);
    Task ResetLimitsAsync(string provider, CancellationToken cancellationToken = default);
}

public record RateLimitStatus
{
    public string Provider { get; init; } = string.Empty;
    public int RequestsInCurrentWindow { get; init; }
    public int MaxRequestsPerWindow { get; init; }
    public TimeSpan WindowDuration { get; init; }
    public DateTime WindowStartTime { get; init; }
    public DateTime NextResetTime => WindowStartTime.Add(WindowDuration);
    public bool IsLimited => RequestsInCurrentWindow >= MaxRequestsPerWindow;
    public int RemainingRequests => Math.Max(0, MaxRequestsPerWindow - RequestsInCurrentWindow);
    public TimeSpan TimeUntilReset => NextResetTime > DateTime.UtcNow 
        ? NextResetTime - DateTime.UtcNow 
        : TimeSpan.Zero;
    public int FailedRequests { get; init; }
    public double SuccessRate => TotalRequests > 0 ? (double)(TotalRequests - FailedRequests) / TotalRequests * 100 : 0;
    public int TotalRequests => RequestsInCurrentWindow;
}

public class ProviderApiRateLimiter : IProviderApiRateLimiter
{
    private readonly ILogger<ProviderApiRateLimiter> _logger;
    private readonly ConcurrentDictionary<string, ProviderLimitTracker> _trackers = new();

    // Rate limits per provider (requests per minute)
    private static readonly Dictionary<string, ProviderLimits> DefaultProviderLimits = new()
    {
        ["OpenAi"] = new(60, TimeSpan.FromMinutes(1)), // OpenAI: 60 requests/minute (conservative)
        ["Anthropic"] = new(30, TimeSpan.FromMinutes(1)), // Anthropic: 30 requests/minute
        ["Google"] = new(60, TimeSpan.FromMinutes(1)), // Google AI: 60 requests/minute
        ["Ollama"] = new(1000, TimeSpan.FromMinutes(1)) // Ollama: No real limits (local)
    };

    public ProviderApiRateLimiter(ILogger<ProviderApiRateLimiter> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanMakeRequestAsync(string provider, CancellationToken cancellationToken = default)
    {
        var tracker = GetOrCreateTracker(provider);
        var canMake = tracker.CanMakeRequest();
        
        if (!canMake)
        {
            _logger.LogWarning("Rate limit exceeded for provider {Provider}. Requests: {Current}/{Max}", 
                provider, tracker.RequestsInCurrentWindow, tracker.Limits.MaxRequests);
        }
        
        return Task.FromResult(canMake);
    }

    public Task RecordRequestAsync(string provider, bool successful, CancellationToken cancellationToken = default)
    {
        var tracker = GetOrCreateTracker(provider);
        tracker.RecordRequest(successful);
        
        _logger.LogDebug("Recorded request for provider {Provider}. Success: {Successful}, Current: {Current}/{Max}",
            provider, successful, tracker.RequestsInCurrentWindow, tracker.Limits.MaxRequests);
        
        return Task.CompletedTask;
    }

    public Task<RateLimitStatus> GetStatusAsync(string provider, CancellationToken cancellationToken = default)
    {
        var tracker = GetOrCreateTracker(provider);
        return Task.FromResult(tracker.GetStatus());
    }

    public Task<IEnumerable<RateLimitStatus>> GetAllStatusAsync(CancellationToken cancellationToken = default)
    {
        var statuses = _trackers.Values.Select(t => t.GetStatus()).ToList();
        return Task.FromResult<IEnumerable<RateLimitStatus>>(statuses);
    }

    public Task ResetLimitsAsync(string provider, CancellationToken cancellationToken = default)
    {
        if (_trackers.TryGetValue(provider, out var tracker))
        {
            tracker.Reset();
            _logger.LogInformation("Reset rate limits for provider {Provider}", provider);
        }
        
        return Task.CompletedTask;
    }

    private ProviderLimitTracker GetOrCreateTracker(string provider)
    {
        return _trackers.GetOrAdd(provider, p =>
        {
            var limits = DefaultProviderLimits.TryGetValue(p, out var providerLimits)
                ? providerLimits
                : new ProviderLimits(60, TimeSpan.FromMinutes(1)); // Default limits
                
            return new ProviderLimitTracker(p, limits);
        });
    }

    private record ProviderLimits(int MaxRequests, TimeSpan WindowDuration);

    private class ProviderLimitTracker
    {
        private readonly object _lock = new();
        private readonly Queue<RequestRecord> _requests = new();
        private int _failedRequests = 0;
        
        public string Provider { get; }
        public ProviderLimits Limits { get; }
        public DateTime WindowStartTime { get; private set; } = DateTime.UtcNow;
        public int RequestsInCurrentWindow => _requests.Count;

        public ProviderLimitTracker(string provider, ProviderLimits limits)
        {
            Provider = provider;
            Limits = limits;
        }

        public bool CanMakeRequest()
        {
            lock (_lock)
            {
                CleanupOldRequests();
                return _requests.Count < Limits.MaxRequests;
            }
        }

        public void RecordRequest(bool successful)
        {
            lock (_lock)
            {
                CleanupOldRequests();
                _requests.Enqueue(new RequestRecord(DateTime.UtcNow, successful));
                
                if (!successful)
                {
                    _failedRequests++;
                }
            }
        }

        public RateLimitStatus GetStatus()
        {
            lock (_lock)
            {
                CleanupOldRequests();
                
                return new RateLimitStatus
                {
                    Provider = Provider,
                    RequestsInCurrentWindow = RequestsInCurrentWindow,
                    MaxRequestsPerWindow = Limits.MaxRequests,
                    WindowDuration = Limits.WindowDuration,
                    WindowStartTime = WindowStartTime,
                    FailedRequests = _failedRequests
                };
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _requests.Clear();
                _failedRequests = 0;
                WindowStartTime = DateTime.UtcNow;
            }
        }

        private void CleanupOldRequests()
        {
            var cutoff = DateTime.UtcNow - Limits.WindowDuration;
            var removedFailures = 0;
            
            while (_requests.Count > 0 && _requests.Peek().Timestamp < cutoff)
            {
                var removed = _requests.Dequeue();
                if (!removed.Successful)
                {
                    removedFailures++;
                }
            }
            
            _failedRequests = Math.Max(0, _failedRequests - removedFailures);
            
            // Update window start time if we removed requests
            if (_requests.Count == 0)
            {
                WindowStartTime = DateTime.UtcNow;
            }
            else if (_requests.Count > 0)
            {
                WindowStartTime = _requests.Peek().Timestamp;
            }
        }
    }

    private record RequestRecord(DateTime Timestamp, bool Successful);
}