using Knowledge.Analytics.Services;
using Microsoft.AspNetCore.SignalR;

namespace Knowledge.Api.Hubs;

/// <summary>
/// SignalR hub for real-time analytics updates
/// </summary>
public class AnalyticsHub : Hub
{
    private readonly ICachedProviderAggregationService _cachedProviderService;
    private readonly ILogger<AnalyticsHub> _logger;

    public AnalyticsHub(
        ICachedProviderAggregationService cachedProviderService,
        ILogger<AnalyticsHub> logger)
    {
        _cachedProviderService = cachedProviderService;
        _logger = logger;
    }

    /// <summary>
    /// Client connects to analytics updates
    /// </summary>
    public async Task JoinAnalyticsGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Analytics");
        _logger.LogDebug("Client {ConnectionId} joined Analytics group", Context.ConnectionId);
        
        // Send initial data immediately upon connection
        await SendInitialAnalyticsData();
    }

    /// <summary>
    /// Client disconnects from analytics updates
    /// </summary>
    public async Task LeaveAnalyticsGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Analytics");
        _logger.LogDebug("Client {ConnectionId} left Analytics group", Context.ConnectionId);
    }

    /// <summary>
    /// Request specific provider data (e.g., OpenAI balance)
    /// </summary>
    public async Task RequestProviderData(string provider)
    {
        try
        {
            var accountInfos = await _cachedProviderService.GetAllAccountInfoAsync();
            var providerData = accountInfos.FirstOrDefault(p => 
                string.Equals(p.Provider, provider, StringComparison.OrdinalIgnoreCase));
            
            if (providerData != null)
            {
                await Clients.Caller.SendAsync("ProviderDataUpdate", new
                {
                    Provider = provider,
                    Data = providerData,
                    Timestamp = DateTime.UtcNow
                });
                
                _logger.LogDebug("Sent {Provider} data to client {ConnectionId}", provider, Context.ConnectionId);
            }
            else
            {
                await Clients.Caller.SendAsync("ProviderDataError", new
                {
                    Provider = provider,
                    Error = "Provider not found or not configured",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending provider data for {Provider} to client {ConnectionId}", 
                provider, Context.ConnectionId);
            
            await Clients.Caller.SendAsync("ProviderDataError", new
            {
                Provider = provider,
                Error = "Failed to retrieve provider data",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Override connection handling
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to AnalyticsHub", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Override disconnection handling
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client {ConnectionId} disconnected with error", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Send initial analytics data to newly connected client
    /// </summary>
    private async Task SendInitialAnalyticsData()
    {
        try
        {
            // Get initial provider account data
            var accountInfos = await _cachedProviderService.GetAllAccountInfoAsync();
            var summary = await _cachedProviderService.GetProviderSummaryAsync(30);

            await Clients.Caller.SendAsync("InitialAnalyticsData", new
            {
                AccountInfos = accountInfos,
                Summary = summary,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogDebug("Sent initial analytics data to client {ConnectionId}", Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending initial analytics data to client {ConnectionId}", Context.ConnectionId);
        }
    }
}