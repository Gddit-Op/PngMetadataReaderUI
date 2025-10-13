using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PngMetadataReaderUI.Helpers;
using PngMetadataReaderUI.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PngMetadataReaderUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private Bitmap? _image;

    [ObservableProperty]
    private string _statusMessage = "Drag and drop an image (PNG/JPG/WebP) here";

    [ObservableProperty]
    private string? _browseFilePath;

    [ObservableProperty]
    private double _imageZoom = 1.0;

    private bool _isGeneratingPrompt;

    private const double MinZoom = 0.2;
    private const double MaxZoom = 5.0;
    private const double ZoomStep = 0.2;
    private const double ZoomEpsilon = 0.001;

    private double _fitZoom = 1.0;
    private bool _userAdjustedZoom;
    private double _viewportWidth;
    private double _viewportHeight;

    private const string PromptSystemInstruction =
        "You are an AI assistant specialized in generating detailed and creative image prompts for AI image generation. " +
        "Your task is to expand a given user prompt into a well-structured, vivid, " +
        "and highly descriptive prompt while ensuring that all terms from the original prompt are included. " +
        "Enhance the visual quality and artistic impact by adding relevant details, " +
        "but do not omit or alter any key elements provided by the user. " +
        "Follow the given instructions or guidelines and respond only with the refined prompt.";

    public event EventHandler<DialogRequest>? DialogRequested;

    public MainWindowViewModel()
    {
        UpdateZoomCommandStates();
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

            if (!ImageFormatHelper.IsSupportedImageFile(filePath))
            {
                var dialogMessage =
                    $"Bitte waehlen Sie eine Bilddatei im Format {ImageFormatHelper.GetSupportedExtensionsDisplay()} aus.";
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

    [RelayCommand(CanExecute = nameof(CanGeneratePrompt))]
    private async Task GeneratePromptAsync()
    {
        if (_isGeneratingPrompt)
        {
            return;
        }

        if (string.IsNullOrEmpty(BrowseFilePath) || !File.Exists(BrowseFilePath))
        {
            var message =
                $"Bitte laden Sie zuerst ein Bild im Format {ImageFormatHelper.GetSupportedExtensionsDisplay()}.";
            StatusMessage = message;
            RequestDialog(DialogType.Error, "Kein Bild geladen", message);
            return;
        }

        if (!ImageFormatHelper.IsSupportedImageFile(BrowseFilePath))
        {
            var message =
                $"Das ausgewaehlte Dateiformat wird nicht unterstuetzt. Erlaubt sind {ImageFormatHelper.GetSupportedExtensionsDisplay()}.";
            StatusMessage = message;
            RequestDialog(DialogType.Error, "Falsches Dateiformat", message);
            return;
        }

        var settings = SettingsService.Load();
        if (string.IsNullOrWhiteSpace(settings.ModelId))
        {
            const string message = "Bitte hinterlegen Sie eine Modell-ID in den Einstellungen.";
            StatusMessage = message;
            RequestDialog(DialogType.Error, "Modell-ID fehlt", message);
            return;
        }

        if (!LmStudioEndpointHelper.TryBuildBaseUri(settings, out var baseUri, out var endpointError) || baseUri == null)
        {
            var message = endpointError ?? "Die Serveradresse konnte nicht interpretiert werden.";
            StatusMessage = message;
            RequestDialog(DialogType.Error, "Verbindungsfehler", message);
            return;
        }

        try
        {
            _isGeneratingPrompt = true;
            GeneratePromptCommand.NotifyCanExecuteChanged();

            StatusMessage = "Bildbeschreibung wird erstellt...";

            if (!ImageFormatHelper.TryEncodeToBase64(BrowseFilePath, out var base64Image, out var mimeType, out var encodeError))
            {
                var message = string.IsNullOrWhiteSpace(encodeError)
                    ? "Das Bild konnte nicht verarbeitet werden."
                    : $"Das Bild konnte nicht verarbeitet werden: {encodeError}";
                StatusMessage = message;
                RequestDialog(DialogType.Error, "Bildverarbeitung fehlgeschlagen", message);
                return;
            }

            var payload = BuildChatCompletionPayload(settings, base64Image, mimeType);

            using var httpClient = new HttpClient
            {
                BaseAddress = baseUri,
                Timeout = TimeSpan.FromSeconds(30)
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            using var response = await httpClient.SendAsync(requestMessage);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var statusLine = $"{(int)response.StatusCode} {response.ReasonPhrase}".Trim();
                var errorPreview = Truncate(responseText, 300);
                var message = $"Anfrage fehlgeschlagen: {statusLine}.{Environment.NewLine}{errorPreview}";
                StatusMessage = "Bildbeschreibung fehlgeschlagen.";
                RequestDialog(DialogType.Error, "API-Fehler", message);
                return;
            }

            var prompt = ExtractPrompt(responseText);
            if (string.IsNullOrWhiteSpace(prompt))
            {
                const string message = "Die Antwort der API konnte nicht ausgewertet werden.";
                StatusMessage = message;
                RequestDialog(DialogType.Error, "Unbekannte Antwort", message);
                return;
            }

            var outputDirectory = Path.GetDirectoryName(BrowseFilePath);
            var outputFileName = $"{Path.GetFileNameWithoutExtension(BrowseFilePath)}_prompt.txt";
            var outputPath = Path.Combine(outputDirectory ?? AppContext.BaseDirectory, outputFileName);

            await File.WriteAllTextAsync(outputPath, prompt, Encoding.UTF8);

            StatusMessage = $"Bildbeschreibung gespeichert: {outputPath}";
            RequestDialog(DialogType.Information, "Bildbeschreibung erstellt", $"Die Bildbeschreibung wurde gespeichert.");
        }
        catch (Exception ex)
        {
            var message = $"Fehler beim Erstellen der Bildbeschreibung: {ex.Message}";
            StatusMessage = message;
            RequestDialog(DialogType.Error, "Bildbeschreibung fehlgeschlagen", message);
        }
        finally
        {
            _isGeneratingPrompt = false;
            GeneratePromptCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanGeneratePrompt()
    {
        return !_isGeneratingPrompt &&
               !string.IsNullOrEmpty(BrowseFilePath) &&
               File.Exists(BrowseFilePath);
    }

    partial void OnBrowseFilePathChanged(string? value)
    {
        GeneratePromptCommand.NotifyCanExecuteChanged();
    }

    private static string BuildChatCompletionPayload(UserSettings settings, string base64Image, string mimeType)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("model", settings.ModelId);
            writer.WriteNumber("temperature", settings.Temperature);
            writer.WriteNumber("max_tokens", settings.MaxTokens);

            writer.WriteStartArray("messages");

            writer.WriteStartObject();
            writer.WriteString("role", "system");
            writer.WritePropertyName("content");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("type", "text");
            writer.WriteString("text", PromptSystemInstruction);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();

            writer.WriteStartObject();
            writer.WriteString("role", "user");
            writer.WritePropertyName("content");
            writer.WriteStartArray();

            writer.WriteStartObject();
            writer.WriteString("type", "text");
            writer.WriteString("text", "Erstelle einen detaillierten Prompt für das hochgeladene Bild zur Verwendung in Bildgenerierungsmodellen.");
            writer.WriteEndObject();

            writer.WriteStartObject();
            writer.WriteString("type", "image_url");
            writer.WritePropertyName("image_url");
            writer.WriteStartObject();
            writer.WriteString("url", $"data:{mimeType};base64,{base64Image}");
            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.WriteEndArray();
            writer.WriteEndObject();

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string? ExtractPrompt(string responseText)
    {
        using var document = JsonDocument.Parse(responseText);
        if (!document.RootElement.TryGetProperty("choices", out var choicesElement) ||
            choicesElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var choice in choicesElement.EnumerateArray())
        {
            if (!choice.TryGetProperty("message", out var messageElement))
            {
                continue;
            }

            if (!messageElement.TryGetProperty("content", out var contentElement))
            {
                continue;
            }

            switch (contentElement.ValueKind)
            {
                case JsonValueKind.String:
                    var stringContent = contentElement.GetString();
                    if (!string.IsNullOrWhiteSpace(stringContent))
                    {
                        return stringContent.Trim();
                    }
                    break;

                case JsonValueKind.Array:
                    var builder = new StringBuilder();
                    foreach (var segment in contentElement.EnumerateArray())
                    {
                        if (segment.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        if (segment.TryGetProperty("text", out var textElement) &&
                            textElement.ValueKind == JsonValueKind.String)
                        {
                            builder.Append(textElement.GetString());
                        }
                        else if (segment.TryGetProperty("type", out var typeElement) &&
                                 typeElement.ValueKind == JsonValueKind.String &&
                                 typeElement.GetString()?.Equals("text", StringComparison.OrdinalIgnoreCase) == true &&
                                 segment.TryGetProperty("content", out var nestedTextElement) &&
                                 nestedTextElement.ValueKind == JsonValueKind.String)
                        {
                            builder.Append(nestedTextElement.GetString());
                        }
                    }

                    if (builder.Length > 0)
                    {
                        return builder.ToString().Trim();
                    }

                    break;
            }
        }

        return null;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }

    private void RequestDialog(DialogType type, string title, string message)
    {
        DialogRequested?.Invoke(this, new DialogRequest(title, message, type));
    }

    [RelayCommand(CanExecute = nameof(CanZoomIn))]
    private void ZoomIn()
    {
        if (Image == null)
        {
            return;
        }

        _userAdjustedZoom = true;
        SetAbsoluteZoom(ImageZoom + ZoomStep);
    }

    [RelayCommand(CanExecute = nameof(CanZoomOut))]
    private void ZoomOut()
    {
        if (Image == null)
        {
            return;
        }

        _userAdjustedZoom = true;
        SetAbsoluteZoom(ImageZoom - ZoomStep);
    }

    [RelayCommand(CanExecute = nameof(CanResetZoom))]
    private void ResetZoom()
    {
        if (Image == null)
        {
            return;
        }

        _userAdjustedZoom = false;
        SetAbsoluteZoom(_fitZoom, clampToUserRange: false);
    }

    partial void OnImageChanged(Bitmap? value)
    {
        if (value is null)
        {
            _fitZoom = 1.0;
            _userAdjustedZoom = false;
            if (Math.Abs(ImageZoom - 1.0) > ZoomEpsilon)
            {
                ImageZoom = 1.0;
            }

            UpdateZoomCommandStates();
            return;
        }

        _userAdjustedZoom = false;
        RecalculateFitZoom();
    }

    public void UpdateViewportSize(double width, double height)
    {
        _viewportWidth = width;
        _viewportHeight = height;
        RecalculateFitZoom();
    }

    private bool CanZoomIn() => Image != null && ImageZoom < MaxZoom - ZoomEpsilon;

    private bool CanZoomOut() => Image != null && ImageZoom > MinZoom + ZoomEpsilon;

    private bool CanResetZoom() => Image != null && Math.Abs(ImageZoom - _fitZoom) > ZoomEpsilon;

    private void SetAbsoluteZoom(double target, bool clampToUserRange = true)
    {
        double clamped;
        if (clampToUserRange)
        {
            clamped = Math.Clamp(target, MinZoom, MaxZoom);
        }
        else
        {
            clamped = Math.Clamp(target, 0.01, MaxZoom);
        }

        if (Math.Abs(ImageZoom - clamped) < ZoomEpsilon)
        {
            UpdateZoomCommandStates();
            return;
        }

        ImageZoom = Math.Round(clamped, 3);
        UpdateZoomCommandStates();
    }

    private void RecalculateFitZoom()
    {
        if (Image == null || _viewportWidth <= 0 || _viewportHeight <= 0)
        {
            UpdateZoomCommandStates();
            return;
        }

        var size = Image.Size;
        if (size.Width <= 0 || size.Height <= 0)
        {
            UpdateZoomCommandStates();
            return;
        }

        var fit = Math.Min(_viewportWidth / size.Width, _viewportHeight / size.Height);
        if (double.IsNaN(fit) || double.IsInfinity(fit) || fit <= 0)
        {
            fit = 1.0;
        }

        _fitZoom = Math.Clamp(fit, 0.01, MaxZoom);

        if (!_userAdjustedZoom)
        {
            SetAbsoluteZoom(_fitZoom, clampToUserRange: false);
        }
        else
        {
            UpdateZoomCommandStates();
        }
    }

    private void UpdateZoomCommandStates()
    {
        ZoomInCommand.NotifyCanExecuteChanged();
        ZoomOutCommand.NotifyCanExecuteChanged();
        ResetZoomCommand.NotifyCanExecuteChanged();
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
