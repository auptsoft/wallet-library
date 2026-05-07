using Microsoft.EntityFrameworkCore;
using Wallet.Core.Entities;
using Wallet.Core.Interfaces;

namespace Wallet.Infrastructure.Repositories;

public class IdempotencyRecordRepository(WalletDbContext dbContext): IIdempotencyRecordRepository
{
    public async Task<IdempotencyRecord?> GetIdempotencyRecordByKey(string key)
    {
        var wallet = await dbContext.IdempotencyRecords.FirstOrDefaultAsync(x=>x.IdempotencyKey == key);
        return wallet;
    }

    public void CreateIdempotencyRecord(IdempotencyRecord record)
    {
        dbContext.IdempotencyRecords.Add(record);
    }
}