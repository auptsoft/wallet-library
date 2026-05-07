using Wallet.Core.Entities;

namespace Wallet.Core.Interfaces;

public interface IWalletTransferRepository
{
    void Add(WalletTransfer walletTransfer);

    Task<(List<WalletTransfer>, long total, long perPage, int page)> GetWalletTransfers(
        Guid? toWalletId, Guid? fromWalletId, DateTime? startDate, DateTime? endDate, int page = 1, int perPage = 10);

    Task<(List<WalletTransfer>, long total, long perPage, int page)> GetWalletTransfers(
        Func<WalletTransfer, bool> predicate,
        int page = 1, int perPage = 10);

    Task<List<WalletTransfer>> GetWalletTransfers(
        Func<WalletTransfer, bool> predicate);

    Task<WalletTransfer?> GetById(Guid id);

    Task<WalletTransfer?> GetByReference(string reference);
}
