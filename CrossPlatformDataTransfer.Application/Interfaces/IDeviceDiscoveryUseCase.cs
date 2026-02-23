using CrossPlatformDataTransfer.Application.DTOs;

namespace CrossPlatformDataTransfer.Application.Interfaces;

public interface IDeviceDiscoveryUseCase
{
    Task ExecuteAsync();
    event EventHandler<DeviceDto> DeviceDiscovered;
}
