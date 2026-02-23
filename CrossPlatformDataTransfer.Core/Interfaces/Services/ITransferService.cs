using CrossPlatformDataTransfer.Core.Entities;
using CrossPlatformDataTransfer.Core.Enums;

namespace CrossPlatformDataTransfer.Core.Interfaces.Services;

public interface ITransferService
{
    Task<TransferSession> StartTransferAsync(string source, string destination, Device targetDevice);
    Task CancelTransferAsync(Guid sessionId);
    Task<TransferStatus> GetStatusAsync(Guid sessionId);
}
