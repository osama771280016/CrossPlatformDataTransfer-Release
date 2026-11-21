using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrossPlatformDataTransfer.Application.Interfaces;
using CrossPlatformDataTransfer.Application.DTOs;
using Microsoft.Extensions.Logging;
using CrossPlatformDataTransfer.Core.Interfaces.Services;
using System.Text.Json;

namespace CrossPlatformDataTransfer.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDeviceDiscoveryUseCase _discoveryUseCase;
    private readonly IAgentCommunicationService _agentClient;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "جاهز";

    [ObservableProperty]
    private double _transferProgress;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private string _pairingCode = string.Empty;

    [ObservableProperty]
    private bool _isAgentConnected;

    [ObservableProperty]
    private bool _isSecureChannelReady;

    [ObservableProperty]
    private string _verificationCode = string.Empty;

    [ObservableProperty]
    private bool _isDisconnectAvailable;

    public ObservableCollection<DeviceDto> Devices { get; } = new();

    [ObservableProperty]
    private DeviceDto? _selectedDevice;

    public MainViewModel(
        IDeviceDiscoveryUseCase discoveryUseCase,
        IAgentCommunicationService agentClient,
        ILogger<MainViewModel> logger)
    {
        _discoveryUseCase = discoveryUseCase;
        _agentClient = agentClient;
        _logger = logger;

        _discoveryUseCase.DeviceDiscovered += (s, device) => 
        {
            System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                if (Devices.All(existing => existing.Id != device.Id))
                    Devices.Add(device);

                StatusMessage = $"تم اكتشاف الجهاز: {device.Name}";
            });
        };

        _discoveryUseCase.DeviceStatusChanged += (_, change) =>
        {
            System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                var device = Devices.FirstOrDefault(existing => existing.Id == change.DeviceId);
                if (device is not null)
                    device.ConnectionStatus = change.Status.ToString();
            });
        };
    }

    [RelayCommand]
    private async Task ConnectAgentAsync()
    {
        if (IsBusy) return;
        if (SelectedDevice is null)
        {
            StatusMessage = "اختر هاتف Android متصلًا أولًا.";
            return;
        }

        if (PairingCode.Length != 8 || PairingCode.Any(ch => !char.IsAsciiDigit(ch)))
        {
            StatusMessage = "أدخل رمز الاقتران المؤقت المكوّن من 8 أرقام الظاهر على الهاتف.";
            return;
        }

        IsBusy = true;
        IsAgentConnected = false;
        IsSecureChannelReady = false;
        IsDisconnectAvailable = false;
        VerificationCode = string.Empty;
        StatusMessage = "جارٍ إنشاء قناة TLS الآمنة…";
        try
        {
            await _agentClient.ConnectAsync(SelectedDevice.SerialNumber, 4711);
            VerificationCode = _agentClient.VerificationCode ?? string.Empty;
            if (string.IsNullOrWhiteSpace(VerificationCode))
            {
                StatusMessage = "تعذر التحقق من شهادة TLS.";
                await _agentClient.DisconnectAsync();
                return;
            }

            IsSecureChannelReady = true;
            IsDisconnectAvailable = true;
            StatusMessage = "قارن بصمة TLS مع الهاتف. أكّد التطابق على الهاتف، ثم أكمل الاقتران هنا.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Android Agent secure connection failed");
            StatusMessage = "تعذر إنشاء اتصال TLS. تأكد من تشغيل الوكيل على الهاتف والسماح بتصحيح USB.";
            try { await _agentClient.DisconnectAsync(); } catch (Exception cleanupError) { _logger.LogDebug(cleanupError, "Agent cleanup failed"); }
            IsSecureChannelReady = false;
            IsDisconnectAvailable = false;
            VerificationCode = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmAndPairAsync()
    {
        if (IsBusy) return;
        if (!IsSecureChannelReady || !_agentClient.IsConnected)
        {
            StatusMessage = "Establish the secure channel and compare the verification codes first.";
            return;
        }
        if (PairingCode.Length != 8 || PairingCode.Any(ch => !char.IsAsciiDigit(ch)))
        {
            StatusMessage = "Enter the current 8-digit pairing code shown on the phone.";
            return;
        }

        IsBusy = true;
        StatusMessage = "بانتظار تأكيد الهاتف ونتيجة الاقتران…";
        try
        {
            var pair = await _agentClient.SendCommandAsync(new AgentCommand
            {
                CommandType = "PAIR",
                Parameters = new Dictionary<string, string> { ["pairingCode"] = PairingCode }
            });

            if (!pair.Success)
            {
                StatusMessage = pair.Message;
                await _agentClient.DisconnectAsync();
                IsSecureChannelReady = false;
                IsDisconnectAvailable = false;
                VerificationCode = string.Empty;
                return;
            }

            var handshake = await _agentClient.SendCommandAsync(new AgentCommand { CommandType = "HANDSHAKE" });
            if (!handshake.Success)
            {
                StatusMessage = $"تم الاقتران، لكن تعذرت المصافحة: {handshake.Message}";
                await _agentClient.DisconnectAsync();
                IsSecureChannelReady = false;
                IsDisconnectAvailable = false;
                VerificationCode = string.Empty;
                return;
            }

            IsAgentConnected = true;
            IsSecureChannelReady = false;
            IsDisconnectAvailable = true;
            PairingCode = string.Empty;
            VerificationCode = string.Empty;
            var description = "تم الاتصال بوكيل Android";
            try
            {
                using var details = JsonDocument.Parse(handshake.Message);
                var root = details.RootElement;
                var model = root.TryGetProperty("model", out var modelValue) ? modelValue.GetString() : null;
                var api = root.TryGetProperty("androidApi", out var apiValue) ? apiValue.ToString() : null;
                if (!string.IsNullOrWhiteSpace(model)) description += $": {model}";
                if (!string.IsNullOrWhiteSpace(api)) description += $" (Android API {api})";
            }
            catch (JsonException)
            {
                // Preserve a successful handshake even if an older agent returns plain text.
            }

            StatusMessage = description + ". نقل البيانات غير مفعّل في هذه النسخة.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Android Agent pairing failed");
            StatusMessage = "فشل الاقتران. تأكد من قبول الهاتف لبصمة TLS المتطابقة وأن رمز الاقتران حديث.";
            try { await _agentClient.DisconnectAsync(); } catch (Exception cleanupError) { _logger.LogDebug(cleanupError, "Agent cleanup failed"); }
            IsSecureChannelReady = false;
            IsDisconnectAvailable = false;
            VerificationCode = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectAgentAsync()
    {
        try
        {
            await _agentClient.DisconnectAsync();
            IsAgentConnected = false;
            IsSecureChannelReady = false;
            IsDisconnectAvailable = false;
            VerificationCode = string.Empty;
            StatusMessage = "تم قطع اتصال وكيل Android.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Android Agent disconnect failed");
            StatusMessage = "تعذر إغلاق اتصال وكيل Android بصورة سليمة.";
        }
    }

    [RelayCommand]
    private async Task DiscoverDevicesAsync()
    {
        IsBusy = true;
        StatusMessage = "جارٍ فحص الأجهزة…";

        try
        {
            await _discoveryUseCase.ExecuteAsync();
            StatusMessage = "جارٍ البحث عن هواتف Android عبر ADB. إذا لم يظهر هاتفك، تحقق من كابل USB وتصريح تصحيح USB.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Discovery failed");
            StatusMessage = "فشل فحص الأجهزة.";
        }
        finally
        {
            IsBusy = false;
        }
    }

}
