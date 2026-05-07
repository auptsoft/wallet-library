using Microsoft.EntityFrameworkCore;
using Wallet.Core.Entities;
using Wallet.Core.Interfaces;

namespace Wallet.Infrastructure.Repositories;

public class WalletTransferRepository(WalletDbContext dbContext): IWalletTransferRepository
{
    public void Add(Core.Entities.WalletTransfer walletTransfer)
    {
        dbContext.WalletTransfers.Add(walletTransfer);
    }
    
    public async Task<(List<WalletTransfer>, long total, long perPage, int page)> GetWalletTransfers(
        Guid? toWalletId, Guid? fromWalletId, DateTime? startDate, DateTime? endDate, int page=1, int perPage=10)
    {
        var query = dbContext.WalletTransfers.AsNoTracking().AsQueryable();
        if (toWalletId == null)
        {
            query = query.Where(x=>x.ToWalletId == toWalletId);
        }

        if (fromWalletId == null)
        {
            query = query.Where(x=>x.FromWalletId == fromWalletId);
        }

        if (startDate != null)
        {
            query = query.Where(x=>x.CreatedAt >= startDate);
        }

        if (endDate != null)
        {
            query = query.Where(x=>x.CreatedAt <= endDate);
        }

        var total = await query.CountAsync();
        var data = await query.Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();
        
        return (data, total, perPage, page);
    }
    
    public async Task<(List<WalletTransfer>, long total, long perPage, int page)> GetWalletTransfers(
        Func<WalletTransfer, bool> predicate,
        int page=1, int perPage=10)
    {
        var query = dbContext.WalletTransfers.AsNoTracking().Where(predicate).AsQueryable();
        
        var total = await query.CountAsync();
        var data = await query.Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();
        
        return (data, total, perPage, page);
    }
    
    public async Task<List<WalletTransfer>> GetWalletTransfers(
        Func<WalletTransfer, bool> predicate)
    {
        var query = dbContext.WalletTransfers.Where(predicate).AsQueryable();
        return await query.ToListAsync();
    }

    public async Task<WalletTransfer?> GetById(Guid id)
    {
        return await dbContext.WalletTransfers.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<WalletTransfer?> GetByReference(string reference)
    {
        return await dbContext.WalletTransfers.FirstOrDefaultAsync(x => x.Reference == reference);
    }
}