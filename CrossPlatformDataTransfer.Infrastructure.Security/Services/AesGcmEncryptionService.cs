using System.Security.Cryptography;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Security.Services;

public class AesGcmEncryptionService : IEncryptionService
{
    public async Task<(byte[] EncryptedData, byte[] Tag, byte[] Nonce)> EncryptAesGcmAsync(byte[] data, byte[] key)
    {
        // AES-GCM requires a 12-byte (96-bit) nonce for optimal security
        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        // AES-GCM tag is typically 16 bytes (128 bits)
        byte[] tag = new byte[16];
        byte[] ciphertext = new byte[data.Length];

        using var aesGcm = new AesGcm(key, tag.Length);
        aesGcm.Encrypt(nonce, data, ciphertext, tag);

        return await Task.FromResult((ciphertext, tag, nonce));
    }

    public async Task<byte[]> DecryptAesGcmAsync(byte[] encryptedData, byte[] tag, byte[] nonce, byte[] key)
    {
        byte[] plaintext = new byte[encryptedData.Length];

        using var aesGcm = new AesGcm(key, tag.Length);
        aesGcm.Decrypt(nonce, encryptedData, tag, plaintext);

        return await Task.FromResult(plaintext);
    }
}
