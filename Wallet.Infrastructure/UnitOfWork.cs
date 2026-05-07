using Microsoft.EntityFrameworkCore.Storage;
using Wallet.Core.Interfaces;

namespace Wallet.Infrastructure;

public class UnitOfWork(WalletDbContext dbContext): IUnitOfWork
{
    private IDbContextTransaction? _transaction;
    
    public async Task BeginTransaction(CancellationToken token=default)
    {
        if (_transaction != null) return;
        _transaction =  await dbContext.Database.BeginTransactionAsync(token);
    }

    public async Task CommitTransaction(CancellationToken token=default)
    {
        try
        {
            await SaveChanges(token);
            if (_transaction != null)
            {
                await _transaction.CommitAsync(token);
            }
        }
        catch (Exception)
        {
            await RollbackTransaction(token);
        }
        finally
        {
            await DisposeAsync();
        }
    }

    public async Task RollbackTransaction(CancellationToken token=default)
    {
        if (_transaction == null) return;
        await _transaction.RollbackAsync(token);
        await DisposeAsync();
    }

    public async Task<int> SaveChanges(CancellationToken token=default)
    {
        return await dbContext.SaveChangesAsync(token);
    }
    
    private async Task DisposeAsync()
    {
        if (_transaction != null) {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}