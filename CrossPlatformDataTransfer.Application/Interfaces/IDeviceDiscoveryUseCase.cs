using CrossPlatformDataTransfer.Application.DTOs;
using CrossPlatformDataTransfer.Core.Enums;

namespace CrossPlatformDataTransfer.Application.Interfaces;

public interface IDeviceDiscoveryUseCase
{
    Task ExecuteAsync();
    event EventHandler<DeviceDto> DeviceDiscovered;
    event EventHandler<Guid> DeviceDisconnected;
    event EventHandler<(Guid DeviceId, ConnectionStatus Status)> DeviceStatusChanged;
}
