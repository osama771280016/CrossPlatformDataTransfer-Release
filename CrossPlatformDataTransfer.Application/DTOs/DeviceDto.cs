namespace CrossPlatformDataTransfer.Application.DTOs;

public class DeviceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ConnectionStatus { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
}
