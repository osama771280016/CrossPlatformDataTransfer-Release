using CrossPlatformDataTransfer.Core.Enums;

namespace CrossPlatformDataTransfer.Core.Entities;

public class TransferItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public TransferType Type { get; set; }
    public string? Hash { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
