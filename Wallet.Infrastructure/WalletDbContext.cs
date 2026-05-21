using Microsoft.EntityFrameworkCore;
using Wallet.Core.Entities;

namespace Wallet.Infrastructure;

public class WalletDbContext : DbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options)
        : base(options)
    {
    }

    public DbSet<Core.Entities.Wallet> Wallets { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }
    public DbSet<WalletTransfer> WalletTransfers { get; set; }
    // public DbSet<BankTransfer> BankTransfers { get; set; }
    public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseModel>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureWallet(modelBuilder);
        ConfigureWalletTransaction(modelBuilder);
        ConfigureWalletTransfer(modelBuilder);
        ConfigureIdempotency(modelBuilder);
    }
    
    
    private void ConfigureWallet(ModelBuilder builder)
    {
        builder.Entity<Core.Entities.Wallet>(entity =>
        {
            entity.ToTable(Core.Entities.Wallet.GetEntityName());
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Balance)
                .HasPrecision(18,2);
            
            entity.HasIndex(x => x.WalletIdentifier).IsUnique();

            entity.Property(x => x.RowVersion)
                .IsConcurrencyToken();
            
            entity.HasIndex(x => x.UserId);
        });
    }
    
    private void ConfigureWalletTransaction(ModelBuilder builder)
    {
        builder.Entity<WalletTransaction>(entity =>
        {
            entity.ToTable(WalletTransaction.GetEntityName());
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount)
                .HasPrecision(18,2);

            // entity.Property(x => x.Reference)
            //     .HasMaxLength(100);

            entity.HasIndex(x => x.WalletId);
            entity.HasIndex(x => x.TransferId);

            entity.HasOne(x => x.Wallet)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.WalletId);
        });
    }
    
    private void ConfigureWalletTransfer(ModelBuilder builder)
    {
        builder.Entity<WalletTransfer>(entity =>
        {
            entity.ToTable(WalletTransfer.GetEntityName());
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount)
                .HasPrecision(18,2);

            entity.Property(x => x.Reference)
                .HasMaxLength(100);

            entity.HasIndex(x => x.Reference)
                .IsUnique();

            entity.HasOne(x => x.FromWallet)
                .WithMany()
                .HasForeignKey(x => x.FromWalletId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ToWallet)
                .WithMany()
                .HasForeignKey(x => x.ToWalletId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
    
    private void ConfigureIdempotency(ModelBuilder builder)
    {
        builder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable(IdempotencyRecord.GetEntityName());

            entity.HasKey(x => x.Id);

            entity.Property(x => x.IdempotencyKey)
                .HasMaxLength(200);

            entity.HasIndex(x => x.IdempotencyKey)
                .IsUnique();
        });
    }
}