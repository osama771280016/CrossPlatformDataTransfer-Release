using System.Net.Http.Json;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Security.Services;

public class LicenseService : ILicenseService
{
    private readonly HttpClient _httpClient;
    private const string LicenseServerUrl = "https://api.yourcompany.com/licenses"; // Placeholder

    public LicenseService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> ActivateAsync(string licenseKey)
    {
        var machineId = GetMachineId();
        var response = await _httpClient.PostAsJsonAsync($"{LicenseServerUrl}/activate", new { licenseKey, machineId });
        
        if (response.IsSuccessStatusCode)
        {
            // In a real app, store the returned JWT token securely (e.g., DPAPI)
            return true;
        }
        return false;
    }

    public async Task<bool> ValidateAsync()
    {
        // Check local storage for token and validate with server
        return await Task.FromResult(true); // Mocked for now
    }

    public async Task<LicenseInfo> GetLicenseInfoAsync()
    {
        return await Task.FromResult(new LicenseInfo("XXXX-XXXX-XXXX", true, DateTime.Now.AddYears(1), GetMachineId()));
    }

    private string GetMachineId()
    {
        // Simple mock machine ID
        return Environment.MachineName + "-" + Environment.UserName;
    }
}
