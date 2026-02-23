using CrossPlatformDataTransfer.Core.Enums;

namespace CrossPlatformDataTransfer.Core.Entities;

public class ClipboardData
{
    public Guid Id { get; set; }
    public DataType Type { get; set; }
    public string? TextContent { get; set; }
    public byte[]? ImageContent { get; set; }
    public DateTime CreatedAt { get; set; }
}
