using CrossPlatformDataTransfer.Core.Entities;

namespace CrossPlatformDataTransfer.Core.Interfaces.Repositories;

public interface IDeviceRepository
{
    Task<Device?> GetByIdAsync(Guid id);
    Task<IEnumerable<Device>> GetAllAsync();
    Task AddAsync(Device device);
    Task UpdateAsync(Device device);
    Task DeleteAsync(Guid id);
}
