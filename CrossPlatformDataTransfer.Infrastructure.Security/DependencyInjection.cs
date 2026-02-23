using Microsoft.Extensions.DependencyInjection;
using CrossPlatformDataTransfer.Core.Interfaces.Services;
using CrossPlatformDataTransfer.Infrastructure.Security.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Security;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();
        services.AddSingleton<IKeyManagementService, KeyManagementService>();
        services.AddSingleton<IHashService, Sha256HashService>();
        
        // Temporarily commented out until implementations are updated to match new Core interfaces
        // services.AddTransient<IKeyExchangeService, DiffieHellmanKeyExchangeService>();
        // services.AddSingleton<IPairingService, SecurePairingService>();
        
        return services;
    }
}
