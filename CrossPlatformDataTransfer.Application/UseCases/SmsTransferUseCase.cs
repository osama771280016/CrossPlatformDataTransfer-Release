using CrossPlatformDataTransfer.Application.DTOs;
using CrossPlatformDataTransfer.Core.Interfaces.Repositories;
using CrossPlatformDataTransfer.Core.Interfaces.Services;

namespace CrossPlatformDataTransfer.Application.UseCases;

public class SmsTransferUseCase
{
    private readonly ITransferService _transferService;
    private readonly IDeviceRepository _deviceRepository;

    public SmsTransferUseCase(ITransferService transferService, IDeviceRepository deviceRepository)
    {
        _transferService = transferService;
        _deviceRepository = deviceRepository;
    }

    public async Task<TransferSessionDto> ExecuteAsync(string sourceSerial, Guid targetDeviceId)
    {
        var targetDevice = await _deviceRepository.GetByIdAsync(targetDeviceId);
        if (targetDevice == null) throw new Exception("Target device not found.");

        // The TransferService now handles the real ADB pipeline
        var session = await _transferService.StartTransferAsync(sourceSerial, "sms_backup", targetDevice);

        return new TransferSessionDto
        {
            Id = session.Id,
            Status = session.Status.ToString(),
            TotalBytes = session.TotalBytes,
            TransferredBytes = session.TransferredBytes,
            ProgressPercentage = session.TotalBytes > 0 
                ? (int)((double)session.TransferredBytes / session.TotalBytes * 100) 
                : 0
        };
    }
}
