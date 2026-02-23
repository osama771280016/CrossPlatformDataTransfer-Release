using System.Security.Cryptography;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Security.Services;

public class Sha256HashService : IHashService
{
    public string ComputeHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    public bool VerifyHash(byte[] data, string hash)
    {
        var computedHash = ComputeHash(data);
        return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
    }
}
