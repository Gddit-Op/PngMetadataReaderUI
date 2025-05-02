using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PngMetadataReaderUI.Helpers;

public class Pipeline : Dictionary<string, Node> { }

public class Node
{
    [JsonPropertyName("inputs")]
    public Dictionary<string, JsonElement> Inputs { get; set; }

    [JsonPropertyName("class_type")]
    public string ClassType { get; set; }

    [JsonPropertyName("_meta")]
    public Meta Meta { get; set; }
}

public class Meta
{
    [JsonPropertyName("title")]
    public string Title { get; set; }
}

// 2) The source‐generation context
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    ReadCommentHandling = JsonCommentHandling.Skip
)]
[JsonSerializable(typeof(Pipeline))]
[JsonSerializable(typeof(Node))]
[JsonSerializable(typeof(Meta))]
internal partial class PipelineJsonContext : JsonSerializerContext
{
    // The source generator will fill in
}
