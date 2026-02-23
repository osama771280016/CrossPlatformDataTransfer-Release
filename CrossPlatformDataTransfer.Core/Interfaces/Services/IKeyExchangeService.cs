namespace CrossPlatformDataTransfer.Core.Interfaces.Services;

public interface IKeyExchangeService
{
    Task<byte[]> GeneratePublicKeyAsync();
    Task<byte[]> DeriveSharedSecretAsync(byte[] remotePublicKey);
}
