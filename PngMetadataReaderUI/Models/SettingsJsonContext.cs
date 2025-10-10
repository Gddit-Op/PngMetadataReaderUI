using System.Text.Json.Serialization;

namespace PngMetadataReaderUI.Models;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UserSettings))]
internal partial class SettingsJsonContext : JsonSerializerContext
{
}
