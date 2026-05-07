using System.ComponentModel.DataAnnotations;
using Wallet.Contract.Enums;

namespace Wallet.Core.Entities;

public class Wallet: BaseModel
{
    public string UserId { get; set; }
    public string WalletName { get; set; } = string.Empty;
    public string WalletIdentifier { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; }
    public bool IsLocked { get; set; }
    public DateTime LockedAt { get; set; }
    public bool IsPlatformWallet { get; set; }
    public bool AllowNegative { get; set; }
    
    public int RowVersion { get; set; }
    
    public WalletType WalletType { get; set; }
    
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    
    public static string GetEntityName() => "wallets";
    
    public int IncrementRowVersion() => RowVersion++;
}