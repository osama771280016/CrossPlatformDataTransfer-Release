using CrossPlatformDataTransfer.Application.DTOs;
using CrossPlatformDataTransfer.Application.Interfaces;
using CrossPlatformDataTransfer.Core.Interfaces.Services;
using CrossPlatformDataTransfer.Core.Interfaces.Repositories;

namespace CrossPlatformDataTransfer.Application.UseCases;

public class DeviceDiscoveryUseCase : IDeviceDiscoveryUseCase
{
    private readonly IDeviceDiscoveryService _discoveryService;
    private readonly IDeviceRepository _deviceRepository;

    public event EventHandler<DeviceDto>? DeviceDiscovered;

    public DeviceDiscoveryUseCase(IDeviceDiscoveryService discoveryService, IDeviceRepository deviceRepository)
    {
        _discoveryService = discoveryService;
        _deviceRepository = deviceRepository;
        
        _discoveryService.DeviceDiscovered += OnDeviceDiscovered;
    }

    public async Task ExecuteAsync()
    {
        await _discoveryService.StartDiscoveryAsync();
    }

    private void OnDeviceDiscovered(object? sender, Core.Entities.Device device)
    {
        // Logic to update repository could go here
        // _deviceRepository.AddAsync(device);

        var dto = new DeviceDto
        {
            Id = device.Id,
            Name = device.Name,
            IpAddress = device.IpAddress,
            Type = device.Type.ToString(),
            ConnectionStatus = device.ConnectionStatus.ToString(),
            LastSeen = device.LastSeen
        };

        DeviceDiscovered?.Invoke(this, dto);
    }
}
