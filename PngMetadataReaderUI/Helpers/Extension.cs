using System;

namespace PngMetadataReaderUI.Helpers;

public static class Extension
{
    public static string ExtractFirstJson(this string input)
    {
        int depth = 0, start = -1;
        var isInString = false;
        var isEscaped = false;

        for (int i = 0; i < input.Length; i++)
        {
            if (isInString)
            {
                if (isEscaped)
                {
                    isEscaped = false;
                }
                else if (input[i] == '\\')
                {
                    isEscaped = true;
                }
                else if (input[i] == (char)34)
                {
                    isInString = false;
                }

                continue;
            }

            if (input[i] == (char)34 && depth > 0)
            {
                isInString = true;
                continue;
            }

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
