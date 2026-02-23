namespace CrossPlatformDataTransfer.Core.Interfaces.Services;

public interface ILicenseService
{
    Task<bool> ActivateAsync(string licenseKey);
    Task<bool> ValidateAsync();
    Task<LicenseInfo> GetLicenseInfoAsync();
}

public record LicenseInfo(string LicenseKey, bool IsActive, DateTime? ExpiryDate, string MachineId);
