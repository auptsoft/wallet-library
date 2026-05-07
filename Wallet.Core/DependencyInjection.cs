using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Contract.Abstractions;
using Wallet.Core.Services;
using WalletModule.Services;

namespace Wallet.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddWalletServices(this IServiceCollection services)
    {
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IWalletQueryService, WalletQueryService>();
        
        return services;
    }
}