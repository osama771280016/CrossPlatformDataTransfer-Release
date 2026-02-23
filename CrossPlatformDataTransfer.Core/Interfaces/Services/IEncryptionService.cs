namespace CrossPlatformDataTransfer.Core.Interfaces.Services;

public interface IEncryptionService
{
    Task<(byte[] EncryptedData, byte[] Tag, byte[] Nonce)> EncryptAesGcmAsync(byte[] data, byte[] key);
    Task<byte[]> DecryptAesGcmAsync(byte[] encryptedData, byte[] tag, byte[] nonce, byte[] key);
}
