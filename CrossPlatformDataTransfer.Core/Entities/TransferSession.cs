using CrossPlatformDataTransfer.Core.Enums;

namespace CrossPlatformDataTransfer.Core.Entities;

public class TransferSession
{
    public Guid Id { get; set; }
    public Device SourceDevice { get; set; } = new();
    public Device DestinationDevice { get; set; } = new();
    public TransferDirection Direction { get; set; }
    public TransferStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<TransferItem> Items { get; set; } = new();
    public long TotalBytes { get; set; }
    public long TransferredBytes { get; set; }
    public string? ErrorMessage { get; set; }
}
