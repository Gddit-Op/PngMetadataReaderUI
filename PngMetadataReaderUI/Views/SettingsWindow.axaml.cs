using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using PngMetadataReaderUI.Helpers;
using PngMetadataReaderUI.Models;
using PngMetadataReaderUI.ViewModels;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PngMetadataReaderUI.Views;

public partial class SettingsWindow : Window
{
    private bool _isTestingConnection;

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

    private async void TestConnectionButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isTestingConnection)
        {
            return;
        }

        if (DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        if (sender is Button button)
        {
            button.IsEnabled = false;
        }

        _isTestingConnection = true;

        try
        {
            var settings = viewModel.ToSettings();
            if (!LmStudioEndpointHelper.TryBuildBaseUri(settings, out var baseUri, out var parseError) || baseUri == null)
            {
                await ShowMessageAsync("Verbindungstest", parseError ?? "Bitte geben Sie eine gültige IP-Adresse ein.", DialogType.Error);
                return;
            }

            using var httpClient = new HttpClient
            {
                BaseAddress = baseUri,
                Timeout = TimeSpan.FromSeconds(10)
            };

            using var response = await httpClient.GetAsync("v1/models");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var statusLine = $"{(int)response.StatusCode} {response.ReasonPhrase}".Trim();
                await ShowMessageAsync("Verbindung fehlgeschlagen", $"Server antwortete mit Status {statusLine}.", DialogType.Error);
                return;
            }

            var message = "Verbindung erfolgreich.";
            if (!string.IsNullOrWhiteSpace(settings.ModelId))
            {
                try
                {
                    message = ModelExists(content, settings.ModelId)
                        ? $"Verbindung erfolgreich. Modell \"{settings.ModelId}\" gefunden."
                        : $"Verbindung erfolgreich, aber Modell \"{settings.ModelId}\" wurde nicht gefunden.";
                }
                catch (JsonException)
                {
                    message = "Verbindung erfolgreich, aber die Antwort konnte nicht ausgewertet werden.";
                }
            }

            await ShowMessageAsync("Verbindungstest", message, DialogType.Information);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Verbindung fehlgeschlagen", ex.Message, DialogType.Error);
        }
        finally
        {
            _isTestingConnection = false;
            if (sender is Button buttonSender)
            {
                buttonSender.IsEnabled = true;
            }
        }
    }

    private static bool ModelExists(string jsonPayload, string modelId)
    {
        using var document = JsonDocument.Parse(jsonPayload);
        if (!document.RootElement.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var item in dataElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (item.TryGetProperty("id", out var idElement) &&
                idElement.ValueKind == JsonValueKind.String &&
                string.Equals(idElement.GetString(), modelId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private Task ShowMessageAsync(string title, string message, DialogType type)
    {
        return MessageBoxService.ShowAsync(this, new DialogRequest(title, message, type));
    }
}
