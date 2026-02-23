using CrossPlatformDataTransfer.Application.DTOs;

namespace CrossPlatformDataTransfer.Application.Interfaces;

public interface ISecureTransferUseCase
{
    Task<TransferSessionDto> ExecuteAsync(string filePath, Guid targetDeviceId);
}
