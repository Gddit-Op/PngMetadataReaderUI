using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;

namespace PngMetadataReaderUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private Bitmap? _image;

    [ObservableProperty]
    private string _statusMessage = "Drag and drop a PNG image here";

    [ObservableProperty]
    private string? _browseFilePath;

    public MainWindowViewModel()
    {
    }

    [RelayCommand]
    private void LoadImage(string? filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            if (!File.Exists(filePath))
            {
                StatusMessage = "File not found!";
                return;
            }

            if (!filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Only PNG files are supported!";
                return;
            }

            // Dispose of the previous image if it exists
            Image?.Dispose();

            // Load the new image
            Image = new Bitmap(filePath);
            StatusMessage = $"Image loaded: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading image: {ex.Message}";
        }
    }
}