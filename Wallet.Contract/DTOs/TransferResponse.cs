namespace Wallet.Contract.DTOs;

public class TransferResponse
{
    public bool IsSuccessfull { get; set; }
    public string Reference { get; set; }
    public string Message { get; set; }
    public string ErrorCode { get; set; }
}

public static class TransferErrorCodes
{
    public const string Success = "00";
    public const string InconsistentDuplicate = "01";
    public const string Error = "02";
    
    public const string ToWalletNotFound = "03";
    public const string FromWalletNotFound = "04";
    
    public const string ToWalletLocked = "05";
    public const string FromWalletLocked = "06";
}