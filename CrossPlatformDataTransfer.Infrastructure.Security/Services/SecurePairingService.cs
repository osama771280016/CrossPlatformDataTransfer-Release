using System.Security.Cryptography;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Security.Services;

/// <summary>
/// Service for establishing a secure pairing between devices using a short-lived random code.
/// </summary>
public class SecurePairingService : IPairingService
{
    private string? _currentCode;
    private DateTime _expiryTime;

    /// <summary>
    /// Generates a secure 6-digit random pairing code valid for 5 minutes.
    /// </summary>
    /// <returns>The generated pairing code.</returns>
    public async Task<string> GeneratePairingCodeAsync()
    {
        // Generate a 6-digit secure random code
        var randomNumber = RandomNumberGenerator.GetInt32(100000, 999999);
        _currentCode = randomNumber.ToString();
        _expiryTime = DateTime.UtcNow.AddMinutes(5); // Valid for 5 minutes
        
        return await Task.FromResult(_currentCode);
    }

    /// <summary>
    /// Validates the provided pairing code against the current active code.
    /// </summary>
    /// <param name="code">The code to validate.</param>
    /// <returns>True if the code is valid and not expired; otherwise, false.</returns>
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
