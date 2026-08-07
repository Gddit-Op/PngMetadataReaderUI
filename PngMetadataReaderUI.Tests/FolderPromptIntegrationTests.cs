using PngMetadataReaderUI.ViewModels;

namespace PngMetadataReaderUI.Tests;

public class FolderPromptIntegrationTests
{
    [Fact]
    public async Task WritesAllImagePromptsIntoOneFolderFile()
    {
        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"PngMetadataReaderUI-folder-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var sourcePath = Path.Combine(AppContext.BaseDirectory, "Sample", "Example.png");
            var firstImagePath = Path.Combine(tempDirectory, "01.png");
            var secondImagePath = Path.Combine(tempDirectory, "02.png");
            File.Copy(sourcePath, firstImagePath);
            File.Copy(sourcePath, secondImagePath);
            File.WriteAllText(Path.Combine(tempDirectory, "01_prompts.txt"), "stale");

            var viewModel = new MainWindowViewModel();
            await viewModel.ExtractFolderAsync(tempDirectory);

            var promptsPath = Path.Combine(tempDirectory, "prompts.txt");
            Assert.True(File.Exists(promptsPath));

            var promptsText = File.ReadAllText(promptsPath);
            Assert.Equal(2, CountOccurrences(promptsText, "positive:"));
            Assert.Equal(2, CountOccurrences(promptsText, "negative:"));
            Assert.Equal(2, CountOccurrences(promptsText, "----"));
            Assert.DoesNotContain("Positive Prompt:", promptsText);
            Assert.False(File.Exists(Path.Combine(tempDirectory, "01_prompts.txt")));
            Assert.False(File.Exists(Path.Combine(tempDirectory, "02_prompts.txt")));
            Assert.Contains("Prompts aus 2 Bildern in prompts.txt gespeichert.", viewModel.StatusMessage);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static int CountOccurrences(string value, string searchValue)
    {
        var count = 0;
        var startIndex = 0;
        while ((startIndex = value.IndexOf(searchValue, startIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += searchValue.Length;
        }

        return count;
    }
}
