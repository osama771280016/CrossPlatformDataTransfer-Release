using CrossPlatformDataTransfer.Core.Entities;

namespace CrossPlatformDataTransfer.Core.Interfaces.Repositories;

public interface ITransferRepository
{
    Task<TransferSession?> GetByIdAsync(Guid id);
    Task<IEnumerable<TransferSession>> GetAllAsync();
    Task AddAsync(TransferSession session);
    Task UpdateAsync(TransferSession session);
    Task DeleteAsync(Guid id);
}
