namespace Wallet.Contract.Enums;

public enum TransactionType
{
    Transfer,
    Deposit,
    Withdrawal,
    Fee,
    Adjustment
}

public enum TransactionDirection
{
    Debit,
    Credit
}

public enum TransferStatus
{
    Pending,
    Completed,
    Failed
}

public enum WalletType
{
    User,
    Merchant,
    Platform,
    BankSettlement,
    Fees
}