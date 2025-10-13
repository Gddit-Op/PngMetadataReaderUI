using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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
    private bool _isPanning;
    private Point _panStartPoint;
    private Vector _panStartOffset;

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
        if (_viewModel?.Image == null || _imageScrollViewer is null)
        {
            return;
        }

        var scrollViewer = _imageScrollViewer;
        var zoomInRequested = e.Delta.Y > 0 && _viewModel.ZoomInCommand.CanExecute(null);
        var zoomOutRequested = e.Delta.Y < 0 && _viewModel.ZoomOutCommand.CanExecute(null);

        if (!zoomInRequested && !zoomOutRequested)
        {
            return;
        }

        var oldZoom = _viewModel.ImageZoom;
        if (oldZoom <= 0)
        {
            return;
        }

        var imageSize = _viewModel.Image.Size;
        if (imageSize.Width <= 0 || imageSize.Height <= 0)
        {
            return;
        }

        var viewport = scrollViewer.Viewport;
        var viewportPosition = e.GetPosition(scrollViewer);
        var currentOffset = scrollViewer.Offset;

        var imageWidth = (double)imageSize.Width;
        var imageHeight = (double)imageSize.Height;

        var contentWidth = imageWidth * oldZoom;
        var contentHeight = imageHeight * oldZoom;

        var marginX = Math.Max(0, (viewport.Width - contentWidth) / 2);
        var marginY = Math.Max(0, (viewport.Height - contentHeight) / 2);

        var contentX = (currentOffset.X + viewportPosition.X - marginX) / oldZoom;
        var contentY = (currentOffset.Y + viewportPosition.Y - marginY) / oldZoom;

        contentX = Math.Clamp(contentX, 0, imageWidth);
        contentY = Math.Clamp(contentY, 0, imageHeight);

        if (zoomInRequested)
        {
            _viewModel.ZoomInCommand.Execute(null);
        }
        else if (zoomOutRequested)
        {
            _viewModel.ZoomOutCommand.Execute(null);
        }

        var newZoom = _viewModel.ImageZoom;
        if (Math.Abs(newZoom - oldZoom) < double.Epsilon)
        {
            return;
        }

        var newContentWidth = imageWidth * newZoom;
        var newContentHeight = imageHeight * newZoom;

        var newMarginX = Math.Max(0, (viewport.Width - newContentWidth) / 2);
        var newMarginY = Math.Max(0, (viewport.Height - newContentHeight) / 2);

        var targetOffsetX = contentX * newZoom + newMarginX - viewportPosition.X;
        var targetOffsetY = contentY * newZoom + newMarginY - viewportPosition.Y;

        Dispatcher.UIThread.Post(
            () => SetScrollOffset(targetOffsetX, targetOffsetY),
            DispatcherPriority.Background);

        e.Handled = true;
    }

    private void DropZone_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is IInputElement inputElement)
        {
            inputElement.Focus();
        }

        if (_imageScrollViewer is null || _viewModel?.Image is null)
        {
            return;
        }

        var props = e.GetCurrentPoint(_imageScrollViewer).Properties;
        if (!props.IsMiddleButtonPressed)
        {
            return;
        }

        e.Pointer.Capture(_imageScrollViewer);
        _isPanning = true;
        _panStartPoint = e.GetPosition(_imageScrollViewer);
        _panStartOffset = _imageScrollViewer.Offset;
        e.Handled = true;
    }

    private void DropZone_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning || _imageScrollViewer is null || e.Pointer.Captured != _imageScrollViewer)
        {
            return;
        }

        var position = e.GetPosition(_imageScrollViewer);
        var delta = position - _panStartPoint;

        var targetX = _panStartOffset.X - delta.X;
        var targetY = _panStartOffset.Y - delta.Y;

        SetScrollOffset(targetX, targetY);
        e.Handled = true;
    }

    private void DropZone_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isPanning || _imageScrollViewer is null || e.InitialPressMouseButton != MouseButton.Middle)
        {
            return;
        }

        ReleasePan(e.Pointer);
        e.Handled = true;
    }

    private void DropZone_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_isPanning && _imageScrollViewer != null)
        {
            ReleasePan(e.Pointer);
        }
    }

    private void DropZone_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_imageScrollViewer is null || _viewModel?.Image is null)
        {
            return;
        }

        var viewport = _imageScrollViewer.Viewport;
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return;
        }

        var stepX = Math.Max(10, viewport.Width * 0.1);
        var stepY = Math.Max(10, viewport.Height * 0.1);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            stepX *= 2;
            stepY *= 2;
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            stepX *= 0.5;
            stepY *= 0.5;
        }

        var offset = _imageScrollViewer.Offset;
        var targetX = offset.X;
        var targetY = offset.Y;

        switch (e.Key)
        {
            case Key.Left:
                targetX -= stepX;
                e.Handled = true;
                break;
            case Key.Right:
                targetX += stepX;
                e.Handled = true;
                break;
            case Key.Up:
                targetY -= stepY;
                e.Handled = true;
                break;
            case Key.Down:
                targetY += stepY;
                e.Handled = true;
                break;
            default:
                return;
        }

        SetScrollOffset(targetX, targetY);
    }

    private void SetScrollOffset(double targetOffsetX, double targetOffsetY)
    {
        if (_imageScrollViewer is null)
        {
            return;
        }

        if (!double.IsFinite(targetOffsetX) || !double.IsFinite(targetOffsetY))
        {
            return;
        }

        var viewport = _imageScrollViewer.Viewport;
        var extent = _imageScrollViewer.Extent;

        var maxOffsetX = Math.Max(0, extent.Width - viewport.Width);
        var maxOffsetY = Math.Max(0, extent.Height - viewport.Height);

        var clampedX = Math.Clamp(targetOffsetX, 0, maxOffsetX);
        var clampedY = Math.Clamp(targetOffsetY, 0, maxOffsetY);

        _imageScrollViewer.Offset = new Vector(clampedX, clampedY);
    }

    private void ReleasePan(IPointer pointer)
    {
        pointer.Capture(null);
        _isPanning = false;
    }
}
