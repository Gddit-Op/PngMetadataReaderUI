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
    private ScrollViewer? _imageScrollViewer;

    public MainWindow()
    {
        InitializeComponent();
        _imageScrollViewer = this.FindControl<ScrollViewer>("ImageScrollViewer");
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
            if (_imageScrollViewer != null)
            {
                var size = _imageScrollViewer.Bounds.Size;
                if (size.Width > 0 && size.Height > 0)
                {
                    _viewModel.UpdateViewportSize(size.Width, size.Height);
                }
            }
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
                    foreach (var file in files)
                    {
                        var localPath = file.TryGetLocalPath();
                        if (ImageFormatHelper.IsSupportedImageFile(localPath))
                        {
                            viewModel.LoadImageCommand.Execute(localPath);
                            return;
                        }
                    }
                }
            }

            viewModel.StatusMessage = "Please drop a supported image file (PNG/JPG/WebP).";
        }
    }

    private async void BrowseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Select Image",
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Supported Images")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp" }
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

    private void ImageScrollViewer_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        var newSize = e.NewSize;
        if (newSize.Width > 0 && newSize.Height > 0)
        {
            _viewModel.UpdateViewportSize(newSize.Width, newSize.Height);
        }
    }

    private void DropZone_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_viewModel?.Image == null)
        {
            return;
        }

        if (e.Delta.Y > 0)
        {
            if (_viewModel.ZoomInCommand.CanExecute(null))
            {
                _viewModel.ZoomInCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Delta.Y < 0)
        {
            if (_viewModel.ZoomOutCommand.CanExecute(null))
            {
                _viewModel.ZoomOutCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
