using System.Diagnostics;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Services.Android;

public class AdbService : IAdbService
{
    public async Task<string> ExecuteAdbCommandAsync(string arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "adb",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Some commands might return non-zero but still have useful output (like 'devices' when empty)
        // We handle errors based on content if needed, but return output as primary
        return string.IsNullOrWhiteSpace(output) ? error : output;
    }

    public async Task<IEnumerable<string>> GetConnectedDeviceSerialsAsync()
    {
        var output = await ExecuteAdbCommandAsync("devices");
        var serials = new List<string>();
        var lines = output.Split(new[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("List of devices") || string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1 && parts[1].Trim() == "device")
            {
                serials.Add(parts[0].Trim());
            }
        }

        return serials;
    }

    public async Task<string> GetDeviceInfoAsync(string serial)
    {
        return await ExecuteAdbCommandAsync($"-s {serial} shell getprop ro.product.model");
    }

    public async Task<bool> PullFileAsync(string serial, string remotePath, string localPath)
    {
        var result = await ExecuteAdbCommandAsync($"-s {serial} pull \"{remotePath}\" \"{localPath}\"");
        return result.Contains("pulled") || result.Contains("1 file pulled");
    }

    public async Task<bool> PushFileAsync(string serial, string localPath, string remotePath)
    {
        var result = await ExecuteAdbCommandAsync($"-s {serial} push \"{localPath}\" \"{remotePath}\"");
        return result.Contains("pushed") || result.Contains("1 file pushed");
    }

    public async Task<string> ExecuteRootCommandAsync(string serial, string command)
    {
        // Requires 'su' to be available on the device
        // Note: This is a blocking shell command
        return await ExecuteAdbCommandAsync($"-s {serial} shell \"su -c '{command}'\"");
    }

    public async Task<bool> IsDeviceRootedAsync(string serial)
    {
        var result = await ExecuteAdbCommandAsync($"-s {serial} shell which su");
        return !string.IsNullOrEmpty(result) && result.Contains("su");
    }
}
