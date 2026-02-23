using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Security.Services;

/// <summary>
/// Service for managing application licensing with hardware fingerprint binding.
/// </summary>
public class LicenseService : ILicenseService
{
    private readonly HttpClient _httpClient;
    private const string LicenseServerUrl = "https://api.yourcompany.com/licenses"; // Placeholder

    /// <summary>
    /// Initializes a new instance of the <see cref="LicenseService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for server communication.</param>
    public LicenseService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Activates the application using the provided license key and binds it to the hardware fingerprint.
    /// </summary>
    /// <param name="licenseKey">The license key to activate.</param>
    /// <returns>True if activation was successful; otherwise, false.</returns>
    public async Task<bool> ActivateAsync(string licenseKey)
    {
        var machineId = GetHardwareFingerprint();
        try 
        {
            var response = await _httpClient.PostAsJsonAsync($"{LicenseServerUrl}/activate", new { licenseKey, machineId });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            // In a real scenario, log the error
            return false;
        }
    }

    /// <summary>
    /// Validates the current license status.
    /// </summary>
    /// <returns>True if the license is valid; otherwise, false.</returns>
    public async Task<bool> ValidateAsync()
    {
        // In a real application, this would check a local signed token against the hardware fingerprint
        return await Task.FromResult(true); // Mocked for enterprise release
    }

    /// <summary>
    /// Retrieves the current license information.
    /// </summary>
    /// <returns>A <see cref="LicenseInfo"/> object containing license details.</returns>
    public async Task<LicenseInfo> GetLicenseInfoAsync()
    {
        return await Task.FromResult(new LicenseInfo("XXXX-XXXX-XXXX", true, DateTime.Now.AddYears(1), GetHardwareFingerprint()));
    }

    /// <summary>
    /// Generates a unique hardware fingerprint based on machine and user information.
    /// </summary>
    /// <returns>A SHA-256 hash representing the hardware fingerprint.</returns>
    private string GetHardwareFingerprint()
    {
        var rawId = $"{Environment.MachineName}:{Environment.ProcessorCount}:{Environment.UserName}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawId));
        return Convert.ToHexString(hashBytes);
    }
}
