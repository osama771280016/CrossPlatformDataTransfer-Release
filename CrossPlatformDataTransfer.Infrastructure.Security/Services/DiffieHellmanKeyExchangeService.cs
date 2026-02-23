/*
using System.Security.Cryptography;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Security.Services;

public class DiffieHellmanKeyExchangeService : IKeyExchangeService, IDisposable
{
    private readonly ECDiffieHellman _ecdh;

    public DiffieHellmanKeyExchangeService()
    {
        _ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
    }

    public async Task<byte[]> GeneratePublicKeyAsync()
    {
        var publicKey = _ecdh.PublicKey.ExportSubjectPublicKeyInfo();
        return await Task.FromResult(publicKey);
    }

    public async Task<byte[]> DeriveSharedSecretAsync(byte[] remotePublicKey)
    {
        using var remoteEcdh = ECDiffieHellman.Create();
        remoteEcdh.ImportSubjectPublicKeyInfo(remotePublicKey, out _);
        
        var sharedSecret = _ecdh.DeriveKeyMaterial(remoteEcdh.PublicKey);
        return await Task.FromResult(sharedSecret);
    }

    public void Dispose()
    {
        _ecdh.Dispose();
    }
}
*/
