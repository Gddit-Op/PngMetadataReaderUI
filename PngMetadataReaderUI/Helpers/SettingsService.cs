using PngMetadataReaderUI.Models;
using System;
using System.IO;
using System.Text.Json;

namespace PngMetadataReaderUI.Helpers;

internal static class SettingsService
{
    private const string SettingsFileName = "settings.json";

    private static string SettingsDirectory
    {
        get
        {
            var processPath = Environment.ProcessPath;
            var directory = processPath is null
                ? AppContext.BaseDirectory
                : Path.GetDirectoryName(processPath);

            return string.IsNullOrWhiteSpace(directory)
                ? AppContext.BaseDirectory
                : directory!;
        }
    }

    private static string GetSettingsFilePath()
    {
        return Path.Combine(SettingsDirectory, SettingsFileName);
    }

    public static UserSettings Load()
    {
        try
        {
            var path = GetSettingsFilePath();
            if (!File.Exists(path))
            {
                return UserSettings.CreateDefault();
            }

            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.UserSettings);
            if (settings == null)
            {
                return UserSettings.CreateDefault();
            }

            settings.EnsureValidRanges();
            return settings;
        }
        catch
        {
            return UserSettings.CreateDefault();
        }
    }

    public static bool TrySave(UserSettings settings, out string? errorMessage)
    {
        try
        {
            settings.EnsureValidRanges();
            var path = GetSettingsFilePath();
            var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.UserSettings);
            File.WriteAllText(path, json);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}
