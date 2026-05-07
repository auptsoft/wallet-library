using Microsoft.EntityFrameworkCore;
using Wallet.Core.Interfaces;

namespace Wallet.Infrastructure.Repositories;

public class WalletRepository(WalletDbContext dbContext): IWalletRepository
{
    public async Task<Core.Entities.Wallet?> GetWallet(Guid id)
    {
        var wallet = await dbContext.Wallets.FindAsync(id);
        return wallet;
    }

    public async Task<Core.Entities.Wallet> CreateWallet(Core.Entities.Wallet wallet)
    {
        var response = await dbContext.Wallets.AddAsync(wallet);
        await dbContext.SaveChangesAsync();
        return response.Entity;
    }

    public async Task<Core.Entities.Wallet?> GetWalletByName(string name)
    {
        var wallet = await dbContext.Wallets.FirstOrDefaultAsync(x=>x.WalletName ==name);
        return wallet;
    }

    public async Task<Core.Entities.Wallet?> GetWalletByIdentifier(string identifier)
    {
        var wallet = await dbContext.Wallets.FirstOrDefaultAsync(x => x.WalletIdentifier == identifier);
        return wallet;
    }
    
}