using System.Windows;
using CrossPlatformDataTransfer.UI.ViewModels;

namespace CrossPlatformDataTransfer.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is MainViewModel viewModel)
                await viewModel.DiscoverDevicesCommand.ExecuteAsync(null);
        };
    }
}
