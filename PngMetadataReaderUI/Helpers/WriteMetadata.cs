using MetadataExtractor;
using PngMetadataReaderUI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PngMetadataReaderUI.Helpers;

internal static class WriteMetadata
{
    private const string WorkflowKeyword = "workflow";

    public static MetadataExtractionResult WriteMetadataToTxt(this string imagePath, string keyword = "prompt")
    {
        if (!File.Exists(imagePath))
        {
            var message = $"Datei wurde nicht gefunden: {imagePath}";
            Console.WriteLine(message);
            return MetadataExtractionResult.Error(message);
        }

        string outputPath = Path.ChangeExtension(imagePath, ".txt");

        try
        {
            var directories = ImageMetadataReader.ReadMetadata(imagePath);
            var textualTags = directories
                .SelectMany(x => x.Tags)
                .Where(tag => !string.IsNullOrWhiteSpace(tag.Description))
                .ToList();

            if (textualTags.Count == 0)
            {
                DeleteOutputFileIfExists(outputPath);
                return MetadataExtractionResult.NoMetadata("Keine Metadaten im Bild gefunden.");
            }

            var builder = new StringBuilder();

            var promptTags = textualTags
                .Where(tag => ContainsKeyword(tag.Description!, keyword))
                .ToList();

            foreach (var metadataTag in promptTags)
            {
                var payload = ExtractKeywordPayload(metadataTag.Description!, keyword);
                builder.AppendLine(
                    $"Raw-Output '{keyword}' ({metadataTag.DirectoryName}/{metadataTag.Name}): {payload}");
            }

            var workflowResult = TryWriteWorkflowJson(textualTags, imagePath);
            var workflowFailed = false;
            string? workflowMessage = null;
            if (workflowResult.Exists)
            {
                if (!string.IsNullOrEmpty(workflowResult.Message))
                {
                    builder.AppendLine(workflowResult.Message);
                }

                workflowFailed = !workflowResult.IsSuccess;
                workflowMessage = workflowResult.Message;
            }

            var promptTag = promptTags.FirstOrDefault();
            if (promptTag == null)
            {
                DeleteOutputFileIfExists(outputPath);
                return MetadataExtractionResult.NoMetadata($"Kein '{keyword}'-Eintrag im Bild gefunden.");
            }

            var promptPayload = ExtractKeywordPayload(promptTag.Description!, keyword);
            var promptJson = promptPayload.ExtractFirstJson();
            var pipeline = JsonSerializer.Deserialize(promptJson, PipelineJsonContext.Default.Pipeline);
            if (pipeline == null)
            {
                builder.AppendLine("Failed to deserialize pipeline JSON.");
                File.WriteAllText(outputPath, builder.ToString());
                return MetadataExtractionResult.Error("Die Prompt-Metadaten konnten nicht verarbeitet werden.");
            }

            foreach (var kvp in pipeline)
            {
                builder.AppendLine($"Node ID = {kvp.Key}");
                builder.AppendLine($"  class_type = {kvp.Value.ClassType}");
                builder.AppendLine($"  title      = {kvp.Value.Meta?.Title}");
                builder.AppendLine("  inputs:");
                foreach (var input in kvp.Value.Inputs)
                {
                    builder.AppendLine($"    {input.Key} = {input.Value.GetRawText()}");
                }
                builder.AppendLine();
            }

            var baseMessage = $"Metadaten gespeichert in {Path.GetFileName(outputPath)}.";
            if (workflowResult.Exists && workflowResult.IsSuccess && !string.IsNullOrEmpty(workflowResult.Message))
            {
                baseMessage += $" {workflowResult.Message}";
            }

            if (workflowFailed)
            {
                var errorMessage = string.IsNullOrEmpty(workflowMessage)
                    ? "Workflow JSON konnte nicht gespeichert werden."
                    : workflowMessage;
                return MetadataExtractionResult.Error(errorMessage);
            }

            File.WriteAllText(outputPath, builder.ToString());
            return MetadataExtractionResult.Success(baseMessage);
        }
        catch (Exception ex)
        {
            File.AppendAllText(outputPath, $"{Environment.NewLine}Error: {ex.Message}{Environment.NewLine}");
            return MetadataExtractionResult.Error($"Fehler beim Lesen der Metadaten: {ex.Message}");
        }
    }

    private static WorkflowSaveResult TryWriteWorkflowJson(IEnumerable<Tag> tags, string imagePath)
    {
        var workflowTag = tags.FirstOrDefault(tag =>
            !string.IsNullOrWhiteSpace(tag.Description) &&
            ContainsKeyword(tag.Description!, WorkflowKeyword));
        if (workflowTag == null)
        {
            return WorkflowSaveResult.NotPresent();
        }

        try
        {
            var workflowPayload = ExtractKeywordPayload(workflowTag.Description!, WorkflowKeyword);
            var workflowJson = workflowPayload.ExtractFirstJson();
            using var document = JsonDocument.Parse(workflowJson);
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
            {
                document.WriteTo(writer);
            }

            var formattedWorkflowJson = Encoding.UTF8.GetString(buffer.ToArray());
            var workflowOutputPath = Path.Combine(
                Path.GetDirectoryName(imagePath) ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(imagePath)}_workflow.json");

            File.WriteAllText(workflowOutputPath, formattedWorkflowJson + Environment.NewLine);
            return WorkflowSaveResult.Success($"Workflow JSON gespeichert als {Path.GetFileName(workflowOutputPath)}.");
        }
        catch (Exception ex)
        {
            return WorkflowSaveResult.Failure($"Workflow JSON konnte nicht gespeichert werden: {ex.Message}");
        }
    }

    private static bool ContainsKeyword(string description, string keyword) =>
        description.Contains(keyword, StringComparison.OrdinalIgnoreCase);

    private static string ExtractKeywordPayload(string description, string keyword)
    {
        var index = description.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return description.Trim();
        }

        return description[index..].Trim();
    }

    private static void DeleteOutputFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private readonly struct WorkflowSaveResult
    {
        private WorkflowSaveResult(bool exists, bool isSuccess, string message)
        {
            Exists = exists;
            IsSuccess = isSuccess;
            Message = message;
        }

        public bool Exists { get; }
        public bool IsSuccess { get; }
        public string Message { get; }

        public static WorkflowSaveResult NotPresent() => new(false, false, string.Empty);
        public static WorkflowSaveResult Success(string message) => new(true, true, message);
        public static WorkflowSaveResult Failure(string message) => new(true, false, message);
    }
}
