namespace CrossPlatformDataTransfer.Core.Interfaces.Services;

public interface IPairingService
{
    Task<string> GeneratePairingCodeAsync();
    Task<bool> ValidatePairingCodeAsync(string code);
}
