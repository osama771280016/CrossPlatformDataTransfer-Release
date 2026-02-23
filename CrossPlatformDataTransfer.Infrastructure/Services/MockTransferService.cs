using CrossPlatformDataTransfer.Core.Entities;
using CrossPlatformDataTransfer.Core.Enums;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Infrastructure.Services;

public class MockTransferService : ITransferService
{
    public async Task<TransferSession> StartTransferAsync(string source, string destination, Device targetDevice)
    {
        var session = new TransferSession
        {
            Id = Guid.NewGuid(),
            SourceDevice = new Device { Name = "Local Machine" },
            DestinationDevice = targetDevice,
            Status = TransferStatus.InProgress,
            TotalBytes = 1024 * 1024, // 1MB
            TransferredBytes = 0
        };

        // Simulate progress in background
        _ = Task.Run(async () => {
            while(session.TransferredBytes < session.TotalBytes)
            {
                await Task.Delay(500);
                session.TransferredBytes += 256 * 1024; // 256KB
                if (session.TransferredBytes >= session.TotalBytes)
                {
                    session.Status = TransferStatus.Completed;
                }
            }
        });

        return await Task.FromResult(session);
    }

    public Task CancelTransferAsync(Guid sessionId) => Task.CompletedTask;

    public Task<TransferStatus> GetStatusAsync(Guid sessionId) => Task.FromResult(TransferStatus.InProgress);
}
