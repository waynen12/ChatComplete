using Knowledge.Analytics.Services;
using Knowledge.Analytics.Models;
using Microsoft.AspNetCore.SignalR;

namespace Knowledge.Api.Hubs;

/// <summary>
/// SignalR hub for real-time analytics updates
/// </summary>
public class AnalyticsHub : Hub
{
    private readonly ICachedProviderAggregationService _cachedProviderService;
    private readonly ILogger<AnalyticsHub> _logger;

    /// <summary>
    /// Initializes a new instance of the AnalyticsHub.
    /// </summary>
    /// <param name="cachedProviderService">The cached provider aggregation service.</param>
    /// <param name="logger">The logger instance.</param>
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
    /// Request specific provider update (compatibility method for widgets)
    /// </summary>
    public async Task RequestProviderUpdate(string provider)
    {
        await RequestProviderData(provider);
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
                // Send provider-specific update
                if (string.Equals(provider, "Anthropic", StringComparison.OrdinalIgnoreCase))
                {
                    await SendAnthropicUsageUpdate(providerData);
                }
                else
                {
                    await Clients.Caller.SendAsync("ProviderDataUpdate", new
                    {
                        Provider = provider,
                        Data = providerData,
                        Timestamp = DateTime.UtcNow
                    });
                }
                
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

    /// <summary>
    /// Send Anthropic-specific usage update with detailed billing data
    /// </summary>
    private async Task SendAnthropicUsageUpdate(object providerDataObj)
    {
        var providerData = (ProviderAccountInfo)providerDataObj;
        
        await Clients.Caller.SendAsync("AnthropicUsageUpdate", new
        {
            provider = "Anthropic",
            isConnected = providerData.IsConnected,
            lastUpdated = DateTime.UtcNow.ToString("O"),
            updateType = "usage",
            // Basic data from provider account info
            totalCost = 0m, // Will be populated by background service with detailed data
            totalRequests = 0,
            totalTokens = 0,
            totalInputTokens = 0,
            totalOutputTokens = 0,
            webSearchRequests = 0,
            uniqueModels = 0,
            hasAdminKey = providerData.IsConnected, // Admin key presence is indicated by successful connection
            billingAccess = providerData.IsConnected, // Billing access is available if connection is successful
            modelBreakdown = new List<object>(),
            startDate = DateTime.UtcNow.AddDays(-30).ToString("O"),
            endDate = DateTime.UtcNow.ToString("O")
        });

        _logger.LogDebug("Sent Anthropic usage update to client {ConnectionId}", Context.ConnectionId);
    }
}