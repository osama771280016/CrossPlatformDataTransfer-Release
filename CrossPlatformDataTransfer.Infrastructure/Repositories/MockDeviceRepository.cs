using CrossPlatformDataTransfer.Core.Entities;
using CrossPlatformDataTransfer.Core.Interfaces.Repositories;

namespace CrossPlatformDataTransfer.Infrastructure.Repositories;

public class MockDeviceRepository : IDeviceRepository
{
    private readonly List<Device> _devices = new();

    public Task<Device?> GetByIdAsync(Guid id) => Task.FromResult(_devices.FirstOrDefault(d => d.Id == id));

    public Task<IEnumerable<Device>> GetAllAsync() => Task.FromResult<IEnumerable<Device>>(_devices);

    public Task AddAsync(Device device)
    {
        _devices.Add(device);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Device device) => Task.CompletedTask;

    public Task DeleteAsync(Guid id) => Task.CompletedTask;
}
