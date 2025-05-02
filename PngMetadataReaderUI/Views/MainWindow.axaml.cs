using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using PngMetadataReaderUI.ViewModels;
using System;
using System.Linq;

namespace PngMetadataReaderUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        // Set up drag and drop for the window
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        // Only allow file drops with PNG images
        //if (e.Data.Contains(DataFormats.FileNames))
        //{
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
        //}
        //else
        //{
        //    e.DragEffects = DragDropEffects.None;
        //}
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
}