using CrossPlatformDataTransfer.Application.DTOs;
using CrossPlatformDataTransfer.Application.Interfaces;
using CrossPlatformDataTransfer.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace CrossPlatformDataTransfer.Application.UseCases;

public class SecureTransferUseCase : ISecureTransferUseCase
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly ILogger<SecureTransferUseCase> _logger;

    public SecureTransferUseCase(
        IDeviceRepository deviceRepository,
        ILogger<SecureTransferUseCase> logger)
    {
        _deviceRepository = deviceRepository;
        _logger = logger;
    }

    public async Task<TransferSessionDto> ExecuteAsync(string filePath, Guid targetDeviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("The selected file was not found.", filePath);

        var targetDevice = await _deviceRepository.GetByIdAsync(targetDeviceId);
        if (targetDevice == null) throw new InvalidOperationException("Target device not found.");

        _logger.LogWarning("File transfer is unavailable because no compatible authenticated Android agent is installed.");
        throw new NotSupportedException(
            "File transfer is unavailable until a compatible authenticated Android agent is installed.");
    }
}
