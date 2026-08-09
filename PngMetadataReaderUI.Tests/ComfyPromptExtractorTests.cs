using PngMetadataReaderUI.Helpers;
using System.Text.Json;

namespace PngMetadataReaderUI.Tests;

public class ComfyPromptExtractorTests
{
    [Fact]
    public void ExtractsPositiveAndNegativeSamplerInputs()
    {
        var pipeline = Deserialize(
            """
            {
              "1": { "inputs": { "text": "a portrait", "clip": ["9", 0] }, "class_type": "CLIPTextEncode" },
              "2": { "inputs": { "text": "blurry, low quality", "clip": ["9", 0] }, "class_type": "CLIPTextEncode" },
              "3": { "inputs": { "positive": ["1", 0], "negative": ["2", 0] }, "class_type": "KSampler" }
            }
            """);

        var result = ComfyPromptExtractor.Extract(pipeline);

        Assert.Equal(["a portrait"], result.Positive);
        Assert.Equal(["blurry, low quality"], result.Negative);
        Assert.Contains("Positive Prompt:", result.ToText());
        Assert.Contains("Negative Prompt:", result.ToText());
        Assert.Equal(
            $"positive:a portrait{Environment.NewLine}{Environment.NewLine}" +
            $"negative:blurry, low quality{Environment.NewLine}----{Environment.NewLine}",
            result.ToFolderText());
    }

    [Fact]
    public void TreatsBasicGuiderConditioningAsPositivePrompt()
    {
        var pipeline = Deserialize(
            """
            {
              "6": { "inputs": { "text": "a bottle with a galaxy inside", "clip": ["11", 0] }, "class_type": "CLIPTextEncode" },
              "22": { "inputs": { "model": ["12", 0], "conditioning": ["6", 0] }, "class_type": "BasicGuider" }
            }
            """);

        var result = ComfyPromptExtractor.Extract(pipeline);

        Assert.Equal(["a bottle with a galaxy inside"], result.Positive);
        Assert.Empty(result.Negative);
        Assert.DoesNotContain("Negative Prompt:", result.ToText());
    }

    [Fact]
    public void PreservesPolarityAcrossIntermediateConditioningNode()
    {
        var pipeline = Deserialize(
            """
            {
              "1": { "inputs": { "text": "sunlit landscape", "clip": ["9", 0] }, "class_type": "CLIPTextEncode" },
              "2": { "inputs": { "text": "fog", "clip": ["9", 0] }, "class_type": "CLIPTextEncode" },
              "3": {
                "inputs": { "positive": ["1", 0], "negative": ["2", 0], "control_net": ["8", 0] },
                "class_type": "ControlNetApplyAdvanced"
              },
              "4": { "inputs": { "positive": ["3", 0], "negative": ["3", 1] }, "class_type": "KSampler" }
            }
            """);

        var result = ComfyPromptExtractor.Extract(pipeline);

        Assert.Equal(["sunlit landscape"], result.Positive);
        Assert.Equal(["fog"], result.Negative);
    }

    [Fact]
    public void IgnoresEmptyNegativePrompt()
    {
        var pipeline = Deserialize(
            """
            {
              "1": { "inputs": { "text": "detailed photo", "clip": ["9", 0] }, "class_type": "CLIPTextEncode" },
              "2": { "inputs": { "text": "", "clip": ["9", 0] }, "class_type": "CLIPTextEncode" },
              "3": { "inputs": { "positive": ["1", 0], "negative": ["2", 0] }, "class_type": "KSampler" }
            }
            """);

        var result = ComfyPromptExtractor.Extract(pipeline);

        Assert.Equal(["detailed photo"], result.Positive);
        Assert.Empty(result.Negative);
    }

    [Fact]
    public void NormalizesNonStandardNumbersOutsideStrings()
    {
        const string json =
            """
            {
              "77": {
                "inputs": { "label": "NaN, Infinity and -Infinity stay text" },
                "class_type": "PreviewImage",
                "is_changed": [NaN, Infinity, -Infinity]
              }
            }
            """;

        var normalized = JsonCompatibility.NormalizeNonStandardNumbers(json);
        using var document = JsonDocument.Parse(normalized);
        var node = document.RootElement.GetProperty("77");

        Assert.Equal(
            "NaN, Infinity and -Infinity stay text",
            node.GetProperty("inputs").GetProperty("label").GetString());
        Assert.All(
            node.GetProperty("is_changed").EnumerateArray(),
            value => Assert.Equal(JsonValueKind.Null, value.ValueKind));
    }

    private static Pipeline Deserialize(string json) =>
        JsonSerializer.Deserialize(json, PipelineJsonContext.Default.Pipeline)!;
}
