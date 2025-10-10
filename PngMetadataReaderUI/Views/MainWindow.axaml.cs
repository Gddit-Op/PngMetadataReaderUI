using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using PngMetadataReaderUI.Helpers;
using PngMetadataReaderUI.Models;
using PngMetadataReaderUI.ViewModels;
using System;
using System.Linq;

namespace PngMetadataReaderUI.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.DialogRequested -= OnDialogRequested;
        }

        _viewModel = DataContext as MainWindowViewModel;

        if (_viewModel != null)
        {
            _viewModel.DialogRequested += OnDialogRequested;
        }

        base.OnDataContextChanged(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.DialogRequested -= OnDialogRequested;
        }

        base.OnClosed(e);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer is IAsyncDataTransfer dataTransfer &&
            dataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private async void Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (e.DataTransfer is IAsyncDataTransfer asyncDataTransfer)
            {
                var files = await asyncDataTransfer.TryGetFilesAsync();
                if (files != null && files.Any())
                {
                    var pngFile = files.FirstOrDefault(f =>
                        f.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

                    if (pngFile != null)
                    {
                        viewModel.LoadImageCommand.Execute(pngFile.TryGetLocalPath());
                        return;
                    }
                }
            }

            viewModel.StatusMessage = "Please drop a PNG file.";
        }
    }

    private async void BrowseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Select PNG Image",
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("PNG Images")
                    {
                        Patterns = new[] { "*.png" }
                    }
                },
                AllowMultiple = false
            };

            var result = await StorageProvider.OpenFilePickerAsync(options);
            if (result != null && result.Count > 0)
            {
                var filePath = result[0].Path.LocalPath;
                viewModel.LoadImageCommand.Execute(filePath);
            }
        }
    }

    private async void SettingsMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var settings = SettingsService.Load();
        var settingsViewModel = new SettingsViewModel();
        settingsViewModel.Apply(settings);

        var window = new SettingsWindow
        {
            DataContext = settingsViewModel
        };

        await window.ShowDialog(this);
    }

    private void ExitMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private async void OnDialogRequested(object? sender, DialogRequest request)
    {
        await MessageBoxService.ShowAsync(this, request);
    }
}
