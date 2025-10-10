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
            var hostInput = settings.IpAddress?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(hostInput))
            {
                await ShowMessageAsync("Verbindungstest", "Bitte geben Sie eine gültige IP-Adresse ein.", DialogType.Error);
                return;
            }

            var scheme = Uri.UriSchemeHttp;
            var port = settings.Port;
            var host = hostInput;

            if (Uri.TryCreate(hostInput, UriKind.Absolute, out var absoluteUri))
            {
                scheme = absoluteUri.Scheme;
                host = absoluteUri.Host;
                port = absoluteUri.IsDefaultPort ? port : absoluteUri.Port;
            }
            else if (hostInput.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                host = hostInput[7..];
            }
            else if (hostInput.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                host = hostInput[8..];
                scheme = Uri.UriSchemeHttps;
            }
            else
            {
                var colonIndex = host.IndexOf(':');
                if (colonIndex >= 0 && colonIndex < host.Length - 1 &&
                    int.TryParse(host[(colonIndex + 1)..], out var inlinePort))
                {
                    port = inlinePort;
                    host = host[..colonIndex];
                }
            }

            host = host.Trim().TrimEnd('/');

            if (string.IsNullOrWhiteSpace(host))
            {
                await ShowMessageAsync("Verbindungstest", "Die IP-Adresse konnte nicht interpretiert werden.", DialogType.Error);
                return;
            }

            var uriBuilder = new UriBuilder
            {
                Scheme = scheme,
                Host = host,
                Port = port,
                Path = "/v1/models"
            };

            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };

            using var response = await httpClient.GetAsync(uriBuilder.Uri);
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
