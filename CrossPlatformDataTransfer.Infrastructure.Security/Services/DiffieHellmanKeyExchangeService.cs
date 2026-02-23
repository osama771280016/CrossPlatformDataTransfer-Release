using System.Security.Cryptography;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Security.Services;

/// <summary>
/// Service for Elliptic Curve Diffie-Hellman (ECDH) key exchange to provide Perfect Forward Secrecy.
/// </summary>
public class DiffieHellmanKeyExchangeService : IKeyExchangeService, IDisposable
{
    private readonly ECDiffieHellman _ecdh;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiffieHellmanKeyExchangeService"/> class using the NIST P-256 curve.
    /// </summary>
    public DiffieHellmanKeyExchangeService()
    {
        _ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
    }

    /// <summary>
    /// Generates the public key for the key exchange.
    /// </summary>
    /// <returns>The exported public key in SubjectPublicKeyInfo format.</returns>
    public async Task<byte[]> GeneratePublicKeyAsync()
    {
        var publicKey = _ecdh.PublicKey.ExportSubjectPublicKeyInfo();
        return await Task.FromResult(publicKey);
    }

    /// <summary>
    /// Derives a shared secret using the local private key and the remote public key.
    /// </summary>
    /// <param name="remotePublicKey">The public key received from the remote party.</param>
    /// <returns>The derived shared secret as a byte array.</returns>
    public async Task<byte[]> DeriveSharedSecretAsync(byte[] remotePublicKey)
    {
        using var remoteEcdh = ECDiffieHellman.Create();
        remoteEcdh.ImportSubjectPublicKeyInfo(remotePublicKey, out _);
        
        var sharedSecret = _ecdh.DeriveKeyMaterial(remoteEcdh.PublicKey);
        return await Task.FromResult(sharedSecret);
    }

    /// <summary>
    /// Disposes the underlying ECDH object.
    /// </summary>
    public void Dispose()
    {
        _ecdh.Dispose();
        GC.SuppressFinalize(this);
    }
}
