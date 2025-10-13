using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;

namespace PngMetadataReaderUI.Helpers;

internal static class ImageFormatHelper
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    public static bool IsSupportedImageFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        return !string.IsNullOrWhiteSpace(extension) && SupportedExtensions.Contains(extension);
    }

    public static string GetSupportedExtensionsDisplay() => "PNG, JPG, JPEG, WEBP";

    public static bool TryGetMimeType(string? path, out string mimeType)
    {
        mimeType = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            mimeType = "image/png";
            return true;
        }

        if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            mimeType = "image/jpeg";
            return true;
        }

        if (path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
        {
            mimeType = "image/webp";
            return true;
        }

        return false;
    }

    public static bool TryEncodeToBase64(string path, out string base64, out string mimeType, out string? error)
    {
        base64 = string.Empty;
        mimeType = string.Empty;
        error = null;

        if (!IsSupportedImageFile(path))
        {
            error = $"Format {Path.GetExtension(path)} wird nicht unterstuetzt.";
            return false;
        }

        try
        {
            if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                base64 = Convert.ToBase64String(File.ReadAllBytes(path));
                mimeType = "image/png";
                return true;
            }

            using var bitmap = new Bitmap(path);
            using var stream = new MemoryStream();
            bitmap.Save(stream); // Avalonia encodes as PNG
            base64 = Convert.ToBase64String(stream.ToArray());
            mimeType = "image/png";
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
