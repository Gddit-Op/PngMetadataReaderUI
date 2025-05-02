using System;

namespace PngMetadataReaderUI.Helpers;

public static class Extension
{
    public static string ExtractFirstJson(this string input)
    {
        int depth = 0, start = -1;
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '{')
            {
                if (depth == 0) start = i;
                depth++;
            }
            else if (input[i] == '}')
            {
                depth--;
                if (depth == 0 && start >= 0)
                {
                    return input.Substring(start, i - start + 1);
                }
                if (depth < 0) break;  // zu viele schließende Klammern
            }
        }
        throw new InvalidOperationException("Kein vollständiges JSON-Objekt gefunden.");
    }
}