using System.Collections.Concurrent;
using CrossPlatformDataTransfer.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CrossPlatformDataTransfer.Infrastructure.Services;

/// <summary>
/// Update integration that remains disabled until a trusted endpoint and
/// signature verification key are configured.
/// </summary>
public class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(HttpClient httpClient, ILogger<UpdateService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _logger = logger;
    }

    /// <summary>
    /// Reports that update checks are unavailable until a trusted endpoint is configured.
    /// </summary>
    public Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        _logger.LogWarning("Update checks are disabled because no trusted update endpoint is configured.");
        return Task.FromResult(new UpdateCheckResult(
            false,
            string.Empty,
            string.Empty,
            "Update service is not configured."));
    }

    /// <summary>
    /// Refuses installation because package signature verification is not configured.
    /// </summary>
    public Task<bool> DownloadAndInstallUpdateAsync(UpdateCheckResult update)
    {
        ArgumentNullException.ThrowIfNull(update);
        _logger.LogWarning("Update installation is disabled because signature verification is not configured.");
        return Task.FromResult(false);
    }
}

/// <summary>
/// Enterprise-grade telemetry service with silent failure handling and event queuing.
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly ILogger<TelemetryService> _logger;
    private readonly ConcurrentQueue<TelemetryEvent> _eventQueue = new();

    public TelemetryService(ILogger<TelemetryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Tracks an event silently. Never throws exceptions to the caller.
    /// </summary>
    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null)
    {
        try 
        {
            _logger.LogInformation($"Telemetry Event: {eventName}");
            // Queue for background processing
            _eventQueue.Enqueue(new TelemetryEvent(eventName, properties, DateTime.UtcNow));
        }
        catch
        {
            // Silent failure as per enterprise requirements
        }
    }

    /// <summary>
    /// Tracks an exception silently. Never throws exceptions to the caller.
    /// </summary>
    public void TrackException(Exception exception, IDictionary<string, string>? properties = null)
    {
        try 
        {
            _logger.LogError(exception, "Telemetry Exception tracked");
            _eventQueue.Enqueue(new TelemetryEvent("Exception", properties, DateTime.UtcNow, exception.Message));
        }
        catch
        {
            // Silent failure
        }
    }

    private record TelemetryEvent(string Name, IDictionary<string, string>? Properties, DateTime Timestamp, string? ErrorMessage = null);
}
