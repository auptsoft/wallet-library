using System.Security.Cryptography;
using System.Text;
using Wallet.Contract.DTOs;

namespace Wallet.Core.Entities;

public class IdempotencyRecord: BaseModel
{
    public string IdempotencyKey { get; set; }
    public string RequestPath { get; set; }
    public string RequestHash { get; set; }
    
    public static string GetEntityName() => "idempotency_records";
    
    
    public static string ComputeHash(TransferRequest request)
    {
        var canonical = string.Join("|",
            request.FromWalletId.ToString(),
            request.ToWalletId.ToString(),
            request.Amount.ToString("F2"), // normalize decimals
            request.Reference?.Trim().ToLower() ?? ""
        );

        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(canonical);
        var hash = sha.ComputeHash(bytes);

        return Convert.ToHexString(hash);
    }
}