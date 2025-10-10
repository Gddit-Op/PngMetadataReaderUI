using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using PngMetadataReaderUI.Helpers;
using PngMetadataReaderUI.Models;
using PngMetadataReaderUI.ViewModels;

namespace PngMetadataReaderUI.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            if (!SettingsService.TrySave(viewModel.ToSettings(), out var errorMessage) && !string.IsNullOrEmpty(errorMessage))
            {
                e.Cancel = true;
                Dispatcher.UIThread.Post(async () =>
                    await MessageBoxService.ShowAsync(this, new DialogRequest(
                        "Settings not saved",
                        $"Failed to write settings: {errorMessage}",
                        DialogType.Error)));
            }
        }
    }
}
