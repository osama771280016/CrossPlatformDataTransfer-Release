using Microsoft.Extensions.DependencyInjection;
using CrossPlatformDataTransfer.Core.Interfaces.Services;
using CrossPlatformDataTransfer.Core.Interfaces.Repositories;
using CrossPlatformDataTransfer.Infrastructure.Services;
using CrossPlatformDataTransfer.Infrastructure.Services.Android;
using CrossPlatformDataTransfer.Infrastructure.Repositories;

namespace CrossPlatformDataTransfer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IAdbService, AdbService>();
        services.AddSingleton<IDeviceDiscoveryService, AdbDeviceDiscoveryService>();
        services.AddSingleton<ITransferService, AndroidSmsTransferService>();
        services.AddSingleton<SmsNormalizationService>();
        services.AddSingleton<IAgentCommunicationService, AndroidAgentClient>();
        services.AddSingleton<ITcpAgentServer, TcpAgentServer>();
        services.AddSingleton<IDeviceRepository, MockDeviceRepository>();
        
        return services;
    }
}
