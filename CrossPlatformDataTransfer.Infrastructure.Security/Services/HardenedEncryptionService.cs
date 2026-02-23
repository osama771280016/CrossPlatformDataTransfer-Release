using System.Security.Cryptography;
using System.Text;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Security.Services;

/// <summary>
/// Enterprise-grade hardened encryption service using AES-256-GCM with PBKDF2 key derivation.
/// </summary>
public class HardenedEncryptionService : IEncryptionService
{
    private const int NonceSize = 12; // 96 bits for GCM
    private const int TagSize = 16;   // 128 bits for GCM
    private const int SaltSize = 16;
    private const int Iterations = 100000;

    /// <summary>
    /// Encrypts data using AES-256-GCM with a random nonce.
    /// </summary>
    /// <param name="data">The plaintext data to encrypt.</param>
    /// <param name="key">The 256-bit encryption key.</param>
    /// <returns>A tuple containing the encrypted data, authentication tag, and nonce.</returns>
    public async Task<(byte[] EncryptedData, byte[] Tag, byte[] Nonce)> EncryptAesGcmAsync(byte[] data, byte[] key)
    {
        if (key.Length != 32) throw new ArgumentException("Key must be 256 bits (32 bytes).");

        byte[] nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        byte[] tag = new byte[TagSize];
        byte[] ciphertext = new byte[data.Length];

        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Encrypt(nonce, data, ciphertext, tag);

        return await Task.FromResult((ciphertext, tag, nonce));
    }

    /// <summary>
    /// Decrypts data using AES-256-GCM and validates the authentication tag.
    /// </summary>
    /// <param name="encryptedData">The ciphertext to decrypt.</param>
    /// <param name="tag">The authentication tag.</param>
    /// <param name="nonce">The nonce used during encryption.</param>
    /// <param name="key">The 256-bit encryption key.</param>
    /// <returns>The decrypted plaintext data.</returns>
    /// <exception cref="SecurityException">Thrown if decryption fails or tag validation fails.</exception>
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
            throw new SecurityException("Decryption failed. Data may have been tampered with.", ex);
        }

        return await Task.FromResult(plaintext);
    }

    /// <summary>
    /// Derives a secure 256-bit key from a password using PBKDF2.
    /// </summary>
    /// <param name="password">The user password.</param>
    /// <param name="salt">The salt for key derivation.</param>
    /// <returns>A 256-bit derived key.</returns>
    public byte[] DeriveKey(string password, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }

    /// <summary>
    /// Generates a random salt for key derivation.
    /// </summary>
    /// <returns>A random 16-byte salt.</returns>
    public byte[] GenerateSalt()
    {
        byte[] salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }
}

/// <summary>
/// Exception thrown when a security-related operation fails.
/// </summary>
public class SecurityException : Exception 
{
    public SecurityException(string message, Exception inner) : base(message, inner) { }
}
