using Wallet.Contract.DTOs;

namespace Wallet.Contract.Abstractions;

public interface IWalletQueryService
{
    Task<(List<WalletTransactionDto>, long total, long perPage, int page)> GetWalletTransactions(
        Guid? walletId, DateTime? startDate, DateTime? endDate, int page, int perPage);

    Task<(List<WalletTransferDto>, long total, long perPage, int page)> GetWalletTransfers(
        Guid? toWalletId, Guid? fromWalletId, DateTime? startDate, DateTime? endDate, int page, int perPage);

    Task<WalletTransferDto?> GetWalletTransferByReference(string reference);
    Task<List<WalletTransactionDto?>> GetWalletTransactionsByTransferReference(string reference);
}