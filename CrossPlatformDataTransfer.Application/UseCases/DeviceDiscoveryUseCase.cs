using CrossPlatformDataTransfer.Application.DTOs;
using CrossPlatformDataTransfer.Application.Interfaces;
using CrossPlatformDataTransfer.Core.Interfaces.Services;
using CrossPlatformDataTransfer.Core.Interfaces.Repositories;
using CrossPlatformDataTransfer.Core.Enums;

namespace CrossPlatformDataTransfer.Application.UseCases;

public class DeviceDiscoveryUseCase : IDeviceDiscoveryUseCase
{
    private readonly IDeviceDiscoveryService _discoveryService;
    private readonly IDeviceRepository _deviceRepository;

    public event EventHandler<DeviceDto>? DeviceDiscovered;
    public event EventHandler<Guid>? DeviceDisconnected;
    public event EventHandler<(Guid DeviceId, ConnectionStatus Status)>? DeviceStatusChanged;

    public DeviceDiscoveryUseCase(IDeviceDiscoveryService discoveryService, IDeviceRepository deviceRepository)
    {
        _discoveryService = discoveryService;
        _deviceRepository = deviceRepository;
        
        _discoveryService.DeviceDiscovered += OnDeviceDiscovered;
        _discoveryService.DeviceDisconnected += (_, deviceId) => DeviceDisconnected?.Invoke(this, deviceId);
        _discoveryService.DeviceStatusChanged += (_, change) => DeviceStatusChanged?.Invoke(this, change);
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
            SerialNumber = device.SerialNumber,
            IpAddress = device.IpAddress,
            Type = device.Type.ToString(),
            ConnectionStatus = device.ConnectionStatus.ToString(),
            LastSeen = device.LastSeen
        };

        DeviceDiscovered?.Invoke(this, dto);
    }
}
