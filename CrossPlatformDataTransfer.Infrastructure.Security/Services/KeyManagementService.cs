using System.Security.Cryptography;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Security.Services;

public class KeyManagementService : IKeyManagementService
{
    public byte[] GenerateNewKey(int keySizeInBits)
    {
        if (keySizeInBits % 8 != 0)
            throw new ArgumentException("Key size must be a multiple of 8.");

        byte[] key = new byte[keySizeInBits / 8];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    public byte[] GenerateNewNonce(int nonceSizeInBytes)
    {
        byte[] nonce = new byte[nonceSizeInBytes];
        RandomNumberGenerator.Fill(nonce);
        return nonce;
    }
}
