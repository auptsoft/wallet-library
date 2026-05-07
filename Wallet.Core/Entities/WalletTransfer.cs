
using Wallet.Contract.Enums;

namespace Wallet.Core.Entities;

public class WalletTransfer: BaseModel
{
    public Guid FromWalletId { get; set; }

    public Guid ToWalletId { get; set; }

    public decimal Amount { get; set; }

    public TransferStatus Status { get; set; }

    public string Reference { get; set; }

    public Wallet FromWallet { get; set; }

    public Wallet ToWallet { get; set; }
    
    public string? Narration { get; set; }

    public ICollection<WalletTransaction> Transactions { get; set; }
    
    public static string GetEntityName() => "wallet_transfers";
}