# Wallet

A .NET 8 library for managing digital wallets, fund transfers, and transaction history. Built around a clean three-layer architecture — contract, core, and infrastructure — so you can consume just the layers you need.

## Solution Structure

```
Wallet/
├── Wallet.Contract       # DTOs, enums, and service abstractions (no dependencies)
├── Wallet.Core           # Domain entities, repository interfaces, business logic
└── Wallet.Infrastructure # EF Core DbContext, repository implementations, DI wiring
```

**Dependency direction:** `Wallet.Infrastructure` → `Wallet.Core` → `Wallet.Contract`

---

## Projects

### Wallet.Contract

Shared contracts with no external dependencies beyond Microsoft DI/Configuration abstractions. Reference this package when you need types without pulling in EF Core or business logic.

**Contents:**
- `IWalletService` / `IWalletQueryService` — service interfaces
- `WalletDto`, `WalletTransactionDto`, `WalletTransferDto` — read models
- `TransferRequest` / `TransferResponse` — transfer input/output
- Enums: `WalletType`, `TransactionDirection`, `TransferStatus`, `TransactionType`
- `TransferErrorCodes` — typed error code constants

### Wallet.Core

Domain logic and repository interfaces. Depends on `Wallet.Contract`.

**Contents:**
- Entities: `Wallet`, `WalletTransaction`, `WalletTransfer`, `IdempotencyRecord`
- Repository interfaces: `IWalletRepository`, `IWalletTransactionRepository`, `IWalletTransferRepository`, `IIdempotencyRecordRepository`, `IUnitOfWork`
- `WalletService` — wallet creation, lookups, and atomic idempotent fund transfers
- `WalletQueryService` — paginated queries for transactions and transfers
- `AddWalletServices()` — DI extension method

### Wallet.Infrastructure

EF Core + PostgreSQL implementation. Depends on `Wallet.Core`.

**Contents:**
- `WalletDbContext` — maps entities to `wallets`, `wallet_transactions`, `wallet_transfers`, `idempotency_records`
- Concrete repository implementations for all four entities
- `UnitOfWork` — wraps EF Core transactions with begin/commit/rollback
- `AddWalletInfrastructure()` — DI extension method

---

## Requirements

- .NET 8.0
- PostgreSQL

---

## Installation

### From NuGet

```bash
dotnet add package Wallet.Infrastructure   # includes Wallet.Core and Wallet.Contract
dotnet add package Wallet.Core             # includes Wallet.Contract only
dotnet add package Wallet.Contract         # standalone contracts only
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

In `Program.cs`, configure the DbContext and call both extension methods:

```csharp
using Microsoft.EntityFrameworkCore;
using Wallet.Core;
using Wallet.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Wallet");

builder.Services.AddDbContext<WalletDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddWalletServices();       // IWalletService, IWalletQueryService
builder.Services.AddWalletInfrastructure(); // IUnitOfWork, all repositories
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
    public async Task<IActionResult> Create([FromBody] CreateWalletRequest request)
    {
        var wallet = await walletService.CreateWallet(
            userId: request.UserId,
            currency: "NGN",
            walletName: request.WalletName,
            walletIdentifier: request.Identifier,
            isPlatformWallet: false,
            allowNegative: false
        );

        return Ok(wallet); // returns WalletDto
    }
}
```

### Look Up a Wallet

```csharp
WalletDto? wallet = await walletService.GetWallet(walletId);
WalletDto? wallet = await walletService.GetWalletByIdentifier("user-acc-001");
WalletDto? wallet = await walletService.GetWalletByName("John's Savings");
```

### Transfer Funds

Transfers are atomic and idempotent. `Reference` is the idempotency key — replaying the same reference with the same payload is a safe no-op; replaying it with a different payload returns `InconsistentDuplicate`.

```csharp
public class TransferController(IWalletService walletService) : ControllerBase
{
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        var result = await walletService.WalletTransfer(new TransferRequest
        {
            FromWalletId = request.FromWalletId,
            ToWalletId   = request.ToWalletId,
            Amount        = 500.00m,
            Reference     = Guid.NewGuid().ToString(), // your idempotency key
            Narration     = "Payment for invoice #1234"
        });

        if (!result.IsSuccessfull)
            return BadRequest(new { result.ErrorCode, result.Message });

        return Ok(result); // TransferResponse with Reference
    }
}
```

**Transfer error codes (`TransferErrorCodes`):**

| Code | Constant | Meaning |
|---|---|---|
| `00` | `Success` | Transfer completed |
| `01` | `InconsistentDuplicate` | Same reference, different payload |
| `02` | `Error` | Unexpected server error |
| `03` | `ToWalletNotFound` | Destination wallet does not exist |
| `04` | `FromWalletNotFound` | Source wallet does not exist |
| `05` | `ToWalletLocked` | Destination wallet is locked |
| `06` | `FromWalletLocked` | Source wallet is locked |

### Query Transactions

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
        var (items, total, pageSize, currentPage) =
            await queryService.GetWalletTransactions(walletId, startDate, endDate, page, perPage);

        return Ok(new { data = items, total, page = currentPage, perPage = pageSize });
    }
}
```

### Query Transfers

```csharp
// Paginated list
var (transfers, total, perPage, page) = await queryService.GetWalletTransfers(
    toWalletId: null, fromWalletId: myWalletId,
    startDate: null, endDate: null,
    page: 1, perPage: 20);

// By reference
WalletTransferDto? transfer = await queryService.GetWalletTransferByReference("REF-001");

// Transactions belonging to a specific transfer
List<WalletTransactionDto?> txns =
    await queryService.GetWalletTransactionsByTransferReference("REF-001");
```

### Unit of Work (advanced)

Use `IUnitOfWork` directly when you need to coordinate multiple repository writes in a single transaction outside of `WalletService`:

```csharp
public class CustomService(
    IUnitOfWork unitOfWork,
    IWalletRepository walletRepository,
    IWalletTransactionRepository transactionRepository)
{
    public async Task Execute(CancellationToken ct = default)
    {
        await unitOfWork.BeginTransaction(ct);
        try
        {
            var wallet = await walletRepository.GetWallet(someId);
            wallet!.Balance += 100;
            transactionRepository.Add(new WalletTransaction { ... });

            await unitOfWork.CommitTransaction(ct); // SaveChanges + commit
        }
        catch
        {
            await unitOfWork.RollbackTransaction(ct);
            throw;
        }
    }
}
```

> `CommitTransaction` calls `SaveChanges` internally — no separate call needed. If it throws, call `RollbackTransaction` to clean up the transaction handle.

---

## Domain Model

```
Wallet
 ├── Id (Guid, PK)
 ├── UserId (string, indexed)
 ├── WalletIdentifier (string, unique)
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
 ├── ToWalletId   → Wallet
 ├── Amount (decimal 18,2)
 ├── Reference (string, unique, max 100)
 ├── Status (Pending | Completed | Failed)
 └── Narration (string?)

WalletTransaction
 ├── Id (Guid, PK)
 ├── WalletId   → Wallet (indexed)
 ├── TransferId → WalletTransfer? (indexed)
 ├── Amount (decimal 18,2)
 ├── Direction (Debit | Credit)
 ├── Description (string)
 └── WalletTransferReference (string)

IdempotencyRecord
 ├── Id (Guid, PK)
 ├── IdempotencyKey (string, unique, max 200)
 ├── RequestPath (string)
 └── RequestHash (SHA-256 of canonical request payload)
```

---

## Configuration Notes

- **Legacy timestamp behaviour** — `Npgsql.EnableLegacyTimestampBehavior` is enabled automatically by `AddWalletInfrastructure()`, required for `DateTime` columns stored as `timestamp without time zone`.
- **Optimistic concurrency** — the `RowVersion` column on `Wallet` is a concurrency token. EF Core throws `DbUpdateConcurrencyException` on collisions; handle this in your service layer.
- **Redis** — `Microsoft.Extensions.Caching.StackExchangeRedis` is included in `Wallet.Infrastructure`. Register it separately with `services.AddStackExchangeRedisCache(...)` when needed.

---

## Package Dependencies

| Package | Version | Used in |
|---|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.8 | Infrastructure |
| `Microsoft.EntityFrameworkCore` | 8.0.8 | Infrastructure |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | 9.0.10 | Infrastructure |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.21 | Infrastructure |
| `BCrypt.Net-Next` | 4.0.3 | Infrastructure |
| `CloudinaryDotNet` | 1.27.8 | Infrastructure |
| `MailKit` | 4.14.1 | Infrastructure |
| `Newtonsoft.Json` | 13.0.3 | Core |
| `Microsoft.Extensions.Logging.Abstractions` | 9.0.9 | Core |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 9.0.9 | Contract |
| `Microsoft.Extensions.Configuration.Abstractions` | 9.0.9 | Contract |

---

## Author

Andrew Oshodin