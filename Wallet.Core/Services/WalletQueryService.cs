using Wallet.Contract.Abstractions;
using Wallet.Contract.DTOs;
using Wallet.Core.Entities;
using Wallet.Core.Interfaces;

namespace WalletModule.Services;

public class WalletQueryService(IWalletTransferRepository walletTransferRepository, IWalletTransactionRepository walletTransactionRepository): IWalletQueryService
{
    public async Task<(List<WalletTransactionDto>, long total, long perPage, int page)> GetWalletTransactions(
        Guid? walletId, DateTime? startDate, DateTime? endDate, int page, int perPage)
    {
        var response = await walletTransactionRepository.GetWalletTransactions(walletId, startDate, endDate, page, perPage);
        var data = response.Item1.Select(GetWalletTransactionDto).ToList();
        return (data, response.total, response.perPage, response.page);
    }

    public async Task<(List<WalletTransferDto>, long total, long perPage, int page)> GetWalletTransfers(
        Guid? toWalletId, Guid? fromWalletId, DateTime? startDate, DateTime? endDate, int page, int perPage)
    {
        var response =  await walletTransferRepository.GetWalletTransfers(toWalletId, fromWalletId, startDate, endDate, page, perPage);
        var data = response.Item1.Select(GetWalletTransferDto).ToList();
        return (data, response.total, response.perPage, response.page);
    }

    public async Task<WalletTransferDto?> GetWalletTransferByReference(string reference)
    {
        var data = await walletTransferRepository.GetByReference(reference);
        return GetWalletTransferDto(data);
    }

    public async Task<List<WalletTransactionDto?>> GetWalletTransactionsByTransferReference(string reference)
    {
        var data = await walletTransactionRepository.GetWalletTransactions(x => x.WalletTransferReference == reference);
        return data.Select(GetWalletTransactionDto).ToList();
    }
    
    
    private WalletTransactionDto? GetWalletTransactionDto(WalletTransaction? walletTransaction)
    {
        if (walletTransaction == null) return null;
        
        return new WalletTransactionDto()
        {
            WalletId =  walletTransaction.WalletId,
            CreatedAt = walletTransaction.CreatedAt,
            Description = walletTransaction.Description,
            Amount =  walletTransaction.Amount,
            BankTransferId =  walletTransaction.BankTransferId,
            Direction =  walletTransaction.Direction,
            Id =  walletTransaction.Id,
            TransferId =   walletTransaction.TransferId,
            UpdatedAt =   walletTransaction.UpdatedAt,
        };
    }

    private WalletTransferDto? GetWalletTransferDto(WalletTransfer? walletTransfer)
    {
        if (walletTransfer == null) return null;
        
        return new WalletTransferDto()
        {
            Id = walletTransfer.Id,
            CreatedAt = walletTransfer.CreatedAt,
            Amount = walletTransfer.Amount,
            FromWalletId = walletTransfer.FromWalletId,
            ToWalletId = walletTransfer.ToWalletId,
            Reference = walletTransfer.Reference,
            Status = walletTransfer.Status,
            UpdatedAt =  walletTransfer.UpdatedAt,
        };
    }
}