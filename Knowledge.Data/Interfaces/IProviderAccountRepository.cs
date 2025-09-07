using Knowledge.Entities;

namespace Knowledge.Data.Interfaces;

public interface IProviderAccountRepository
{
    Task<IEnumerable<ProviderAccountRecord>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<ProviderAccountRecord?> GetAccountByProviderAsync(string provider, CancellationToken cancellationToken = default);
    Task CreateOrUpdateAccountAsync(ProviderAccountRecord account, CancellationToken cancellationToken = default);
    Task UpdateConnectionStatusAsync(string provider, bool isConnected, string? errorMessage = null, CancellationToken cancellationToken = default);
    Task UpdateBalanceAsync(string provider, decimal? balance, string? balanceUnit = null, CancellationToken cancellationToken = default);
    Task UpdateLastSyncAsync(string provider, DateTime syncTime, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetConnectedProvidersAsync(CancellationToken cancellationToken = default);
}