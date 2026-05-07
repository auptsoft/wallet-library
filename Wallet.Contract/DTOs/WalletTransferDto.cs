using Wallet.Contract.Enums;

namespace Wallet.Contract.DTOs;

public class WalletTransferDto
{
    public Guid Id { get; set; }

    public Guid FromWalletId { get; set; }

    public Guid ToWalletId { get; set; }

    public decimal Amount { get; set; }

    public TransferStatus Status { get; set; }

    public string Reference { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}