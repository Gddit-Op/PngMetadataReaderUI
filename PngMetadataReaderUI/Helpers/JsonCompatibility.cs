using System;
using System.Text;

namespace PngMetadataReaderUI.Helpers;

internal static class JsonCompatibility
{
    private static readonly string[] NonStandardNumberLiterals =
    [
        "-Infinity",
        "Infinity",
        "NaN"
    ];

    public static string NormalizeNonStandardNumbers(string json)
    {
        StringBuilder? normalized = null;
        var segmentStart = 0;
        var inString = false;
        var escaped = false;

        for (var index = 0; index < json.Length; index++)
        {
            var current = json[index];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (current == '\\')
                {
                    escaped = true;
                }
                else if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (current == '"')
            {
                inString = true;
                continue;
            }

            foreach (var literal in NonStandardNumberLiterals)
            {
                if (!json.AsSpan(index).StartsWith(literal, StringComparison.Ordinal) ||
                    !IsValueBoundary(json, index - 1) ||
                    !IsValueBoundary(json, index + literal.Length))
                {
                    continue;
                }

                normalized ??= new StringBuilder(json.Length);
                normalized.Append(json, segmentStart, index - segmentStart);
                normalized.Append("null");
                index += literal.Length - 1;
                segmentStart = index + 1;
                break;
            }
        }

        if (normalized == null)
        {
            return json;
        }

        normalized.Append(json, segmentStart, json.Length - segmentStart);
        return normalized.ToString();
    }

    private static bool IsValueBoundary(string json, int index) =>
        index < 0 ||
        index >= json.Length ||
        char.IsWhiteSpace(json[index]) ||
        json[index] is ':' or ',' or '[' or ']' or '{' or '}';
}
