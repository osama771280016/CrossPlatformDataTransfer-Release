using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Security.Services;

/// <summary>
/// Placeholder for a future licensing integration. No licensing backend or
/// signed local license format is configured in this release.
/// </summary>
public class LicenseService : ILicenseService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LicenseService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for server communication.</param>
    public LicenseService(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
    }

    /// <summary>
    /// Activates the application using the provided license key and binds it to the hardware fingerprint.
    /// </summary>
    /// <param name="licenseKey">The license key to activate.</param>
    /// <returns>True if activation was successful; otherwise, false.</returns>
    public Task<bool> ActivateAsync(string licenseKey) => Task.FromResult(false);

    /// <summary>
    /// Validates the current license status.
    /// </summary>
    /// <returns>True if the license is valid; otherwise, false.</returns>
    public Task<bool> ValidateAsync() => Task.FromResult(false);

    /// <summary>
    /// Retrieves the current license information.
    /// </summary>
    /// <returns>A <see cref="LicenseInfo"/> object containing license details.</returns>
    public Task<LicenseInfo> GetLicenseInfoAsync() =>
        Task.FromResult(new LicenseInfo(string.Empty, false, null, string.Empty));
}
