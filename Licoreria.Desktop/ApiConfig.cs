namespace Licoreria.Desktop;

public static class ApiConfig
{
    public const string HOST = "http://192.168.1.20:5128";
    public const string API = $"{HOST}/api";

    public static string Img(string? nombreArchivo)
    {
        var n = (nombreArchivo ?? "").Trim();

        if (string.IsNullOrWhiteSpace(n))
            return $"{HOST}/imagenes/shot_icon.png";

        // Si ya viene como "/imagenes/aguila.png" o "imagenes/aguila.png"
        if (n.StartsWith("/imagenes/", StringComparison.OrdinalIgnoreCase))
            return $"{HOST}{n}";
        if (n.StartsWith("imagenes/", StringComparison.OrdinalIgnoreCase))
            return $"{HOST}/{n}";

        // Si viene URL completa
        if (n.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            n.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return n;

        return $"{HOST}/imagenes/{Uri.EscapeDataString(n)}";
    }
}
