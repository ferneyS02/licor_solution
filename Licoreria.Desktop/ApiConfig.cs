using System;

namespace Licoreria.Desktop;

public static class ApiConfig
{
    // Puedes dejarlo en localhost sin problema
    public const string HOST = "http://localhost:5128";
    public const string API = $"{HOST}/api";

    public static string Img(string? nombreArchivo)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo))
            return $"{HOST}/imagenes/";

        // ✅ deja solo el nombre del archivo (por si llega con ruta)
        var clean = nombreArchivo.Trim().Replace("\\", "/");
        if (clean.Contains("/"))
            clean = clean.Split('/')[^1];

        // ✅ encode (espacios, ñ, tildes, etc.)
        clean = Uri.EscapeDataString(clean);

        return $"{HOST}/imagenes/{clean}";
    }
}

