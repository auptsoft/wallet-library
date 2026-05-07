

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Wallet.Contract.Abstractions;
using Wallet.Contract.DTOs;
using Wallet.Contract.Enums;
using Wallet.Core.Entities;
using Wallet.Core.Interfaces;

namespace Wallet.Core.Services;

public class WalletService (
    IUnitOfWork unitOfWork,
    IWalletRepository walletRepository,
    IWalletTransactionRepository walletTransactionRepository,
    IWalletTransferRepository walletTransferRepository,
    IIdempotencyRecordRepository idempotencyRecordRepository,
    ILogger<WalletService> logger
    ): IWalletService
{
    public async Task<WalletDto> CreateWallet(string userId, string currency, string walletName,  bool isPlatformWallet=false, bool allowNegative=false, string walletIdentifier="")
    {
        var payload = new Core.Entities.Wallet()
        {
            Balance = 0,
            Currency = currency,
            IsPlatformWallet = isPlatformWallet,
            UserId = userId,
            AllowNegative = allowNegative,
            WalletName =  walletName,
            WalletIdentifier = walletIdentifier,
            RowVersion = 1
        };
        
        logger.LogInformation($"Creating wallet: {JsonConvert.SerializeObject(payload)}");
        
        var wallet = await walletRepository.CreateWallet(payload);
        return GetWalletDto(wallet);
    }
    
    public async Task<WalletDto?> GetWallet(Guid walletId)
    {
        var wallet = await walletRepository.GetWallet(walletId);
        return wallet == null ? null : GetWalletDto(wallet);
    }
    
    public async Task<TransferResponse> WalletTransfer(TransferRequest request)
    {
        await unitOfWork.BeginTransaction();
        try
        {
            var idempotencyRecord = await idempotencyRecordRepository.GetIdempotencyRecordByKey(request.Reference);
            if (idempotencyRecord != null)
            {
                logger.LogInformation("IdempotencyRecord already exists");
                var requestHash = IdempotencyRecord.ComputeHash(request);
                if (idempotencyRecord.RequestHash != requestHash)
                {
                    logger.LogInformation("IdempotencyRecord doesn't match");
                    return new TransferResponse()
                    {
                        Message = "Inconsistent duplicate request",
                        IsSuccessfull = false,
                        ErrorCode = TransferErrorCodes.InconsistentDuplicate,
                    };
                }
                
                logger.LogInformation("IdempotencyRecord found. operation ignored");
                return new TransferResponse()
                {
                    Message = "Duplicate request. Ignored.",
                    IsSuccessfull = true,
                    ErrorCode = TransferErrorCodes.Success,
                };
            }
            
            var toWallet = await walletRepository.GetWallet(request.ToWalletId);
            var fromWallet = await walletRepository.GetWallet(request.FromWalletId);

            if (toWallet == null)
            {
                logger.LogInformation("ToWallet not found");
                return new TransferResponse()
                {
                    Message = "ToWallet not found",
                    IsSuccessfull = false,
                    ErrorCode = TransferErrorCodes.ToWalletNotFound,
                };
            }
            if (fromWallet == null)
            {
                logger.LogInformation("FromWallet not found");
                return new TransferResponse()
                {
                    Message = "FromWallet not found",
                    IsSuccessfull = false,
                    ErrorCode = TransferErrorCodes.FromWalletNotFound,
                };
            }

            if (toWallet.IsLocked)
            {
                logger.LogInformation("ToWallet is locked");
                return new TransferResponse()
                {
                    Message = "ToWallet is locked",
                    IsSuccessfull = false,
                    ErrorCode = TransferErrorCodes.ToWalletLocked,
                };
            }

            if (fromWallet.IsLocked)
            {
                logger.LogInformation("FromWallet is locked");
                return new TransferResponse()
                {
                    Message = "FromWallet is locked",
                    IsSuccessfull = false,
                    ErrorCode = TransferErrorCodes.ToWalletLocked,
                };
            }
            
            toWallet.Balance += request.Amount;
            toWallet.IncrementRowVersion();
            
            fromWallet.Balance -= request.Amount;
            fromWallet.IncrementRowVersion();

            var walletTransferreference = request.Reference; // Guid.NewGuid().ToString();

            var creditWalletTransaction = new WalletTransaction()
            {
                WalletId = request.ToWalletId,
                Amount = request.Amount,
                Direction = TransactionDirection.Credit,
                Description = $"credit|{request.Narration}",
                WalletTransferReference = walletTransferreference,
            };

            var debitWalletTransaction = new WalletTransaction()
            {
                WalletId = request.FromWalletId,
                Amount = request.Amount,
                Direction = TransactionDirection.Debit,
                Description = $"debit|{request.Narration}",
                WalletTransferReference = walletTransferreference,
            };
            
            walletTransactionRepository.Add(creditWalletTransaction);
            walletTransactionRepository.Add(debitWalletTransaction);

            walletTransferRepository.Add(new WalletTransfer()
            {
                FromWalletId = request.FromWalletId,
                ToWalletId = request.ToWalletId,
                Amount = request.Amount,
                Reference = walletTransferreference,
                Status =  TransferStatus.Completed,
                Narration = request.Narration,
            });
            
            AddIdempotencyRecord(request);
            
            await unitOfWork.CommitTransaction();

            return new TransferResponse()
            {
                ErrorCode = TransferErrorCodes.Success,
                Message = "Success",
                Reference =  walletTransferreference,
                IsSuccessfull = true,
            };
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransaction();
            logger.LogError(ex, "An error occured");
            
            return new TransferResponse()
            {
                Message = ex.Message,
                IsSuccessfull = false,
                ErrorCode = TransferErrorCodes.Error,
            };
        }
    }

    public async Task<WalletDto?> GetWalletByIdentifier(string identifier)
    {
        var wallet = await walletRepository.GetWalletByIdentifier(identifier);
        return GetWalletDto(wallet);
    }

    public async Task<WalletDto?> GetWalletByName(string walletName)
    {
        var wallet = await walletRepository.GetWalletByName(walletName);
        return GetWalletDto(wallet);
    }

    private void AddIdempotencyRecord(TransferRequest request)
    {
        var requestHash = IdempotencyRecord.ComputeHash(request);
        var requestPath = "Wallet.Transfer";
        
        idempotencyRecordRepository.CreateIdempotencyRecord(new IdempotencyRecord()
        {
            IdempotencyKey =  request.Reference,
            RequestPath = requestPath,
            RequestHash = requestHash,
        });
    }

    private WalletDto? GetWalletDto(Entities.Wallet? wallet)
    {
        if (wallet == null) return null;
        return new WalletDto()
        {
            Id = wallet.Id,
            WalletName = wallet.WalletName,
            WalletIdentifier = wallet.WalletIdentifier,
            AllowNegative = wallet.AllowNegative,
            Balance = wallet.Balance,
            Currency = wallet.Currency,
            IsLocked = wallet.IsLocked,
            IsPlatformWallet = wallet.IsPlatformWallet,
            LockedAt = wallet.LockedAt,
            UserId = wallet.UserId,
            CreatedAt =  wallet.CreatedAt,
            UpdatedAt =   wallet.UpdatedAt,
        };
    }
}