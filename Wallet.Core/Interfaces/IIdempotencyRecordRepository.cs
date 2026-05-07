using Wallet.Core.Entities;

namespace Wallet.Core.Interfaces;

public interface IIdempotencyRecordRepository
{
    Task<IdempotencyRecord?> GetIdempotencyRecordByKey(string key);
    void CreateIdempotencyRecord(IdempotencyRecord record);
}