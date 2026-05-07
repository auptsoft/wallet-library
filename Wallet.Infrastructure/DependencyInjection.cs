using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Core.Interfaces;
using Wallet.Infrastructure.Repositories;

namespace Wallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWalletInfrastructure(this IServiceCollection services)
        {
            // services.AddDbContext<WalletDbContext>(options =>
            // {
            //     options.UseNpgsql(connectionString);
            // });

            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<IWalletTransferRepository, WalletTransferRepository>();
            services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
            services.AddScoped<IIdempotencyRecordRepository, IdempotencyRecordRepository>();
            
            return services;
        }
}