namespace Licoreria.Desktop;

/// <summary>
/// Sesión en memoria (simple, local).
/// Mantiene el token JWT y la info del usuario autenticado.
/// </summary>
public static class Session
{
    public static int IdUsuario { get; set; }
    public static string Nombre { get; set; } = string.Empty;
    public static string Rol { get; set; } = string.Empty; // Admin | Vendedor | Sistema
    public static string? Token { get; set; }

    public static bool IsLoggedIn => !string.IsNullOrWhiteSpace(Token);

    public static void Clear()
    {
        IdUsuario = 0;
        Nombre = string.Empty;
        Rol = string.Empty;
        Token = null;
    }
}
