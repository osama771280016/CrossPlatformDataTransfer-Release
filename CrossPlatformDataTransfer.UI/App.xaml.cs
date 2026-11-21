using System.Windows;
using CrossPlatformDataTransfer.Core.Interfaces.Services;
using CrossPlatformDataTransfer.Startup;
using CrossPlatformDataTransfer.UI.ViewModels;
using CrossPlatformDataTransfer.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrossPlatformDataTransfer.UI;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        services.AddAllProjectServices();
        services.AddSingleton<ILogger<MainViewModel>>(NullLogger<MainViewModel>.Instance);
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        var window = _serviceProvider.GetRequiredService<MainWindow>();
        window.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is not null)
        {
            _serviceProvider.GetRequiredService<IAgentCommunicationService>()
                .DisconnectAsync().GetAwaiter().GetResult();
            _serviceProvider.GetRequiredService<IDeviceDiscoveryService>()
                .StopDiscoveryAsync().GetAwaiter().GetResult();
            _serviceProvider.Dispose();
        }

        base.OnExit(e);
    }
}
