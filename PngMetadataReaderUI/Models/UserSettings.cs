using System;

namespace PngMetadataReaderUI.Models;

public sealed class UserSettings
{
    public string IpAddress { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 1234;

    public double Temperature { get; set; } = 0.7;

    public int MaxTokens { get; set; } = 600;

    public string ModelId { get; set; } = "lfm2-vl-1.6b";

    public static UserSettings CreateDefault() => new();

    public void EnsureValidRanges()
    {
        if (Port is < 1 or > 65535)
        {
            Port = Math.Clamp(Port, 1, 65535);
        }

        if (Temperature is < 0 or > 2)
        {
            Temperature = Math.Clamp(Temperature, 0, 2);
        }

        if (MaxTokens < 1)
        {
            MaxTokens = 1;
        }

        ModelId ??= string.Empty;
    }
}
