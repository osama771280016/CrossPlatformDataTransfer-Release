using System.Security.Cryptography;
using CrossPlatformDataTransfer.Application.DTOs;
using CrossPlatformDataTransfer.Application.Interfaces;
using CrossPlatformDataTransfer.Application.UseCases;
using CrossPlatformDataTransfer.Core.Entities;
using CrossPlatformDataTransfer.Core.Interfaces.Repositories;
using CrossPlatformDataTransfer.Infrastructure.Security.Services;
using CrossPlatformDataTransfer.Infrastructure.Services;
using CrossPlatformDataTransfer.Infrastructure.Services.Android;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CrossPlatformDataTransfer.Tests;

public sealed class SafetyBoundaryTests
{
    [Fact]
    public async Task HardenedEncryption_RoundTripsAuthenticatedPayload()
    {
        var service = new HardenedEncryptionService();
        var key = RandomNumberGenerator.GetBytes(32);
        var plaintext = RandomNumberGenerator.GetBytes(8192);

        var encrypted = await service.EncryptAesGcmAsync(plaintext, key);
        var decrypted = await service.DecryptAesGcmAsync(
            encrypted.EncryptedData, encrypted.Tag, encrypted.Nonce, key);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public async Task HardenedEncryption_RejectsTamperedCiphertext()
    {
        var service = new HardenedEncryptionService();
        var key = RandomNumberGenerator.GetBytes(32);
        var encrypted = await service.EncryptAesGcmAsync("private payload"u8.ToArray(), key);
        encrypted.EncryptedData[0] ^= 0x01;

        await Assert.ThrowsAsync<CrossPlatformDataTransfer.Infrastructure.Security.Services.SecurityException>(
            () => service.DecryptAesGcmAsync(
                encrypted.EncryptedData, encrypted.Tag, encrypted.Nonce, key));
    }

    [Fact]
    public async Task LicenseService_FailsClosedWithoutConfiguredBackend()
    {
        var service = new LicenseService(new HttpClient());

        Assert.False(await service.ActivateAsync("any-key"));
        Assert.False(await service.ValidateAsync());

        var info = await service.GetLicenseInfoAsync();
        Assert.False(info.IsActive);
        Assert.Null(info.ExpiryDate);
        Assert.Empty(info.LicenseKey);
        Assert.Empty(info.MachineId);
    }

    [Fact]
    public async Task UpdateService_DoesNotClaimAnUpdateOrInstallUnsignedContent()
    {
        var service = new UpdateService(
            new HttpClient(), NullLogger<UpdateService>.Instance);

        var result = await service.CheckForUpdatesAsync();

        Assert.False(result.HasUpdate);
        Assert.Equal("Update service is not configured.", result.ReleaseNotes);
        Assert.False(await service.DownloadAndInstallUpdateAsync(result with { HasUpdate = true }));
    }

    [Fact]
    public async Task SmsTransfer_RefusesUnsupportedRootBasedFlow()
    {
        var service = new AndroidSmsTransferService();

        await Assert.ThrowsAsync<NotSupportedException>(() => service.StartTransferAsync(
            "source", "destination", new Device { SerialNumber = "target" }));
    }

    [Fact]
    public async Task FileTransfer_RefusesToReportSuccessWithoutAndroidAgent()
    {
        var device = new Device { Id = Guid.NewGuid(), Name = "Test device" };
        var repository = new SingleDeviceRepository(device);
        var useCase = new SecureTransferUseCase(
            repository, NullLogger<SecureTransferUseCase>.Instance);
        var filePath = Path.GetTempFileName();

        try
        {
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                useCase.ExecuteAsync(filePath, device.Id));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private sealed class SingleDeviceRepository(Device device) : IDeviceRepository
    {
        public Task<Device?> GetByIdAsync(Guid id) =>
            Task.FromResult<Device?>(id == device.Id ? device : null);

        public Task<IEnumerable<Device>> GetAllAsync() =>
            Task.FromResult<IEnumerable<Device>>([device]);

        public Task AddAsync(Device value) => Task.CompletedTask;

        public Task UpdateAsync(Device value) => Task.CompletedTask;

        public Task DeleteAsync(Guid id) => Task.CompletedTask;
    }
}
