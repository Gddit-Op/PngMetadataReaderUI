using MetadataExtractor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PngMetadataReaderUI.Helpers;

internal static class WriteMetadata
{
    public static void WriteMetadataToTxt(this string pngpath, string keyword = "prompt")
    {
        if (!File.Exists(pngpath))
        {
            Console.WriteLine($"File not found: {pngpath}");
            return;
        }

        // Determine output text file path
        string outputPath = Path.ChangeExtension(pngpath, ".txt");

        try
        {
            // Reads all metadata directories from the file
            var directories = ImageMetadataReader.ReadMetadata(pngpath);
            // PNG textual chunks appear in PngTextDirectory
            var textDirs = directories.Where(x => x.Name == "PNG-tEXt").ToList();
            if (textDirs.Count == 0)
            {
                File.AppendAllText(outputPath, "No textual metadata found in PNG.\r\n");
                //Console.WriteLine($"Output written to {outputPath}");
                return;
            }

            var textTags = textDirs
                .SelectMany(x => x.Tags)
                .Where(tag => string.IsNullOrWhiteSpace(tag.Description) == false)
                .ToList();
            if (textTags.Count == 0)
            {
                File.AppendAllText(outputPath, "No textual metadata found in PNG.\r\n");
                return;
            }

            foreach (var metadataTag in textTags)
            {
                File.AppendAllText(outputPath, $"Raw-Output '{keyword}': {metadataTag.Description}\r\n");
            }

            TryWriteWorkflowJson(textTags, pngpath, outputPath);

            // Find the chunk with the specified keyword
            var promptTag = textTags.FirstOrDefault(x => x.Description != null && x.Description.StartsWith(keyword));
            if (promptTag == null || string.IsNullOrWhiteSpace(promptTag.Description))
            {
                File.AppendAllText(outputPath, $"\r\nNo '{keyword}' chunk found in PNG metadata.\r\n");
                //Console.WriteLine($"Output written to {outputPath}");
                return;
            }

            var promptJson = promptTag.Description.ExtractFirstJson();
            // Deserialize JSON from the metadata chunk
            var pipeline = JsonSerializer.Deserialize<Pipeline>(promptJson, PipelineJsonContext.Default.Options);
            if (pipeline == null)
            {
                File.AppendAllText(outputPath, "Failed to deserialize pipeline JSON.\r\n");
                //Console.WriteLine($"Output written to {outputPath}");
                return;
            }

            // Write output to text file
            using var writer = new StreamWriter(outputPath, append: true);
            foreach (var kvp in pipeline)
            {
                writer.WriteLine($"Node ID = {kvp.Key}");
                writer.WriteLine($"  class_type = {kvp.Value.ClassType}");
                writer.WriteLine($"  title      = {kvp.Value.Meta?.Title}");
                writer.WriteLine("  inputs:");
                foreach (var inp in kvp.Value.Inputs)
                {
                    writer.WriteLine($"    {inp.Key} = {inp.Value.GetRawText()}");
                }
                writer.WriteLine();
            }

            //Console.WriteLine($"Output written to {outputPath}");
        }
        catch (Exception ex)
        {
            File.AppendAllText(outputPath, $"\r\nError: {ex.Message}\r\n");
            //Console.WriteLine($"Error occurred. Details written to {outputPath}");
        }
    }

    private static void TryWriteWorkflowJson(IEnumerable<Tag> tags, string pngPath, string outputPath)
    {
        var workflowTag = tags.FirstOrDefault(x => x.Description != null && x.Description.StartsWith("workflow", StringComparison.OrdinalIgnoreCase));
        if (workflowTag == null || string.IsNullOrWhiteSpace(workflowTag.Description))
        {
            return;
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

            var workflowOutputPath = Path.Combine(Path.GetDirectoryName(pngPath) ?? string.Empty, $"{Path.GetFileNameWithoutExtension(pngPath)}_workflow.json");
            File.WriteAllText(workflowOutputPath, formattedWorkflowJson + Environment.NewLine);
            File.AppendAllText(outputPath, $"Workflow JSON saved to {Path.GetFileName(workflowOutputPath)}\r\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText(outputPath, $"Failed to save workflow JSON: {ex.Message}\r\n");
        }
    }
}
