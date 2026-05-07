using Microsoft.EntityFrameworkCore;
using Wallet.Core.Entities;
using Wallet.Core.Interfaces;

namespace Wallet.Infrastructure.Repositories;

public class WalletTransactionRepository(WalletDbContext dbContext): IWalletTransactionRepository
{
    public void Add(WalletTransaction walletTransaction)
    {
        dbContext.WalletTransactions.Add(walletTransaction);
    }
    
    public async Task<(List<WalletTransaction>, long total, long perPage, int page)> GetWalletTransactions(
        Guid? walletId, DateTime? startDate, DateTime? endDate, int page=1, int perPage=10)
    {
        var query = dbContext.WalletTransactions.AsNoTracking().AsQueryable();
        if (walletId != null)
        {
            query = query.Where(x=>x.WalletId == walletId);
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
    
    public async Task<(List<WalletTransaction>, long total, long perPage, int page)> GetWalletTransactions(
        Func<WalletTransaction, bool> predicate,
        int page=1, int perPage=10)
    {
        var query = dbContext.WalletTransactions.AsNoTracking().Where(predicate).AsQueryable();
        
        var total = await query.CountAsync();
        var data = await query.Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();
        
        return (data, total, perPage, page);
    }
    
    public async Task<List<WalletTransaction>> GetWalletTransactions(
        Func<WalletTransaction, bool> predicate)
    {
        var query = dbContext.WalletTransactions.Where(predicate).AsQueryable();
        return await query.ToListAsync();
    }
}