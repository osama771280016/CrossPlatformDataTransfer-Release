using CrossPlatformDataTransfer.Core.Entities;
using CrossPlatformDataTransfer.Core.Enums;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Services.Android;

public class AndroidSmsTransferService : ITransferService
{
    private readonly IAdbService _adbService;
    private readonly IEncryptionService _encryptionService;
    private readonly IKeyManagementService _keyManagement;

    private const string SmsDbPath = "/data/data/com.android.providers.telephony/databases/mmssms.db";
    private const string TempLocalPath = "./temp_sms.db";

    public AndroidSmsTransferService(
        IAdbService adbService, 
        IEncryptionService encryptionService,
        IKeyManagementService keyManagement)
    {
        _adbService = adbService;
        _encryptionService = encryptionService;
        _keyManagement = keyManagement;
    }

    public async Task<TransferSession> StartTransferAsync(string source, string destination, Device targetDevice)
    {
        var session = new TransferSession
        {
            Id = Guid.NewGuid(),
            Status = TransferStatus.InProgress,
            SourceDevice = new Device { Name = "Source Android", SerialNumber = source },
            DestinationDevice = targetDevice
        };

        try 
        {
            bool isRooted = await _adbService.IsDeviceRootedAsync(source);
            if (!isRooted)
            {
                throw new UnauthorizedAccessException("Source device must be rooted to access SMS database.");
            }

            string stagingPath = "/data/local/tmp/mmssms_stage.db";
            await _adbService.ExecuteRootCommandAsync(source, $"cp {SmsDbPath} {stagingPath} && chmod 666 {stagingPath}");
            
            bool pulled = await _adbService.PullFileAsync(source, stagingPath, TempLocalPath);
            if (!pulled) throw new Exception("Failed to pull SMS database from source device.");

            await _adbService.ExecuteAdbCommandAsync($"-s {source} shell rm {stagingPath}");

            byte[] dbBytes = await File.ReadAllBytesAsync(TempLocalPath);
            session.TotalBytes = dbBytes.Length;

            // Generate key and encrypt
            var key = _keyManagement.GenerateNewKey(256);
            var result = await _encryptionService.EncryptAesGcmAsync(dbBytes, key);
            
            // For simplicity in this demo, we'll combine Nonce + Tag + Data
            byte[] finalPayload = new byte[result.Nonce.Length + result.Tag.Length + result.EncryptedData.Length];
            Buffer.BlockCopy(result.Nonce, 0, finalPayload, 0, result.Nonce.Length);
            Buffer.BlockCopy(result.Tag, 0, finalPayload, result.Nonce.Length, result.Tag.Length);
            Buffer.BlockCopy(result.EncryptedData, 0, finalPayload, result.Nonce.Length + result.Tag.Length, result.EncryptedData.Length);

            string targetStagingPath = "/data/local/tmp/encrypted_sms.bin";
            string localEncryptedPath = "./encrypted_sms.bin";
            await File.WriteAllBytesAsync(localEncryptedPath, finalPayload);

            bool pushed = await _adbService.PushFileAsync(targetDevice.SerialNumber, localEncryptedPath, targetStagingPath);
            if (!pushed) throw new Exception("Failed to push encrypted data to target device.");

            session.TransferredBytes = session.TotalBytes;
            session.Status = TransferStatus.Completed;

            File.Delete(TempLocalPath);
            File.Delete(localEncryptedPath);
        }
        catch (Exception)
        {
            session.Status = TransferStatus.Failed;
        }

        return session;
    }

    public Task CancelTransferAsync(Guid sessionId) => Task.CompletedTask;

    public Task<TransferStatus> GetStatusAsync(Guid sessionId) => Task.FromResult(TransferStatus.InProgress);
}
