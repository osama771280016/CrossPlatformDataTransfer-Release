using CrossPlatformDataTransfer.Application.Interfaces;
using CrossPlatformDataTransfer.Application.DTOs;
using CrossPlatformDataTransfer.Application.Transfer;
using CrossPlatformDataTransfer.Core.Interfaces.Services;
using CrossPlatformDataTransfer.Core.Interfaces.Repositories;
using CrossPlatformDataTransfer.Core.Entities;
using Microsoft.Extensions.Logging;

namespace CrossPlatformDataTransfer.Application.UseCases;

public class SecureTransferUseCase : ISecureTransferUseCase
{
    private readonly ChunkedTransferEngine _transferEngine;
    private readonly IDeviceRepository _deviceRepository;
    private readonly ILogger<SecureTransferUseCase> _logger;

    public SecureTransferUseCase(
        ChunkedTransferEngine transferEngine,
        IDeviceRepository deviceRepository,
        ILogger<SecureTransferUseCase> logger)
    {
        _transferEngine = transferEngine;
        _deviceRepository = deviceRepository;
        _logger = logger;
    }

    public async Task<TransferSessionDto> ExecuteAsync(string filePath, Guid targetDeviceId)
    {
        var targetDevice = await _deviceRepository.GetByIdAsync(targetDeviceId);
        if (targetDevice == null) throw new Exception("Target device not found");

        _logger.LogInformation("Initiating secure chunked transfer to {DeviceName}", targetDevice.Name);

        // In a real scenario, we would use the _transferEngine to stream the actual file
        // For the integration demo, we return a DTO representing the initiated session
        return new TransferSessionDto
        {
            Id = Guid.NewGuid(),
            SourceDeviceName = "Local PC",
            DestinationDeviceName = targetDevice.Name,
            Status = "Initiated",
            ProgressPercentage = 0
        };
    }
}
