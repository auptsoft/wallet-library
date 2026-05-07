namespace Wallet.Contract.DTOs;

public class TransferRequest
{
    public Guid FromWalletId { get; set; }
    public Guid ToWalletId { get; set; }
    public decimal Amount { get; set; }
    public required string Reference { get; set; }
    public required string Narration { get; set; }
}
