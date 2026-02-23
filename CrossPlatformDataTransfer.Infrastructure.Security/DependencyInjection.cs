using Microsoft.Extensions.DependencyInjection;
using CrossPlatformDataTransfer.Core.Interfaces.Services;
using CrossPlatformDataTransfer.Infrastructure.Security.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Security;

/// <summary>
/// Dependency injection registration for the Security infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds security-related services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        services.AddSingleton<IEncryptionService, HardenedEncryptionService>();
        services.AddSingleton<IKeyManagementService, KeyManagementService>();
        services.AddSingleton<IHashService, Sha256HashService>();
        services.AddHttpClient<ILicenseService, LicenseService>();
        
        // Registering previously commented out services
        services.AddTransient<IKeyExchangeService, DiffieHellmanKeyExchangeService>();
        services.AddSingleton<IPairingService, SecurePairingService>();
        
        return services;
    }
}
