/*
using System.Security.Cryptography;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Security.Services;

public class SecurePairingService : IPairingService
{
    private string? _currentCode;
    private DateTime _expiryTime;

    public async Task<string> GeneratePairingCodeAsync()
    {
        // Generate a 6-digit secure random code
        var randomNumber = RandomNumberGenerator.GetInt32(100000, 999999);
        _currentCode = randomNumber.ToString();
        _expiryTime = DateTime.UtcNow.AddMinutes(5); // Valid for 5 minutes
        
        return await Task.FromResult(_currentCode);
    }

    public async Task<bool> ValidatePairingCodeAsync(string code)
    {
        if (string.IsNullOrEmpty(_currentCode) || DateTime.UtcNow > _expiryTime)
        {
            return await Task.FromResult(false);
        }

        var isValid = _currentCode == code;
        if (isValid)
        {
            _currentCode = null; // Consume the code after successful validation
        }

        return await Task.FromResult(isValid);
    }
}
*/
