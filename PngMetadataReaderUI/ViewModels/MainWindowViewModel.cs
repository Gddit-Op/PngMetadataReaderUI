using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PngMetadataReaderUI.Helpers;
using PngMetadataReaderUI.Models;
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

    public event EventHandler<DialogRequest>? DialogRequested;

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
                var fileName = GetDisplayName(filePath);
                var dialogMessage = $"Die Datei \"{fileName}\" wurde nicht gefunden.";
                StatusMessage = dialogMessage;
                RequestDialog(DialogType.Error, "Datei nicht gefunden", dialogMessage);
                return;
            }

            if (!filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                const string dialogMessage = "Bitte wählen Sie eine PNG-Datei aus.";
                StatusMessage = dialogMessage;
                RequestDialog(DialogType.Error, "Falsches Dateiformat", dialogMessage);
                return;
            }

            BrowseFilePath = filePath;

            Image?.Dispose();

            Image = new Bitmap(filePath);
            StatusMessage = $"Image loaded: {Path.GetFileName(filePath)}";

            var extractionResult = filePath.WriteMetadataToTxt();
            StatusMessage = extractionResult.Message;

            switch (extractionResult.Status)
            {
                case MetadataExtractionStatus.NoMetadata:
                    RequestDialog(DialogType.Information, "Keine Metadaten gefunden", extractionResult.Message);
                    break;
                case MetadataExtractionStatus.Error:
                    RequestDialog(DialogType.Error, "Fehler bei der Metadatenanalyse", extractionResult.Message);
                    break;
            }
        }
        catch (Exception ex)
        {
            var message = $"Error loading image: {ex.Message}";
            StatusMessage = message;
            RequestDialog(DialogType.Error, "Fehler beim Laden des Bildes", message);
        }
    }

    private void RequestDialog(DialogType type, string title, string message)
    {
        DialogRequested?.Invoke(this, new DialogRequest(title, message, type));
    }

    private static string GetDisplayName(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        try
        {
            var name = Path.GetFileName(path);
            return string.IsNullOrEmpty(name) ? path : name;
        }
        catch (Exception)
        {
            return path;
        }
    }
}
