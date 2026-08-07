using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PngMetadataReaderUI.Helpers;

internal static class ComfyPromptExtractor
{
    public static ComfyPrompts Extract(Pipeline pipeline)
    {
        var positive = new List<string>();
        var negative = new List<string>();

        foreach (var node in pipeline.Values)
        {
            ExtractInput(pipeline, node, "positive", true, positive);
            ExtractInput(pipeline, node, "negative", false, negative);

            if (node.ClassType.Equals("BasicGuider", StringComparison.OrdinalIgnoreCase))
            {
                ExtractInput(pipeline, node, "conditioning", true, positive);
            }
        }

        var known = positive.Concat(negative).ToHashSet(StringComparer.Ordinal);
        var unclassified = new List<string>();
        foreach (var node in pipeline.Values.Where(IsTextEncode))
        {
            var texts = GetTexts(node).ToList();
            var title = node.Meta?.Title ?? string.Empty;
            if (title.Contains("negative", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("negativ", StringComparison.OrdinalIgnoreCase))
            {
                AddDistinct(negative, texts);
            }
            else if (title.Contains("positive", StringComparison.OrdinalIgnoreCase) ||
                     title.Contains("positiv", StringComparison.OrdinalIgnoreCase))
            {
                AddDistinct(positive, texts);
            }
            else
            {
                unclassified.AddRange(texts.Where(text => !known.Contains(text)));
            }
        }

        var distinctUnclassified = unclassified.Distinct(StringComparer.Ordinal).ToList();
        if (positive.Count == 0 && distinctUnclassified.Count == 1)
        {
            positive.Add(distinctUnclassified[0]);
        }

        return new ComfyPrompts(positive, negative);
    }

    private static void ExtractInput(
        Pipeline pipeline,
        Node node,
        string inputName,
        bool isPositive,
        List<string> destination)
    {
        if (TryGetInput(node, inputName, out var input) && TryGetReference(input, out var reference))
        {
            Traverse(pipeline, reference, isPositive, destination, []);
        }
    }

    private static void Traverse(
        Pipeline pipeline,
        NodeReference reference,
        bool isPositive,
        List<string> destination,
        HashSet<NodeReference> visited)
    {
        if (!visited.Add(reference) || !pipeline.TryGetValue(reference.NodeId, out var node))
        {
            return;
        }

        if (IsTextEncode(node))
        {
            AddDistinct(destination, GetTexts(node));
            return;
        }

        var polarityName = isPositive ? "positive" : "negative";
        if (TryGetInput(node, polarityName, out var polarityInput) &&
            TryGetReference(polarityInput, out var polarityReference))
        {
            Traverse(pipeline, polarityReference, isPositive, destination, visited);
            return;
        }

        foreach (var input in node.Inputs.Values)
        {
            if (TryGetReference(input, out var nestedReference))
            {
                Traverse(pipeline, nestedReference, isPositive, destination, visited);
            }
        }
    }

    private static IEnumerable<string> GetTexts(Node node)
    {
        foreach (var input in node.Inputs)
        {
            var isTextInput = input.Key.Equals("text", StringComparison.OrdinalIgnoreCase) ||
                              input.Key.StartsWith("text_", StringComparison.OrdinalIgnoreCase);
            if (!isTextInput || input.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var text = input.Value.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text;
            }
        }
    }

    private static bool IsTextEncode(Node node) =>
        node.ClassType.Contains("CLIPTextEncode", StringComparison.OrdinalIgnoreCase);

    private static bool TryGetInput(Node node, string name, out JsonElement value)
    {
        foreach (var input in node.Inputs)
        {
            if (input.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = input.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetReference(JsonElement value, out NodeReference reference)
    {
        reference = default;
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
        {
            return false;
        }

        var idElement = value[0];
        var nodeId = idElement.ValueKind switch
        {
            JsonValueKind.String => idElement.GetString(),
            JsonValueKind.Number => idElement.GetRawText(),
            _ => null
        };
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return false;
        }

        var outputIndex = value.GetArrayLength() > 1 && value[1].TryGetInt32(out var index) ? index : 0;
        reference = new NodeReference(nodeId, outputIndex);
        return true;
    }

    private static void AddDistinct(List<string> destination, IEnumerable<string> values)
    {
        foreach (var value in values.Where(value => !destination.Contains(value, StringComparer.Ordinal)))
        {
            destination.Add(value);
        }
    }

    private readonly record struct NodeReference(string NodeId, int OutputIndex);
}

internal sealed record ComfyPrompts(IReadOnlyList<string> Positive, IReadOnlyList<string> Negative)
{
    public static ComfyPrompts Empty { get; } = new([], []);

    public bool HasPrompts => Positive.Count > 0 || Negative.Count > 0;

    public string ToText()
    {
        var builder = new StringBuilder();
        Append(builder, "Positive Prompt", Positive);
        Append(builder, "Negative Prompt", Negative);
        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    public string ToFolderText()
    {
        var builder = new StringBuilder();
        builder.Append("positive:");
        builder.AppendLine(string.Join(Environment.NewLine, Positive));
        builder.AppendLine();
        builder.Append("negative:");
        builder.AppendLine(string.Join(Environment.NewLine, Negative));
        builder.AppendLine("----");
        return builder.ToString();
    }

    private static void Append(StringBuilder builder, string heading, IReadOnlyList<string> prompts)
    {
        for (var index = 0; index < prompts.Count; index++)
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            var number = prompts.Count > 1 ? $" {index + 1}" : string.Empty;
            builder.AppendLine($"{heading}{number}:");
            builder.AppendLine(prompts[index]);
        }
    }
}
