namespace CrossPlatformDataTransfer.Core.Interfaces.Services;

public interface IAdbService
{
    Task<string> ExecuteAdbCommandAsync(string arguments);
    Task<IEnumerable<string>> GetConnectedDeviceSerialsAsync();
    Task<string> GetDeviceInfoAsync(string serial);
    Task<bool> PullFileAsync(string serial, string remotePath, string localPath);
    Task<bool> PushFileAsync(string serial, string localPath, string remotePath);
    Task<string> ExecuteRootCommandAsync(string serial, string command);
    Task<bool> IsDeviceRootedAsync(string serial);
}
