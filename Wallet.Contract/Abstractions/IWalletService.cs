using Wallet.Contract.DTOs;

namespace Wallet.Contract.Abstractions;

public interface IWalletService
{
    Task<WalletDto> CreateWallet(string userId, string currency, string walletName, bool isPlatformWallet, bool allowNegative, string walletIdentifier);
    Task<WalletDto?> GetWallet(Guid walletId);
    Task<TransferResponse> WalletTransfer(TransferRequest request);
    Task<WalletDto?> GetWalletByIdentifier(string identifier);
    Task<WalletDto?> GetWalletByName(string walletName);
}