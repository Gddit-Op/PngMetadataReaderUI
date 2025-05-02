using MetadataExtractor;
using System;
using System.IO;
using System.Linq;
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
            var textDirs = directories.Where(x => x.Name == "PNG-tEXt");
            if (textDirs.Any() == false)
            {
                File.AppendAllText(outputPath, "No textual metadata found in PNG.\r\n");
                //Console.WriteLine($"Output written to {outputPath}");
                return;
            }

            foreach (var dir in textDirs.SelectMany(x => x.Tags))
            {
                File.AppendAllText(outputPath, $"Raw-Output '{keyword}': {dir.Description}\r\n");
            }

            // Find the chunk with the specified keyword
            var promptTag = textDirs.SelectMany(x => x.Tags).FirstOrDefault(x => x.Description != null && x.Description.StartsWith(keyword));
            if (promptTag == null || string.IsNullOrWhiteSpace(promptTag.Description))
            {
                File.AppendAllText(outputPath, $"\r\nNo '{keyword}' chunk found in PNG metadata.\r\n");
                //Console.WriteLine($"Output written to {outputPath}");
                return;
            }

            var tag = promptTag.Description.ExtractFirstJson();
            // Deserialize JSON from the metadata chunk
            var pipeline = JsonSerializer.Deserialize<Pipeline>(tag, PipelineJsonContext.Default.Options);
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
                writer.WriteLine($"  title      = {kvp.Value.Meta.Title}");
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
}