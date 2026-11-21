using System.Diagnostics;
using System.Text.RegularExpressions;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Services.Android;

/// <summary>
/// Runs only the small set of ADB operations required for device discovery and
/// loopback port forwarding. The Android SDK copy is preferred over unrelated
/// adb.exe files earlier on PATH so one consistent ADB server is used.
/// </summary>
public sealed class AdbService : IAdbService
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(20);
    private static readonly Regex SafeSerial = new("^[A-Za-z0-9._:-]{1,128}$", RegexOptions.CultureInvariant);
    private readonly string _adbPath;

    public AdbService() : this(ResolveAdbPath())
    {
    }

    internal AdbService(string adbPath)
    {
        if (string.IsNullOrWhiteSpace(adbPath))
            throw new ArgumentException("An ADB executable path is required.", nameof(adbPath));
        _adbPath = adbPath;
    }

    public async Task<IEnumerable<string>> GetConnectedDeviceSerialsAsync()
    {
        var output = await RunAdbAsync("devices");
        return output.Split(new[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.StartsWith("List of devices", StringComparison.Ordinal))
            .Select(line => line.Split('\t', StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length > 1 && parts[1].Trim() == "device")
            .Select(parts => parts[0].Trim())
            .Where(IsSafeSerial)
            .ToArray();
    }

    public async Task<string> GetDeviceInfoAsync(string serial)
    {
        ValidateSerial(serial);
        return await RunAdbAsync("-s", serial, "shell", "getprop", "ro.product.model");
    }

    public async Task ForwardPortAsync(string serial, int localPort, int remotePort)
    {
        ValidateSerial(serial);
        ValidatePort(localPort, nameof(localPort));
        ValidatePort(remotePort, nameof(remotePort));
        await RunAdbAsync("-s", serial, "forward", $"tcp:{localPort}", $"tcp:{remotePort}");
    }

    public async Task RemoveForwardAsync(string serial, int localPort)
    {
        ValidateSerial(serial);
        ValidatePort(localPort, nameof(localPort));
        await RunAdbAsync("-s", serial, "forward", "--remove", $"tcp:{localPort}");
    }

    private async Task<string> RunAdbAsync(params string[] arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = _adbPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(CommandTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process exited while the timeout cleanup was starting.
            }

            await process.WaitForExitAsync();
            throw new TimeoutException($"ADB did not finish within {CommandTimeout.TotalSeconds:0} seconds.");
        }
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"ADB command failed ({process.ExitCode}): {stderr.Trim()}");

        return string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
    }

    private static string ResolveAdbPath()
    {
        var candidates = new List<string>();
        AddSdkCandidate(candidates, Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"));
        AddSdkCandidate(candidates, Environment.GetEnvironmentVariable("ANDROID_HOME"));
        AddSdkCandidate(candidates, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk"));
        AddSdkCandidate(candidates, @"C:\Android");

        var sdkAdb = candidates.FirstOrDefault(File.Exists);
        if (sdkAdb is not null)
            return Path.GetFullPath(sdkAdb);

        // Use PATH only when no Android SDK installation can be found.
        var pathAdb = FindOnPath("adb.exe");
        if (pathAdb is not null)
            return pathAdb;

        throw new FileNotFoundException(
            "Android Debug Bridge was not found. Install Android SDK Platform-Tools or set ANDROID_SDK_ROOT.");
    }

    private static void AddSdkCandidate(ICollection<string> candidates, string? sdkRoot)
    {
        if (!string.IsNullOrWhiteSpace(sdkRoot))
            candidates.Add(Path.Combine(sdkRoot, "platform-tools", "adb.exe"));
    }

    private static string? FindOnPath(string executable)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path)) return null;

        return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => Path.Combine(directory.Trim().Trim('"'), executable))
            .FirstOrDefault(File.Exists);
    }

    private static bool IsSafeSerial(string serial) => SafeSerial.IsMatch(serial);

    private static void ValidateSerial(string serial)
    {
        if (!IsSafeSerial(serial))
            throw new ArgumentException("The ADB device serial is invalid.", nameof(serial));
    }

    private static void ValidatePort(int port, string parameterName)
    {
        if (port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
