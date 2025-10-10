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
    public static MetadataExtractionResult WriteMetadataToTxt(this string pngPath, string keyword = "prompt")
    {
        if (!File.Exists(pngPath))
        {
            var message = $"Datei wurde nicht gefunden: {pngPath}";
            Console.WriteLine(message);
            return MetadataExtractionResult.Error(message);
        }

        string outputPath = Path.ChangeExtension(pngPath, ".txt");

        try
        {
            var directories = ImageMetadataReader.ReadMetadata(pngPath);
            var textDirs = directories.Where(x => x.Name == "PNG-tEXt").ToList();
            if (textDirs.Count == 0)
            {
                File.AppendAllText(outputPath, "No textual metadata found in PNG." + Environment.NewLine);
                return MetadataExtractionResult.NoMetadata("Keine Metadaten im PNG gefunden.");
            }

            var textTags = textDirs
                .SelectMany(x => x.Tags)
                .Where(tag => string.IsNullOrWhiteSpace(tag.Description) == false)
                .ToList();

            if (textTags.Count == 0)
            {
                File.AppendAllText(outputPath, "No textual metadata found in PNG." + Environment.NewLine);
                return MetadataExtractionResult.NoMetadata("Keine Metadaten im PNG gefunden.");
            }

            foreach (var metadataTag in textTags)
            {
                File.AppendAllText(outputPath, $"Raw-Output '{keyword}': {metadataTag.Description}{Environment.NewLine}");
            }

            var workflowResult = TryWriteWorkflowJson(textTags, pngPath);
            var workflowFailed = false;
            string? workflowMessage = null;
            if (workflowResult.Exists)
            {
                if (!string.IsNullOrEmpty(workflowResult.Message))
                {
                    File.AppendAllText(outputPath, workflowResult.Message + Environment.NewLine);
                }

                workflowFailed = !workflowResult.IsSuccess;
                workflowMessage = workflowResult.Message;
            }

            var promptTag = textTags.FirstOrDefault(x =>
                x.Description != null && x.Description.StartsWith(keyword, StringComparison.OrdinalIgnoreCase));
            if (promptTag == null || string.IsNullOrWhiteSpace(promptTag.Description))
            {
                File.AppendAllText(outputPath, $"{Environment.NewLine}No '{keyword}' chunk found in PNG metadata.{Environment.NewLine}");
                return MetadataExtractionResult.NoMetadata($"Kein '{keyword}'-Eintrag im PNG gefunden.");
            }

            var promptJson = promptTag.Description.ExtractFirstJson();
            var pipeline = JsonSerializer.Deserialize(promptJson, PipelineJsonContext.Default.Pipeline);
            if (pipeline == null)
            {
                File.AppendAllText(outputPath, "Failed to deserialize pipeline JSON." + Environment.NewLine);
                return MetadataExtractionResult.Error("Die Prompt-Metadaten konnten nicht verarbeitet werden.");
            }

            using var writer = new StreamWriter(outputPath, append: true);
            foreach (var kvp in pipeline)
            {
                writer.WriteLine($"Node ID = {kvp.Key}");
                writer.WriteLine($"  class_type = {kvp.Value.ClassType}");
                writer.WriteLine($"  title      = {kvp.Value.Meta?.Title}");
                writer.WriteLine("  inputs:");
                foreach (var input in kvp.Value.Inputs)
                {
                    writer.WriteLine($"    {input.Key} = {input.Value.GetRawText()}");
                }
                writer.WriteLine();
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

            return MetadataExtractionResult.Success(baseMessage);
        }
        catch (Exception ex)
        {
            File.AppendAllText(outputPath, $"{Environment.NewLine}Error: {ex.Message}{Environment.NewLine}");
            return MetadataExtractionResult.Error($"Fehler beim Lesen der Metadaten: {ex.Message}");
        }
    }

    private static WorkflowSaveResult TryWriteWorkflowJson(IEnumerable<Tag> tags, string pngPath)
    {
        var workflowTag = tags.FirstOrDefault(x =>
            x.Description != null && x.Description.StartsWith("workflow", StringComparison.OrdinalIgnoreCase));
        if (workflowTag == null || string.IsNullOrWhiteSpace(workflowTag.Description))
        {
            return WorkflowSaveResult.NotPresent();
        }

        try
        {
            var workflowJson = workflowTag.Description.ExtractFirstJson();
            using var document = JsonDocument.Parse(workflowJson);
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
            {
                document.WriteTo(writer);
            }

            var formattedWorkflowJson = Encoding.UTF8.GetString(buffer.ToArray());
            var workflowOutputPath = Path.Combine(
                Path.GetDirectoryName(pngPath) ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(pngPath)}_workflow.json");

            File.WriteAllText(workflowOutputPath, formattedWorkflowJson + Environment.NewLine);
            return WorkflowSaveResult.Success($"Workflow JSON gespeichert als {Path.GetFileName(workflowOutputPath)}.");
        }
        catch (Exception ex)
        {
            return WorkflowSaveResult.Failure($"Workflow JSON konnte nicht gespeichert werden: {ex.Message}");
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
