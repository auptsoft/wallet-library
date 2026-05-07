namespace Wallet.Contract.DTOs;

public class WalletDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string WalletName { get; set; } = string.Empty;
    public string WalletIdentifier { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "NGN";
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public bool IsPlatformWallet { get; set; }
    public bool AllowNegative { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
}