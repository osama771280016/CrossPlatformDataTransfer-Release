using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrossPlatformDataTransfer.Application.Interfaces;
using CrossPlatformDataTransfer.Application.DTOs;
using CrossPlatformDataTransfer.Application.Transfer;
using Microsoft.Extensions.Logging;

namespace CrossPlatformDataTransfer.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDeviceDiscoveryUseCase _discoveryUseCase;
    private readonly ISecureTransferUseCase _transferUseCase;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartTransferCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private double _transferProgress;

    [ObservableProperty]
    private string _progressText = string.Empty;

    public ObservableCollection<DeviceDto> Devices { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartTransferCommand))]
    private DeviceDto? _selectedDevice;

    public MainViewModel(
        IDeviceDiscoveryUseCase discoveryUseCase,
        ISecureTransferUseCase transferUseCase,
        ILogger<MainViewModel> logger)
    {
        _discoveryUseCase = discoveryUseCase;
        _transferUseCase = transferUseCase;
        _logger = logger;

        _discoveryUseCase.DeviceDiscovered += (s, device) => 
        {
            // In a real WPF app, we would use Dispatcher.Invoke here
            Devices.Add(device);
        };
    }

    [RelayCommand]
    private async Task DiscoverDevicesAsync()
    {
        IsBusy = true;
        StatusMessage = "Discovering devices...";
        Devices.Clear();
        
        try
        {
            await _discoveryUseCase.ExecuteAsync();
            StatusMessage = $"Discovery completed. Found {Devices.Count} devices.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Discovery failed");
            StatusMessage = "Discovery failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanTransfer))]
    private async Task StartTransferAsync()
    {
        if (SelectedDevice == null) return;

        IsBusy = true;
        StatusMessage = $"Starting secure transfer to {SelectedDevice.Name}...";
        TransferProgress = 0;

        try
        {
            // The transfer use case will internally use the ChunkedTransferEngine
            var result = await _transferUseCase.ExecuteAsync("selected_data.dat", SelectedDevice.Id);
            
            TransferProgress = 100;
            StatusMessage = $"Transfer {result.Status}!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transfer failed");
            StatusMessage = $"Transfer failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanTransfer() => SelectedDevice != null && !IsBusy;
}
