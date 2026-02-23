namespace CrossPlatformDataTransfer.Core.Interfaces.Services;

public interface IKeyManagementService
{
    byte[] GenerateNewKey(int keySizeInBits);
    byte[] GenerateNewNonce(int nonceSizeInBytes);
    // Potentially methods for storing/retrieving keys securely
}
