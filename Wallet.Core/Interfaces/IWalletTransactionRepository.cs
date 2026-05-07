using Wallet.Core.Entities;

namespace Wallet.Core.Interfaces;

public interface IWalletTransactionRepository
{
    void Add(WalletTransaction walletTransaction);

    Task<(List<WalletTransaction>, long total, long perPage, int page)> GetWalletTransactions(
        Guid? walletId, DateTime? startDate, DateTime? endDate, int page = 1, int perPage = 10);

    Task<(List<WalletTransaction>, long total, long perPage, int page)> GetWalletTransactions(
        Func<WalletTransaction, bool> predicate,
        int page = 1, int perPage = 10);

    Task<List<WalletTransaction>> GetWalletTransactions(
        Func<WalletTransaction, bool> predicate);
}