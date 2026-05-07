using Wallet.Contract.Enums;

namespace Wallet.Core.Entities;

public class WalletTransaction: BaseModel
{
    public Guid WalletId { get; set; }

    public decimal Amount { get; set; }

    public TransactionDirection Direction { get; set; }

    public Guid? TransferId { get; set; }

    public Guid? BankTransferId { get; set; }

    public string Description { get; set; }
    
    public string WalletTransferReference { get; set; }

    public Wallet Wallet { get; set; }

    public WalletTransfer Transfer { get; set; }
    
    public static string GetEntityName() => "wallet_transactions";
}