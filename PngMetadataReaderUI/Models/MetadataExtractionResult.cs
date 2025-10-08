namespace PngMetadataReaderUI.Models;

public enum MetadataExtractionStatus
{
    Success,
    NoMetadata,
    Error
}

public sealed record MetadataExtractionResult(MetadataExtractionStatus Status, string Message)
{
    public static MetadataExtractionResult Success(string message) =>
        new(MetadataExtractionStatus.Success, message);

    public static MetadataExtractionResult NoMetadata(string message) =>
        new(MetadataExtractionStatus.NoMetadata, message);

    public static MetadataExtractionResult Error(string message) =>
        new(MetadataExtractionStatus.Error, message);
}
