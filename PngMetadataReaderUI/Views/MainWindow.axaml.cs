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
            var dialog = new OpenFileDialog
            {
                Title = "Select PNG Image",
                Filters = [new() { Name = "PNG Images", Extensions = ["png"] }]
            };

            var result = await dialog.ShowAsync(this);
            if (result != null && result.Length > 0)
            {
                viewModel.LoadImageCommand.Execute(result[0]);
            }
        }
    }
}