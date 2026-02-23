namespace CrossPlatformDataTransfer.Core.Interfaces.Services;

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckForUpdatesAsync();
    Task<bool> DownloadAndInstallUpdateAsync(UpdateCheckResult update);
}

public record UpdateCheckResult(bool HasUpdate, string Version, string DownloadUrl, string ReleaseNotes);

public interface ITelemetryService
{
    void TrackEvent(string eventName, IDictionary<string, string>? properties = null);
    void TrackException(Exception exception, IDictionary<string, string>? properties = null);
}
