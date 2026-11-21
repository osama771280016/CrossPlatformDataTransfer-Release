using CrossPlatformDataTransfer.Core.Entities;
using CrossPlatformDataTransfer.Core.Enums;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Services.Android;

public class AdbDeviceDiscoveryService : IDeviceDiscoveryService
{
    private readonly IAdbService _adbService;
    private bool _isScanning;
    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<string, Device> _devicesBySerial = new(StringComparer.Ordinal);

    public event EventHandler<Device>? DeviceDiscovered;
    public event EventHandler<Guid>? DeviceDisconnected;
    public event EventHandler<(Guid DeviceId, ConnectionStatus Status)>? DeviceStatusChanged;

    public AdbDeviceDiscoveryService(IAdbService adbService)
    {
        _adbService = adbService;
    }

    public async Task StartDiscoveryAsync()
    {
        if (_isScanning) return;
        _isScanning = true;

        _ = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                await ScanDevicesAsync();
                await Task.Delay(2000, _cts.Token);
            }
        }, _cts.Token);

        await Task.CompletedTask;
    }

    public async Task StopDiscoveryAsync()
    {
        _cts.Cancel();
        _isScanning = false;
        await Task.CompletedTask;
    }

    private async Task ScanDevicesAsync()
    {
        try
        {
            var currentSerials = await _adbService.GetConnectedDeviceSerialsAsync();
            var currentSerialSet = new HashSet<string>(currentSerials, StringComparer.Ordinal);

            // Check for new devices
            foreach (var serial in currentSerialSet)
            {
                if (!_devicesBySerial.TryGetValue(serial, out var device))
                {
                    var model = await _adbService.GetDeviceInfoAsync(serial);
                    device = new Device
                    {
                        Id = Guid.NewGuid(),
                        SerialNumber = serial,
                        Name = $"{model.Trim()} ({serial})",
                        Type = DeviceType.Android,
                        ConnectionStatus = ConnectionStatus.Connected,
                        LastSeen = DateTime.UtcNow
                    };

                    _devicesBySerial.Add(serial, device);
                    DeviceDiscovered?.Invoke(this, device);
                }
                else
                {
                    device.LastSeen = DateTime.UtcNow;
                    if (device.ConnectionStatus != ConnectionStatus.Connected)
                    {
                        device.ConnectionStatus = ConnectionStatus.Connected;
                        DeviceStatusChanged?.Invoke(this, (device.Id, device.ConnectionStatus));
                    }
                }
            }

            // Check for disconnected devices
            foreach (var device in _devicesBySerial.Values)
            {
                if (device.ConnectionStatus == ConnectionStatus.Connected &&
                    !currentSerialSet.Contains(device.SerialNumber))
                {
                    device.ConnectionStatus = ConnectionStatus.Disconnected;
                    DeviceDisconnected?.Invoke(this, device.Id);
                    DeviceStatusChanged?.Invoke(this, (device.Id, device.ConnectionStatus));
                }
            }
        }
        catch (Exception)
        {
            // Error handling
        }
    }
}
