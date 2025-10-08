namespace PngMetadataReaderUI.Models;

public enum DialogType
{
    Information,
    Error
}

public sealed record DialogRequest(string Title, string Message, DialogType Type);
