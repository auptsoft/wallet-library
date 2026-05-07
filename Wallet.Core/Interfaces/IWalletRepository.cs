namespace Wallet.Core.Interfaces;

public interface IWalletRepository
{
    Task<Core.Entities.Wallet> CreateWallet(Core.Entities.Wallet wallet);
    Task<Core.Entities.Wallet?> GetWallet(Guid id);
    Task<Core.Entities.Wallet?> GetWalletByName(string name);
    Task<Core.Entities.Wallet?> GetWalletByIdentifier(string identifier);
}