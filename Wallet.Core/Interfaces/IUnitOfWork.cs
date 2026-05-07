namespace Wallet.Core.Interfaces;

public interface IUnitOfWork
{
    Task BeginTransaction(CancellationToken token=default);
    Task CommitTransaction(CancellationToken token=default);
    Task RollbackTransaction(CancellationToken token=default);
    Task<int> SaveChanges(CancellationToken token=default);
}