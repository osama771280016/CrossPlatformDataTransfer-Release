namespace CrossPlatformDataTransfer.Application.DTOs;

public class TransferSessionDto
{
    public Guid Id { get; set; }
    public string SourceDeviceName { get; set; } = string.Empty;
    public string DestinationDeviceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; }
    public long TotalBytes { get; set; }
    public long TransferredBytes { get; set; }
    public string? ErrorMessage { get; set; }
}
