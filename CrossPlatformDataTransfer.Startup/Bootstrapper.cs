using Microsoft.Extensions.DependencyInjection;
using CrossPlatformDataTransfer.Application;
using CrossPlatformDataTransfer.Infrastructure;
using CrossPlatformDataTransfer.Infrastructure.Security;
using CrossPlatformDataTransfer.Application.Transfer;

namespace CrossPlatformDataTransfer.Startup;

public static class Bootstrapper
{
    public static IServiceCollection AddAllProjectServices(this IServiceCollection services)
    {
        services.AddApplicationServices();
        services.AddInfrastructureServices();
        services.AddSecurityServices();
        
        // Register the advanced transfer engine
        services.AddSingleton<ChunkedTransferEngine>();
        
        return services;
    }
}
