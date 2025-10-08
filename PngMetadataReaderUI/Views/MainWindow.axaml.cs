using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
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
        var fileNames = e.Data.GetFiles();
        if (fileNames != null &&
            fileNames.Any(f => f.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            var fileNames = e.Data.GetFiles()?.ToList();
            if (fileNames != null && fileNames.Count > 0)
            {
                var pngFile = fileNames.FirstOrDefault(f =>
                    f.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

                if (pngFile != null)
                {
                    viewModel.LoadImageCommand.Execute(pngFile.TryGetLocalPath());
                }
                else
                {
                    viewModel.StatusMessage = "Please drop a PNG file.";
                }
            }
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

    private async void OnDialogRequested(object? sender, DialogRequest request)
    {
        var messageBlock = new TextBlock
        {
            Text = request.Message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 420
        };

        if (request.Type == DialogType.Error)
        {
            messageBlock.Foreground = Brushes.Red;
        }

        var okButton = new Button
        {
            Content = "OK",
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Right,
            IsDefault = true
        };

        var dialog = new Window
        {
            Title = request.Title,
            CanResize = false,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 12,
                Children =
                {
                    messageBlock,
                    okButton
                }
            }
        };

        void CloseHandler(object? _, Avalonia.Interactivity.RoutedEventArgs _1) => dialog.Close();

        okButton.Click += CloseHandler;

        await dialog.ShowDialog(this);

        okButton.Click -= CloseHandler;
    }
}
