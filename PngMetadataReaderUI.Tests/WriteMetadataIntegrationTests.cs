using PngMetadataReaderUI.Helpers;
using PngMetadataReaderUI.Models;

namespace PngMetadataReaderUI.Tests;

public class WriteMetadataIntegrationTests
{
    [Fact]
    public void WritesExtractedPromptsNextToImage()
    {
        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"PngMetadataReaderUI-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var sourcePath = Path.Combine(AppContext.BaseDirectory, "Sample", "Example.png");
            var imagePath = Path.Combine(tempDirectory, "Example.png");
            File.Copy(sourcePath, imagePath);

            var result = imagePath.WriteMetadataToTxt();

            Assert.Equal(MetadataExtractionStatus.Success, result.Status);
            var promptsPath = Path.Combine(tempDirectory, "Example_prompts.txt");
            Assert.True(File.Exists(promptsPath));

            var prompts = File.ReadAllText(promptsPath);
            Assert.StartsWith("positive:", prompts);
            Assert.Contains("a bottle with a beautiful rainbow galaxy inside it", prompts);
            Assert.Contains($"{Environment.NewLine}{Environment.NewLine}negative:{Environment.NewLine}----", prompts);
            Assert.DoesNotContain("Positive Prompt:", prompts);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
