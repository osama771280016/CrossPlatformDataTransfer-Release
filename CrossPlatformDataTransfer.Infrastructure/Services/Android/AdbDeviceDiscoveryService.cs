using CrossPlatformDataTransfer.Core.Entities;
using CrossPlatformDataTransfer.Core.Enums;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Services.Android;

public class AdbDeviceDiscoveryService : IDeviceDiscoveryService
{
    private readonly IAdbService _adbService;
    private bool _isScanning;
    private readonly CancellationTokenSource _cts = new();
    private readonly HashSet<string> _detectedSerials = new();

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
            var currentSerialList = currentSerials.ToList();

            // Check for new devices
            foreach (var serial in currentSerialList)
            {
                if (!_detectedSerials.Contains(serial))
                {
                    var model = await _adbService.GetDeviceInfoAsync(serial);
                    var device = new Device
                    {
                        Id = Guid.NewGuid(), // Mapping serial to Guid would happen in a repository
                        Name = $"{model.Trim()} ({serial})",
                        Type = DeviceType.Android,
                        ConnectionStatus = ConnectionStatus.Connected,
                        LastSeen = DateTime.Now
                    };

                    _detectedSerials.Add(serial);
                    DeviceDiscovered?.Invoke(this, device);
                }
            }

            // Check for disconnected devices
            // Note: In a real implementation, we would need a way to map the serial back to the original Guid
            // For now, we focus on the structure.
        }
        catch (Exception)
        {
            // Error handling
        }
    }
}
