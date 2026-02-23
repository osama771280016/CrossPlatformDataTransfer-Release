using CrossPlatformDataTransfer.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CrossPlatformDataTransfer.Infrastructure.Services;

public class UpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(HttpClient httpClient, ILogger<UpdateService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        _logger.LogInformation("Checking for updates...");
        // Mocked update check
        return await Task.FromResult(new UpdateCheckResult(false, "1.0.0", "", "No updates available."));
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(UpdateCheckResult update)
    {
        if (!update.HasUpdate) return false;
        _logger.LogInformation($"Downloading update {update.Version} from {update.DownloadUrl}");
        return await Task.FromResult(true);
    }
}

public class TelemetryService : ITelemetryService
{
    private readonly ILogger<TelemetryService> _logger;

    public TelemetryService(ILogger<TelemetryService> logger)
    {
        _logger = logger;
    }

    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null)
    {
        _logger.LogInformation($"Telemetry Event: {eventName}");
    }

    public void TrackException(Exception exception, IDictionary<string, string>? properties = null)
    {
        _logger.LogError(exception, "Telemetry Exception tracked");
    }
}
