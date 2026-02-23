using CrossPlatformDataTransfer.Core.Enums;

namespace CrossPlatformDataTransfer.Core.Entities;

public class Device
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DeviceType Type { get; set; }
    public ConnectionStatus ConnectionStatus { get; set; }
    public DateTime LastSeen { get; set; }
}
