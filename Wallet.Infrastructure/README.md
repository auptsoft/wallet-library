# Wallet.Infrastructure

The infrastructure layer of the Wallet system, providing EF Core data access, repository implementations, and a unit-of-work for PostgreSQL-backed wallet operations.

## Overview

`Wallet.Infrastructure` is a .NET 8 class library that wires up:

- **`WalletDbContext`** — EF Core DbContext with PostgreSQL (Npgsql) backing four tables: `wallets`, `wallet_transactions`, `wallet_transfers`, `idempotency_records`
- **Repositories** — concrete implementations of the interfaces defined in `Wallet.Core`
- **`UnitOfWork`** — wraps EF Core transactions with begin/commit/rollback lifecycle
- **`DependencyInjection`** — a single extension method that registers everything into `IServiceCollection`

The project is part of a three-layer solution:

| Project | Role |
|---|---|
| `Wallet.Contract` | DTOs, enums, error codes |
| `Wallet.Core` | Domain entities, repository interfaces, business logic services |
| `Wallet.Infrastructure` | EF Core DbContext, repository implementations, DI wiring |

---

## Requirements

- .NET 8.0
- PostgreSQL
- (Optional) Redis — for distributed caching via `Microsoft.Extensions.Caching.StackExchangeRedis`

---

## Installation

### From NuGet

```bash
dotnet add package Wallet.Infrastructure
dotnet add package Wallet.Core
```

### From source

```bash
git clone <repo-url>
cd Wallet
dotnet build
```

---

## Getting Started

### 1. Register Services

In `Program.cs` (or your composition root), call both layer extension methods and configure the DbContext:

```csharp
using Microsoft.EntityFrameworkCore;
using Wallet.Core;
using Wallet.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Wallet");

builder.Services.AddDbContext<WalletDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddWalletServices();       // registers IWalletService, IWalletQueryService
builder.Services.AddWalletInfrastructure(); // registers IUnitOfWork, all repositories
```

### 2. Run Migrations

```bash
dotnet ef migrations add InitialCreate --project Wallet.Infrastructure --startup-project YourApi
dotnet ef database update --project Wallet.Infrastructure --startup-project YourApi
```

---

## Typical Usage

### Create a Wallet

```csharp
public class WalletController(IWalletService walletService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest request)
    {
        var wallet = await walletService.CreateWallet(
            userId: request.UserId,
            currency: "NGN",
            walletName: request.WalletName,
            walletIdentifier: request.Identifier
        );

        return Ok(wallet);
    }
}
```

`CreateWallet` returns a `WalletDto`:

```csharp
public class WalletDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string WalletName { get; set; }
    public string WalletIdentifier { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; }
    public bool IsLocked { get; set; }
    public bool IsPlatformWallet { get; set; }
    public bool AllowNegative { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

### Transfer Funds Between Wallets

Transfers are atomic and idempotent. The `Reference` field acts as the idempotency key — submitting the same reference twice with identical parameters is a safe no-op; submitting the same reference with different parameters returns an `InconsistentDuplicate` error.

```csharp
public class TransferController(IWalletService walletService) : ControllerBase
{
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        var result = await walletService.WalletTransfer(new TransferRequest
        {
            FromWalletId = request.FromWalletId,
            ToWalletId = request.ToWalletId,
            Amount = 500.00m,
            Reference = Guid.NewGuid().ToString(), // your unique idempotency key
            Narration = "Payment for invoice #1234"
        });

        if (!result.IsSuccessfull)
            return BadRequest(result.Message);

        return Ok(result);
    }
}
```

`TransferResponse` shape:

```csharp
public class TransferResponse
{
    public bool IsSuccessfull { get; set; }
    public string Reference { get; set; }
    public string Message { get; set; }
    public string ErrorCode { get; set; }  // see TransferErrorCodes below
}
```

**Error Codes (`TransferErrorCodes`):**

| Code | Constant | Meaning |
|---|---|---|
| `00` | `Success` | Transfer completed |
| `01` | `InconsistentDuplicate` | Same reference, different payload |
| `02` | `Error` | Unexpected server error |
| `03` | `ToWalletNotFound` | Destination wallet missing |
| `04` | `FromWalletNotFound` | Source wallet missing |
| `05` | `ToWalletLocked` | Destination wallet is locked |
| `06` | `FromWalletLocked` | Source wallet is locked |

---

### Look Up a Wallet

```csharp
// By ID
WalletDto? wallet = await walletService.GetWallet(walletId);

// By unique identifier string
WalletDto? wallet = await walletService.GetWalletByIdentifier("user-acc-001");

// By name
WalletDto? wallet = await walletService.GetWalletByName("John's Savings");
```

---

### Query Transactions (via `IWalletQueryService`)

```csharp
public class TransactionController(IWalletQueryService queryService) : ControllerBase
{
    [HttpGet("{walletId}/transactions")]
    public async Task<IActionResult> GetTransactions(
        Guid walletId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20)
    {
        var (transactions, total, pageSize, currentPage) =
            await queryService.GetWalletTransactions(walletId, startDate, endDate, page, perPage);

        return Ok(new
        {
            data = transactions,
            total,
            page = currentPage,
            perPage = pageSize
        });
    }
}
```

---

### Using the Unit of Work Directly

If you need to coordinate multiple repository writes in a single transaction outside of `WalletService`:

```csharp
public class CustomOperationService(
    IUnitOfWork unitOfWork,
    IWalletRepository walletRepository,
    IWalletTransactionRepository transactionRepository)
{
    public async Task ExecuteCustomOperation(CancellationToken ct = default)
    {
        await unitOfWork.BeginTransaction(ct);
        try
        {
            var wallet = await walletRepository.GetWallet(someId);
            wallet!.Balance += 100;

            transactionRepository.Add(new WalletTransaction { ... });

            await unitOfWork.CommitTransaction(ct); // saves + commits
        }
        catch
        {
            await unitOfWork.RollbackTransaction(ct);
            throw;
        }
    }
}
```

> `CommitTransaction` calls `SaveChanges` internally before committing, so you do not need a separate `SaveChanges` call. A rollback is triggered automatically on exception inside `CommitTransaction`.

---

## Domain Model

```
Wallet
 ├── Id (Guid, PK)
 ├── UserId (string, indexed)
 ├── WalletIdentifier (string, unique index)
 ├── WalletName (string)
 ├── Balance (decimal 18,2)
 ├── Currency (string)
 ├── WalletType (User | Merchant | Platform | BankSettlement | Fees)
 ├── IsLocked / LockedAt
 ├── IsPlatformWallet / AllowNegative
 ├── RowVersion (optimistic concurrency token)
 └── Transactions → WalletTransaction[]

WalletTransfer
 ├── Id (Guid, PK)
 ├── FromWalletId → Wallet
 ├── ToWalletId → Wallet
 ├── Amount (decimal 18,2)
 ├── Reference (string, unique index, max 100)
 ├── Status (Pending | Completed | Failed)
 └── Narration (string?)

WalletTransaction
 ├── Id (Guid, PK)
 ├── WalletId → Wallet (indexed)
 ├── TransferId → WalletTransfer? (indexed)
 ├── Amount (decimal 18,2)
 ├── Direction (Debit | Credit)
 ├── Description (string)
 └── WalletTransferReference (string)

IdempotencyRecord
 ├── Id (Guid, PK)
 ├── IdempotencyKey (string, unique index, max 200)
 ├── RequestPath (string)
 └── RequestHash (string — SHA-256 of canonical request payload)
```

---

## Configuration Notes

- **Legacy timestamp behaviour** — `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` is set by `AddWalletInfrastructure`. This is required for `DateTime` columns mapped as `timestamp without time zone` in older Npgsql conventions.
- **Optimistic concurrency** — the `RowVersion` column on `Wallet` is a concurrency token. EF Core will throw `DbUpdateConcurrencyException` if two concurrent updates collide. Handle this at the service layer when building on top of the repository directly.
- **Redis** — `Microsoft.Extensions.Caching.StackExchangeRedis` is included as a dependency. Configure it separately with `services.AddStackExchangeRedisCache(...)` when needed.

---

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.8 | PostgreSQL EF Core provider |
| `Microsoft.EntityFrameworkCore` | 8.0.8 | ORM |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | 9.0.10 | Distributed cache |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.21 | JWT auth middleware |
| `BCrypt.Net-Next` | 4.0.3 | Password hashing |
| `CloudinaryDotNet` | 1.27.8 | File/image storage |
| `MailKit` | 4.14.1 | Email sending |

---

## License

Author: Andrew Oshodin