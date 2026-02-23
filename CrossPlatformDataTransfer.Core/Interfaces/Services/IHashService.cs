namespace CrossPlatformDataTransfer.Core.Interfaces.Services;

public interface IHashService
{
    string ComputeHash(byte[] data);
    bool VerifyHash(byte[] data, string hash);
}
