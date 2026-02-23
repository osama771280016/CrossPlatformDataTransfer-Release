using Microsoft.Extensions.DependencyInjection;
using CrossPlatformDataTransfer.Application.Interfaces;
using CrossPlatformDataTransfer.Application.UseCases;

namespace CrossPlatformDataTransfer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IDeviceDiscoveryUseCase, DeviceDiscoveryUseCase>();
        services.AddScoped<ISecureTransferUseCase, SecureTransferUseCase>();
        services.AddScoped<SmsTransferUseCase>();
        services.AddScoped<AgentBasedTransferUseCase>();
        
        return services;
    }
}
