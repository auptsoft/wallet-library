
using Wallet.Contract.Enums;

namespace Wallet.Contract.DTOs;

public class WalletTransactionDto
{
    public Guid Id { get; set; }

    public Guid WalletId { get; set; }

    public decimal Amount { get; set; }

    public TransactionDirection Direction { get; set; }

    public Guid? TransferId { get; set; }

    public Guid? BankTransferId { get; set; }

    public string Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}