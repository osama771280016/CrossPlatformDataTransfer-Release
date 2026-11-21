namespace CrossPlatformDataTransfer.Core.Interfaces.Services;

/// <summary>
/// Narrow ADB operations used by the application. Arbitrary shell commands,
/// direct device file access, and root commands are intentionally unavailable.
/// </summary>
public interface IAdbService
{
    Task<IEnumerable<string>> GetConnectedDeviceSerialsAsync();
    Task<string> GetDeviceInfoAsync(string serial);
    Task ForwardPortAsync(string serial, int localPort, int remotePort);
    Task RemoveForwardAsync(string serial, int localPort);
}
