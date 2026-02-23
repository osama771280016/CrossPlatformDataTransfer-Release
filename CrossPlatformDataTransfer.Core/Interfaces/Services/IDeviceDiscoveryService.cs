using CrossPlatformDataTransfer.Core.Entities;
using CrossPlatformDataTransfer.Core.Enums;

namespace CrossPlatformDataTransfer.Core.Interfaces.Services;

public interface IDeviceDiscoveryService
{
    Task StartDiscoveryAsync();
    Task StopDiscoveryAsync();
    event EventHandler<Device> DeviceDiscovered;
    event EventHandler<Guid> DeviceDisconnected;
    event EventHandler<(Guid DeviceId, ConnectionStatus Status)> DeviceStatusChanged;
}
