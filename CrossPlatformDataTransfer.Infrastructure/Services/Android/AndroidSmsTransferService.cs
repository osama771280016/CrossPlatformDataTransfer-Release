using CrossPlatformDataTransfer.Core.Entities;
using CrossPlatformDataTransfer.Core.Enums;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Services.Android;

public class AndroidSmsTransferService : ITransferService
{
    public AndroidSmsTransferService() { }

    public Task<TransferSession> StartTransferAsync(string source, string destination, Device targetDevice)
    {
        ArgumentNullException.ThrowIfNull(targetDevice);
        return Task.FromException<TransferSession>(new NotSupportedException(
            "SMS transfer is disabled until Android permission handling, authenticated key exchange, and a tested restore flow are implemented."));
    }

    public Task CancelTransferAsync(Guid sessionId) => Task.FromException(
        new NotSupportedException("SMS transfer is not available."));

    public Task<TransferStatus> GetStatusAsync(Guid sessionId) => Task.FromException<TransferStatus>(
        new NotSupportedException("SMS transfer is not available."));
}
