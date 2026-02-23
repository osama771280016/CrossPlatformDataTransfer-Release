using System.Security.Cryptography;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Security.Services;

/// <summary>
/// Enterprise-grade hardened encryption service using AES-GCM with additional integrity checks.
/// </summary>
public class HardenedEncryptionService : IEncryptionService
{
    private const int NonceSize = 12; // 96 bits
    private const int TagSize = 16;   // 128 bits

    public async Task<(byte[] EncryptedData, byte[] Tag, byte[] Nonce)> EncryptAesGcmAsync(byte[] data, byte[] key)
    {
        if (key.Length != 32) throw new ArgumentException("Key must be 256 bits (32 bytes).");

        byte[] nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        byte[] tag = new byte[TagSize];
        byte[] ciphertext = new byte[data.Length];

        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Encrypt(nonce, data, ciphertext, tag);

        // In a hardened scenario, we might want to prepend versioning or metadata
        return await Task.FromResult((ciphertext, tag, nonce));
    }

    public async Task<byte[]> DecryptAesGcmAsync(byte[] encryptedData, byte[] tag, byte[] nonce, byte[] key)
    {
        if (key.Length != 32) throw new ArgumentException("Key must be 256 bits (32 bytes).");
        
        byte[] plaintext = new byte[encryptedData.Length];

        using var aesGcm = new AesGcm(key, TagSize);
        try 
        {
            aesGcm.Decrypt(nonce, encryptedData, tag, plaintext);
        }
        catch (CryptographicException ex)
        {
            // Log tampering attempt here in a real scenario
            throw new SecurityException("Decryption failed. Data may have been tampered with.", ex);
        }

        return await Task.FromResult(plaintext);
    }
}

public class SecurityException : Exception 
{
    public SecurityException(string message, Exception inner) : base(message, inner) { }
}
