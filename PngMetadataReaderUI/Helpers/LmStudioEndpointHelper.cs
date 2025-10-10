using PngMetadataReaderUI.Models;
using System;

namespace PngMetadataReaderUI.Helpers;

internal static class LmStudioEndpointHelper
{
    public static bool TryBuildBaseUri(UserSettings settings, out Uri? uri, out string? errorMessage)
    {
        return TryBuildBaseUri(settings.IpAddress, settings.Port, out uri, out errorMessage);
    }

    public static bool TryBuildBaseUri(string? addressInput, int defaultPort, out Uri? uri, out string? errorMessage)
    {
        uri = null;
        errorMessage = null;

        var hostInput = (addressInput ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(hostInput))
        {
            errorMessage = "Bitte IP-Adresse oder Hostnamen angeben.";
            return false;
        }

        var scheme = Uri.UriSchemeHttp;
        var port = defaultPort;
        var host = hostInput;

        if (Uri.TryCreate(hostInput, UriKind.Absolute, out var absoluteUri))
        {
            scheme = absoluteUri.Scheme;
            host = absoluteUri.Host;
            if (!absoluteUri.IsDefaultPort)
            {
                port = absoluteUri.Port;
            }
        }
        else
        {
            if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                host = host[7..];
            }
            else if (host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                host = host[8..];
                scheme = Uri.UriSchemeHttps;
            }

            var colonIndex = host.IndexOf(':');
            if (colonIndex >= 0 && colonIndex < host.Length - 1 &&
                int.TryParse(host[(colonIndex + 1)..], out var inlinePort))
            {
                port = inlinePort;
                host = host[..colonIndex];
            }
        }

        host = host.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(host))
        {
            errorMessage = "Host konnte nicht ermittelt werden.";
            return false;
        }

        if (port is < 1 or > 65535)
        {
            errorMessage = "Port muss zwischen 1 und 65535 liegen.";
            return false;
        }

        uri = new UriBuilder
        {
            Scheme = scheme,
            Host = host,
            Port = port,
            Path = "/"
        }.Uri;

        return true;
    }
}
